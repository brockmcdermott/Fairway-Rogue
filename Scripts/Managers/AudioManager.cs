using Godot;

public partial class AudioManager : Node
{
    public string CurrentMusicKey { get; private set; } = string.Empty;

    public void PlaySfx(string key)
    {
        GD.Print($"[AudioManager] PlaySfx placeholder: {key}");
    }

    public void PlayMusic(string key)
    {
        CurrentMusicKey = key;
        GD.Print($"[AudioManager] PlayMusic placeholder: {key}");
    }

    public void StopMusic()
    {
        CurrentMusicKey = string.Empty;
        GD.Print("[AudioManager] StopMusic placeholder");
    }

    public void FadeToMusic(string key)
    {
        CurrentMusicKey = key;
        GD.Print($"[AudioManager] FadeToMusic placeholder: {key}");
    }
}
