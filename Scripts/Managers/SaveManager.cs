using System;
using System.Text.Json;
using Godot;

public partial class SaveManager : Node
{
    private const string RunSavePath = "user://run_save.json";
    private const string HighScorePath = "user://high_score.json";
    private const string SettingsPath = "user://settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public void SaveRun(RunSaveData data)
    {
        WriteJson(RunSavePath, data);
    }

    public RunSaveData? LoadRun()
    {
        return ReadJson<RunSaveData>(RunSavePath);
    }

    public void SaveHighScore(int score)
    {
        WriteJson(HighScorePath, score);
    }

    public int LoadHighScore()
    {
        var score = ReadJson<int?>(HighScorePath);
        return score ?? 0;
    }

    public void SaveSettings(SettingsData settings)
    {
        WriteJson(SettingsPath, settings);
    }

    public SettingsData LoadSettings()
    {
        return ReadJson<SettingsData>(SettingsPath) ?? new SettingsData();
    }

    public bool HasRunSave()
    {
        return FileAccess.FileExists(RunSavePath);
    }

    public void DeleteRunSave()
    {
        if (!HasRunSave())
        {
            return;
        }

        var absolutePath = ProjectSettings.GlobalizePath(RunSavePath);
        var result = DirAccess.RemoveAbsolute(absolutePath);
        if (result != Error.Ok)
        {
            GD.PushWarning($"[SaveManager] Failed to delete save ({result}) at {absolutePath}");
        }
    }

    private void WriteJson<T>(string path, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PushError($"[SaveManager] Could not open {path} for writing.");
                return;
            }

            file.StoreString(json);
        }
        catch (Exception ex)
        {
            GD.PushError($"[SaveManager] WriteJson failed for {path}: {ex.Message}");
        }
    }

    private T? ReadJson<T>(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            return default;
        }

        try
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PushWarning($"[SaveManager] Could not open {path} for reading.");
                return default;
            }

            var json = file.GetAsText();
            if (string.IsNullOrWhiteSpace(json))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[SaveManager] ReadJson failed for {path}: {ex.Message}");
            return default;
        }
    }
}
