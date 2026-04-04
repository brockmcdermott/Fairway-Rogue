using Godot;

public partial class ShopController : Control
{
    private Label? _currencyLabel;
    private Button? _continueButton;
    private Button? _mainMenuButton;

    private GameManager? GameManagerSingleton => GetNodeOrNull<GameManager>("/root/GameManager");
    private RunManager? RunManagerSingleton => GetNodeOrNull<RunManager>("/root/RunManager");
    private SceneRouter? SceneRouterSingleton => GetNodeOrNull<SceneRouter>("/root/SceneRouter");

    public override void _Ready()
    {
        _currencyLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/CurrencyLabel");
        _continueButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/ContinueButton");
        _mainMenuButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/MainMenuButton");

        if (_continueButton != null)
        {
            _continueButton.Pressed += OnContinuePressed;
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.Pressed += OnMainMenuPressed;
        }

        GameManagerSingleton?.ChangeState(GameState.Shop);
        RefreshCurrency();
    }

    private void RefreshCurrency()
    {
        if (_currencyLabel != null)
        {
            var currency = RunManagerSingleton?.Currency ?? 0;
            _currencyLabel.Text = $"Currency: {currency}";
        }
    }

    private void OnContinuePressed()
    {
        GameManagerSingleton?.ChangeState(GameState.LoadingHole);
        SceneRouterSingleton?.GoToGameplay();
        GameManagerSingleton?.ChangeState(GameState.InHole);
    }

    private void OnMainMenuPressed()
    {
        GameManagerSingleton?.GoToMainMenu();
    }
}
