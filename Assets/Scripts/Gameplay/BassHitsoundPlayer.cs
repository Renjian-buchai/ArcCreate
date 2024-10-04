namespace ArcCreate.Gameplay.Hitsound 
{
using ManagedBass;

  public class BassHitsoundPlayer 
  {
    /// <summary>
    /// Volume of hit sound. 
    /// 1.0 => normal, 0.0 => silent, >1.0 => amplified
    /// </summary>
    private float volume = 1.0f;
    private int arcClipHandle;
    private int tapClipHandle;

    public BassHitsoundPlayer() 
    {
    }

    public float Volume 
    {
      get => this.volume;
      set => this.volume = value;
    }

    public void Dispose() 
    {
      Bass.ChannelStop(this.tapClipHandle);
      Bass.ChannelStop(this.arcClipHandle);
    }

    public void LoadArc(byte[] pathToClip) 
    {
      this.arcClipHandle = Bass.CreateStream(pathToClip, 0, 0, BassFlags.Prescan | BassFlags.Default);
    }

    public void LoadTap(byte[] pathToClip) 
    {
      this.tapClipHandle = Bass.CreateStream(pathToClip, 0, 0, BassFlags.Prescan | BassFlags.Default);
    }

    public void PlayArc() 
    {
      Bass.ChannelPlay(this.arcClipHandle);
    }

    public void PlayTap() 
    {
      Bass.ChannelPlay(this.tapClipHandle);
    }
  }
}
