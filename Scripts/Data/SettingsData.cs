using System;

[Serializable]
public class SettingsData
{
    public float MasterVolume { get; set; } = 1.0f;
    public bool Fullscreen { get; set; }
}
