using Godot;

public partial class SceneRouter : Node
{
    public const string MainMenuScenePath = "res://Scenes/MainMenu.tscn";
    public const string GameplayScenePath = "res://Scenes/Gameplay/GameplayScene.tscn";
    public const string ShopScenePath = "res://Scenes/UI/ShopScene.tscn";
    public const string ScorecardScenePath = "res://Scenes/UI/ScorecardScene.tscn";
    public const string RunCompleteScenePath = "res://Scenes/UI/RunCompleteScene.tscn";
    public const string SettingsScenePath = "res://Scenes/UI/SettingsScene.tscn";

    public Error GoToMainMenu()
    {
        return ChangeScene(MainMenuScenePath);
    }

    public Error GoToGameplay()
    {
        return ChangeScene(GameplayScenePath);
    }

    public Error GoToShop()
    {
        return ChangeScene(ShopScenePath);
    }

    public Error GoToScorecard()
    {
        return ChangeScene(ScorecardScenePath);
    }

    public Error GoToRunComplete()
    {
        return ChangeScene(RunCompleteScenePath);
    }

    public Error GoToSettings()
    {
        return ChangeScene(SettingsScenePath);
    }

    public Error ChangeScene(string scenePath)
    {
        if (!ResourceLoader.Exists(scenePath))
        {
            GD.PushError($"[SceneRouter] Scene not found: {scenePath}");
            return Error.FileNotFound;
        }

        var result = GetTree().ChangeSceneToFile(scenePath);
        if (result != Error.Ok)
        {
            GD.PushError($"[SceneRouter] Failed to change scene to {scenePath}: {result}");
        }

        return result;
    }
}
