using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace ArcCreate.Compose.Rendering
{
    public class FrameRenderer : IDisposable
    {
        private readonly RenderTexture renderTexture;
        private readonly Texture2D texture2D;
        private readonly Camera[] cameras;
        private readonly RenderTexture[] defaultRenderTextures;
        private readonly string outputPath;
        private readonly float startRenderingTime;
        private readonly float endRenderingTime;
        private readonly int crf;
        private readonly float fps;
        private readonly int width;
        private readonly int height;
        private byte[] cachedByteArray;

        public FrameRenderer(string outputPath, Camera[] cameras, int width, int height, float fps, int crf, int from, int to)
        {
            this.width = width;
            this.height = height;
            this.cameras = cameras;
            this.outputPath = outputPath;
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false, true);

            startRenderingTime = from / 1000f;
            endRenderingTime = to / 1000f;
            this.crf = crf;
            this.fps = fps;

            defaultRenderTextures = new RenderTexture[cameras.Length];
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                defaultRenderTextures[i] = cam.targetTexture;
            }
        }

        public delegate void RenderStatusDelegate(TimeSpan passed, TimeSpan remaining);

        public Texture2D Texture2D => texture2D;

        public void Dispose()
        {
            UnityEngine.Object.Destroy(renderTexture);
            UnityEngine.Object.Destroy(texture2D);
        }

        public async UniTask RenderVideo(CancellationToken token, RenderStatusDelegate onETA)
        {
            if (!TestFfmpeg())
            {
                return;
            }

            RenderTexture activeRT = RenderTexture.active;
            foreach (Camera cam in cameras)
            {
                cam.targetTexture = renderTexture;
            }

            Process ffmpegProcess = null;
            BinaryWriter ffmpegWriter = null;
            try
            {
                ffmpegProcess = GetFFmpegProcess(outputPath);
                ffmpegWriter = new BinaryWriter(ffmpegProcess.StandardInput.BaseStream);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(I18n.S("Compose.Exception.FFmpeg.Start", new Dictionary<string, object>()
                {
                    { "Message", e.Message },
                    { "StackTrace", e.StackTrace },
                }));

                ffmpegProcess?.Dispose();
                ffmpegWriter?.Dispose();
            }

            try
            {
                DateTime startAt = DateTime.Now;
                Time.captureFramerate = Mathf.RoundToInt(fps);

                float unityStartTime = Time.time;
                while (!token.IsCancellationRequested)
                {
                    if (ffmpegProcess.HasExited)
                    {
                        break;
                    }

                    foreach (var cam in cameras)
                    {
                        cam.targetTexture = renderTexture;
                        RenderTexture.active = cam.targetTexture;
                        cam.Render();
                    }

                    texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture2D.Apply();

                    Profiler.BeginSample("Renderer: Extract bytes");
                    NativeArray<byte> bytes = texture2D.GetRawTextureData<byte>();
                    if (bytes.Length != cachedByteArray?.Length)
                    {
                        cachedByteArray = new byte[bytes.Length];
                    }

                    bytes.CopyTo(cachedByteArray);
                    ffmpegWriter.Write(cachedByteArray);

                    Profiler.EndSample();

                    Time.timeScale = 1;
                    await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

                    float time = Time.time - unityStartTime + startRenderingTime;
                    if (time * 1000 > Services.Gameplay.Audio.AudioLength || time > endRenderingTime)
                    {
                        break;
                    }
                    else
                    {
                        Services.Gameplay.Audio.AudioTiming = Mathf.RoundToInt(time * 1000);
                        Time.timeScale = 0;
                    }

                    TimeSpan elapsed = DateTime.Now - startAt;

                    double speed = (time - startRenderingTime) / elapsed.TotalSeconds;
                    TimeSpan eta = TimeSpan.FromSeconds((endRenderingTime - time) / speed);
                    onETA.Invoke(elapsed, eta);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(I18n.S("Compose.Exception.FFmpeg.Write", new Dictionary<string, object>()
                {
                    { "Message", e.Message },
                    { "StackTrace", e.StackTrace },
                }));
            }
            finally
            {
                ffmpegProcess.Dispose();
                ffmpegWriter.Dispose();

                // Weird bug occurs if you remove this. I encourage everyone reading this source code to try it out lol.
                await UniTask.DelayFrame(60);

                RenderTexture.active = activeRT;
                for (int i = 0; i < cameras.Length; i++)
                {
                    Camera cam = cameras[i];
                    cam.targetTexture = defaultRenderTextures[i];
                }

                Time.captureFramerate = 0;
            }
        }

        private bool TestFfmpeg()
        {
            try
            {
                Process testFFmpegProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Settings.FFmpegPath.Value,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    },
                };
                testFFmpegProcess.Start();
                return true;
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogError(I18n.S("Compose.Exception.FFmpeg.NotFound", Settings.FFmpegPath.Value));
            }

            return false;
        }

        private Process GetFFmpegProcess(string videoPath)
        {
            // string path = GetPath();

            // libx264 doesn't believe in odd sizes
            int w = Mathf.Max(width - (width % 2), 2);
            int h = Mathf.Max(height - (height % 2), 2);
            videoPath = videoPath.Replace(@"""", @"\""");
            if (!videoPath.ToLower().EndsWith(".mp4"))
            {
                videoPath += ".mp4";
            }

            UnityEngine.Debug.Log($"Writing to {videoPath}");

            // string argsForSfxAudio = "";
            // foreach (string key in sfxAudio.Keys)
            // {
            //     argsForSfxAudio += $"-i \"{Path.Combine(path, $"sfx_{key}.wav")}\" ";
            // }
#pragma warning disable
            string args = ""
            + $" -f rawvideo -pixel_format argb -video_size {width}x{height} -framerate {fps} -i pipe: "
            // + $"-i \"{Path.Combine(path, "sfx.wav")}\" "   // First audio (sfx)
            // + $"-i \"{Path.Combine(path, "song.wav")}\" "  // Second audio (sfx)
            // + argsForSfxAudio
            // + $"-filter_complex amix=inputs={2 + sfxAudio.Count}:duration=longest " //Mix 2 audio files
            + $"-c:v libx264 "             //Video codec
            + $"-c:a aac "                 //Audio codec
            + $"-pix_fmt yuv420p "         //Set pixel format for QuickTime
            + $"-crf {crf} "               //Video quality
            + $"-vf vflip,scale={w}x{h} "  //Video size, vflip because screenshot in byte array is upside down
            + $"-b:a 384k "                //Audio quality
            + $"-bf 2 "                    //2 B-frames
            + $"-flags +cgop "             //Closed GOP (as it should be)
            + $"-movflags +faststart "     //Move stream info to the beginning of file
            + $"-preset ultrafast "        //Creates slightly larger file but speeds up rendering drastically
            + $"-y -- \"{videoPath}\"";
#pragma warning restore

            var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Settings.FFmpegPath.Value,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };

            ffmpegProcess.Start();
            ffmpegProcess.ErrorDataReceived += (sender, eventArgs) => { UnityEngine.Debug.Log(eventArgs.Data); };
            ffmpegProcess.BeginErrorReadLine();
            ffmpegProcess.OutputDataReceived += (sender, eventArgs) => { UnityEngine.Debug.Log(eventArgs.Data); };
            ffmpegProcess.BeginOutputReadLine();

            return ffmpegProcess;
        }
    }
}