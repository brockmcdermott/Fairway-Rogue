using System.Collections.Generic;
using Godot;

public partial class AudioManager : Node
{
    private readonly struct ToneProfile
    {
        public ToneProfile(float startFrequencyHz, float endFrequencyHz, float durationSeconds, float amplitude)
        {
            StartFrequencyHz = startFrequencyHz;
            EndFrequencyHz = endFrequencyHz;
            DurationSeconds = durationSeconds;
            Amplitude = amplitude;
        }

        public float StartFrequencyHz { get; }
        public float EndFrequencyHz { get; }
        public float DurationSeconds { get; }
        public float Amplitude { get; }
    }

    [ExportGroup("Audio Buses")]
    [Export] public string SfxBus { get; set; } = "Master";
    [Export] public string MusicBus { get; set; } = "Master";

    [ExportGroup("Volume")]
    [Export] public float SfxVolumeDb { get; set; } = -9.0f;
    [Export] public float MusicVolumeDb { get; set; } = -18.0f;

    [ExportGroup("Generator")]
    [Export] public int MixRate { get; set; } = 44100;

    public string CurrentMusicKey { get; private set; } = string.Empty;

    private readonly Dictionary<string, ToneProfile> _sfxProfiles = new Dictionary<string, ToneProfile>
    {
        ["shot"] = new ToneProfile(680.0f, 340.0f, 0.14f, 0.55f),
        ["splash"] = new ToneProfile(220.0f, 120.0f, 0.24f, 0.45f),
        ["sand_impact"] = new ToneProfile(300.0f, 180.0f, 0.18f, 0.35f),
        ["hole_complete"] = new ToneProfile(480.0f, 760.0f, 0.22f, 0.45f),
        ["ui_click"] = new ToneProfile(680.0f, 680.0f, 0.06f, 0.30f),
        ["shop_purchase"] = new ToneProfile(420.0f, 900.0f, 0.20f, 0.40f),
        ["recovery_prompt"] = new ToneProfile(330.0f, 240.0f, 0.20f, 0.40f)
    };

    private readonly Dictionary<string, ToneProfile> _musicCues = new Dictionary<string, ToneProfile>
    {
        ["menu"] = new ToneProfile(170.0f, 210.0f, 0.28f, 0.25f),
        ["gameplay"] = new ToneProfile(210.0f, 240.0f, 0.28f, 0.22f)
    };

    private SaveManager? SaveManagerSingleton => AutoloadLocator.Get<SaveManager>(this, nameof(SaveManager));

    public override void _Ready()
    {
        ApplySettingsVolumeIfAvailable();
    }

    public void PlaySfx(string key)
    {
        if (!_sfxProfiles.TryGetValue(key, out var profile))
        {
            profile = _sfxProfiles["ui_click"];
        }

        var player = BuildOneShotPlayer(SfxBus, SfxVolumeDb);
        PlayTone(player, profile);
    }

    public void PlayMusic(string key)
    {
        if (CurrentMusicKey == key)
        {
            return;
        }

        CurrentMusicKey = key;
        if (!_musicCues.TryGetValue(key, out var profile))
        {
            return;
        }

        var player = BuildOneShotPlayer(MusicBus, MusicVolumeDb);
        PlayTone(player, profile);
    }

    public void StopMusic()
    {
        CurrentMusicKey = string.Empty;
    }

    public void FadeToMusic(string key)
    {
        PlayMusic(key);
    }

    private AudioStreamPlayer BuildOneShotPlayer(string busName, float volumeDb)
    {
        var player = new AudioStreamPlayer
        {
            Bus = busName,
            VolumeDb = volumeDb
        };

        AddChild(player);
        return player;
    }

    private void PlayTone(AudioStreamPlayer player, ToneProfile profile)
    {
        var duration = Mathf.Max(0.03f, profile.DurationSeconds);
        var stream = new AudioStreamGenerator
        {
            MixRate = MixRate,
            BufferLength = duration + 0.10f
        };

        player.Stream = stream;
        player.Play();

        if (player.GetStreamPlayback() is AudioStreamGeneratorPlayback playback)
        {
            PushToneSamples(playback, stream.MixRate, profile);
        }

        var cleanupTimer = GetTree().CreateTimer(duration + 0.16f);
        cleanupTimer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(player))
            {
                player.QueueFree();
            }
        };
    }

    private static void PushToneSamples(AudioStreamGeneratorPlayback playback, float mixRate, ToneProfile profile)
    {
        var sampleCount = Mathf.Max(1, Mathf.RoundToInt(profile.DurationSeconds * mixRate));
        var startFreq = Mathf.Max(20.0f, profile.StartFrequencyHz);
        var endFreq = Mathf.Max(20.0f, profile.EndFrequencyHz);

        var phase = 0.0f;
        for (var i = 0; i < sampleCount; i += 1)
        {
            var t = sampleCount <= 1 ? 0.0f : i / (float)(sampleCount - 1);
            var frequency = Mathf.Lerp(startFreq, endFreq, t);
            phase += Mathf.Tau * frequency / mixRate;

            var envelope = Mathf.Sin(Mathf.Pi * t);
            var sample = Mathf.Sin(phase) * profile.Amplitude * envelope;
            playback.PushFrame(new Vector2(sample, sample));
        }
    }

    private void ApplySettingsVolumeIfAvailable()
    {
        var settings = SaveManagerSingleton?.LoadSettings();
        if (settings == null)
        {
            return;
        }

        var linear = Mathf.Clamp(settings.MasterVolume, 0.0f, 1.0f);
        var masterVolumeDb = Mathf.LinearToDb(Mathf.Max(0.001f, linear));
        SfxVolumeDb = masterVolumeDb - 8.0f;
        MusicVolumeDb = masterVolumeDb - 18.0f;
    }
}
