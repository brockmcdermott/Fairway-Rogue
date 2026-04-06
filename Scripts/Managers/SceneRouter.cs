using Godot;

public partial class SceneRouter : Node
{
    public const string MainMenuScenePath = "res://Scenes/MainMenu.tscn";
    public const string GameplayScenePath = "res://Scenes/Gameplay/GameplayScene.tscn";
    public const string ShopScenePath = "res://Scenes/UI/ShopScene.tscn";
    public const string ScorecardScenePath = "res://Scenes/UI/ScorecardScene.tscn";
    public const string RunCompleteScenePath = "res://Scenes/UI/RunCompleteScene.tscn";
    public const string SettingsScenePath = "res://Scenes/UI/SettingsScene.tscn";

    private string? _pendingScenePath;
    private bool _changeDeferred;

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

        _pendingScenePath = scenePath;

        if (_changeDeferred)
        {
            return Error.Ok;
        }

        // Always defer scene changes so collision/physics callbacks cannot remove
        // CollisionObject nodes in the same step.
        _changeDeferred = true;
        CallDeferred(nameof(ApplyDeferredSceneChange));
        return Error.Ok;
    }

    private void ApplyDeferredSceneChange()
    {
        _changeDeferred = false;

        var path = _pendingScenePath;
        _pendingScenePath = null;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        _ = ChangeSceneImmediate(path);
    }

    private Error ChangeSceneImmediate(string scenePath)
    {
        var result = GetTree().ChangeSceneToFile(scenePath);
        if (result != Error.Ok)
        {
            GD.PushError($"[SceneRouter] Failed to change scene to {scenePath}: {result}");
        }

        return result;
    }
}
