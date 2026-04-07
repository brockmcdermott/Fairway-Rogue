using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public partial class SaveManager : Node
{
    private const string RunSavePath = "user://run_save.json";
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
        return TryLoadRun(out var data) ? data : null;
    }

    public bool TryLoadRun(out RunSaveData? runSaveData)
    {
        runSaveData = ReadJson<RunSaveData>(RunSavePath);
        if (runSaveData == null)
        {
            return false;
        }

        if (!ValidateRunSaveData(runSaveData))
        {
            GD.PushWarning("[SaveManager] Run save exists but failed validation. Treating as corrupt.");
            runSaveData = null;
            return false;
        }

        return true;
    }

    public void SaveHighScore(int score)
    {
        SaveHighScore(score, RunManager.DefaultTotalHoles);
    }

    public void SaveHighScore(int score, int totalHoles)
    {
        WriteJson(GetHighScorePath(totalHoles), score);
    }

    public bool HasHighScoreSave()
    {
        return HasHighScoreSave(RunManager.DefaultTotalHoles);
    }

    public bool HasHighScoreSave(int totalHoles)
    {
        return FileAccess.FileExists(GetHighScorePath(totalHoles));
    }

    public int LoadHighScore()
    {
        return LoadHighScore(RunManager.DefaultTotalHoles);
    }

    public int LoadHighScore(int totalHoles)
    {
        var score = ReadJson<int?>(GetHighScorePath(totalHoles));
        return score ?? 0;
    }

    public void SaveBestRun(BestRunData bestRunData)
    {
        SaveBestRun(bestRunData, RunManager.DefaultTotalHoles);
    }

    public void SaveBestRun(BestRunData bestRunData, int totalHoles)
    {
        WriteJson(GetBestRunPath(totalHoles), bestRunData);
    }

    public bool HasBestRunSave()
    {
        return HasBestRunSave(RunManager.DefaultTotalHoles);
    }

    public bool HasBestRunSave(int totalHoles)
    {
        return FileAccess.FileExists(GetBestRunPath(totalHoles));
    }

    public BestRunData? LoadBestRun()
    {
        return LoadBestRun(RunManager.DefaultTotalHoles);
    }

    public BestRunData? LoadBestRun(int totalHoles)
    {
        return ReadJson<BestRunData>(GetBestRunPath(totalHoles));
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

    private static string GetHighScorePath(int totalHoles)
    {
        return $"user://high_score_{Mathf.Clamp(totalHoles, 1, 36)}.json";
    }

    private static string GetBestRunPath(int totalHoles)
    {
        return $"user://best_run_{Mathf.Clamp(totalHoles, 1, 36)}.json";
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

    private static bool ValidateRunSaveData(RunSaveData data)
    {
        if (data.TotalHoles <= 0)
        {
            return false;
        }

        if (data.CurrentHoleIndex <= 0 || data.CurrentHoleIndex > data.TotalHoles)
        {
            return false;
        }

        if (data.Currency < 0)
        {
            return false;
        }

        data.HoleResults ??= new List<HoleResultData>();
        if (data.HoleResults.Count > data.TotalHoles)
        {
            return false;
        }

        data.CurrentLoadout ??= PlayerLoadout.CreateDefault();
        data.CurrentLoadout.UnlockedBallIds ??= new List<string>();
        data.CurrentLoadout.PurchasedUpgradeIds ??= new List<string>();

        return true;
    }
}
