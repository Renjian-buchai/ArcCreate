using System;
using UnityEngine;
using ManagedBass;

namespace ArcCreate.Gameplay.Hitsound {
  public class BassHitsoundPlayer {
    /// <summary>
    /// Volume of hitsound. 
    /// 1.0 => normal, 0.0 => silent, >1.0 => amplified
    /// </summary>
    private float volume = 1.0f;
    private int arcClipHandle;
    private int tapClipHandle;

    public BassHitsoundPlayer() {
    }

    public float Volume {
      get => volume;
      set => volume = value;
    }

    public void Dispose() {
      Bass.ChannelStop(tapClipHandle);
      Bass.ChannelStop(arcClipHandle);
    }

    public void LoadArc(byte[] pathToClip) {
      arcClipHandle = Bass.CreateStream(pathToClip, 0, 0, BassFlags.Prescan | BassFlags.Default);
    }

    public void LoadTap(byte[] pathToClip) {
      tapClipHandle = Bass.CreateStream(pathToClip, 0, 0, BassFlags.Prescan | BassFlags.Default);
    }

    public void PlayArc() {
      Bass.ChannelPlay(arcClipHandle);
    }

    public void PlayTap() {
      Bass.ChannelPlay(tapClipHandle);
    }
  }

}