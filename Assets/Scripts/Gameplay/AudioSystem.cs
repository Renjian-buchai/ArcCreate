namespace ArcCreate.AudioSystem 
{
using ManagedBass;
using UnityEngine;

  public static class AudioSystem 
  {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public static void Initialise() 
    {
      // Code basically copied from https://github.com/2394425147/my-findings/
      Bass.Init();

      Application.quitting += Dispose;

      Bass.UpdatePeriod = 5;
      Bass.DeviceBufferLength = 10;
      Bass.PlaybackBufferLength = 100;
      Bass.DeviceNonStop = true;

      // To synchronise audio with direct sound playback, if it ever falls back to that
      Bass.Configure(Configuration.TruePlayPosition, 0);

      // Set audio policy to one that obeys the mute switch
      // Probably means that it turns off audio when silent mode is on
      Bass.Configure(Configuration.IOSMixAudio, 5);

      // Always provide a default device
      Bass.Configure(Configuration.IncludeDefaultDevice, true);

      // Enable custom BASS_CONFIG_MP3_OLDGOPS flag for backwards compatibility
      Bass.Configure((Configuration)68, 1);

      // Disable BASS_CONFIG_DEV_TIMEOUT flag to keep BASS audio output from pausing on device processing timeout.
      // See https://www.un4seen.com/forum/?topic=19601 for more information.
      Bass.Configure((Configuration)70, false);
    }

    public static void Dispose() 
    {
      Bass.Free();

      Application.quitting -= Dispose;
    }
  }
}