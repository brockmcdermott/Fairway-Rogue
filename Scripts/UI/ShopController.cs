using System.Collections.Generic;
using Godot;

public partial class ShopController : Control
{
    private Label? _currencyLabel;
    private Label? _activeBallLabel;
    private OptionButton? _ballSelect;
    private ItemList? _upgradeList;
    private Label? _upgradeDetailsLabel;
    private Label? _statusLabel;
    private Button? _purchaseButton;
    private Button? _continueButton;
    private Button? _mainMenuButton;
    private readonly List<UpgradeData> _visibleUpgrades = new List<UpgradeData>();
    private readonly List<BallData> _visibleBalls = new List<BallData>();
    private bool _isRefreshingBallOptions;

    private GameManager? GameManagerSingleton => AutoloadLocator.Get<GameManager>(this, nameof(GameManager));
    private RunManager? RunManagerSingleton => AutoloadLocator.Get<RunManager>(this, nameof(RunManager));
    private AudioManager? AudioManagerSingleton => AutoloadLocator.Get<AudioManager>(this, nameof(AudioManager));

    public override void _Ready()
    {
        _currencyLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/CurrencyLabel");
        _activeBallLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ActiveBallLabel");
        _ballSelect = GetNodeOrNull<OptionButton>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/BallRow/BallSelect");
        _upgradeList = GetNodeOrNull<ItemList>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/UpgradeList");
        _upgradeDetailsLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/UpgradeDetailsLabel");
        _statusLabel = GetNodeOrNull<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/StatusLabel");
        _purchaseButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/PurchaseButton");
        _continueButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/ContinueButton");
        _mainMenuButton = GetNodeOrNull<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonRow/MainMenuButton");

        if (_ballSelect != null)
        {
            _ballSelect.ItemSelected += OnBallSelected;
        }

        if (_upgradeList != null)
        {
            _upgradeList.ItemSelected += OnUpgradeSelected;
        }

        if (_purchaseButton != null)
        {
            _purchaseButton.Pressed += OnPurchasePressed;
        }

        if (_continueButton != null)
        {
            _continueButton.Pressed += OnContinuePressed;
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.Pressed += OnMainMenuPressed;
        }

        GameManagerSingleton?.ChangeState(GameState.Shop);
        AudioManagerSingleton?.PlayMusic("menu");
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshCurrency();
        RefreshUnlockedBallOptions();
        RefreshUpgradeList();
        RefreshSelectedUpgradeDetails();
    }

    private void RefreshCurrency()
    {
        var run = RunManagerSingleton;
        if (_currencyLabel == null || run == null)
        {
            return;
        }

        _currencyLabel.Text = $"Currency: {run.Currency}";
    }

    private void RefreshUnlockedBallOptions()
    {
        var run = RunManagerSingleton;
        if (_ballSelect == null || _activeBallLabel == null || run == null)
        {
            return;
        }

        _isRefreshingBallOptions = true;
        _visibleBalls.Clear();
        _visibleBalls.AddRange(run.GetUnlockedBalls());

        _ballSelect.Clear();

        var selectedIndex = -1;
        for (var i = 0; i < _visibleBalls.Count; i += 1)
        {
            var ball = _visibleBalls[i];
            _ballSelect.AddItem(ball.Name);
            if (ball.Id == run.CurrentLoadout.ActiveBallId)
            {
                selectedIndex = i;
            }
        }

        if (selectedIndex >= 0)
        {
            _ballSelect.Select(selectedIndex);
        }

        var activeBall = run.GetActiveBallData();
        _activeBallLabel.Text = $"Active Ball: {activeBall.Name}  (Dist x{activeBall.DistanceMultiplier:0.00}, Ctrl x{activeBall.ControlFrictionMultiplier:0.00})";
        _isRefreshingBallOptions = false;
    }

    private void RefreshUpgradeList()
    {
        var run = RunManagerSingleton;
        if (_upgradeList == null || run == null)
        {
            return;
        }

        _visibleUpgrades.Clear();
        _visibleUpgrades.AddRange(run.GetAvailableUpgrades());

        _upgradeList.Clear();
        for (var i = 0; i < _visibleUpgrades.Count; i += 1)
        {
            var upgrade = _visibleUpgrades[i];
            _upgradeList.AddItem($"{upgrade.Name}  -  {upgrade.Cost}");
        }

        if (_visibleUpgrades.Count > 0)
        {
            _upgradeList.Select(0);
        }
    }

    private void OnContinuePressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.ContinueRunToNextHole();
    }

    private void OnMainMenuPressed()
    {
        AudioManagerSingleton?.PlaySfx("ui_click");
        GameManagerSingleton?.GoToMainMenu();
    }

    private void OnPurchasePressed()
    {
        var run = RunManagerSingleton;
        if (run == null || _upgradeList == null)
        {
            return;
        }

        var selected = _upgradeList.GetSelectedItems();
        if (selected.Length == 0)
        {
            SetStatus("Select an upgrade first.");
            return;
        }

        var selectedIndex = selected[0];
        if (selectedIndex < 0 || selectedIndex >= _visibleUpgrades.Count)
        {
            SetStatus("Invalid upgrade selection.");
            return;
        }

        var upgrade = _visibleUpgrades[selectedIndex];
        if (run.TryPurchaseUpgrade(upgrade.Id, out var message))
        {
            GameManagerSingleton?.SaveCurrentRunIfAvailable();
            AudioManagerSingleton?.PlaySfx("shop_purchase");
            SetStatus(message);
            RefreshAll();
            return;
        }

        AudioManagerSingleton?.PlaySfx("ui_click");
        SetStatus(message);
        RefreshSelectedUpgradeDetails();
    }

    private void OnUpgradeSelected(long index)
    {
        RefreshSelectedUpgradeDetails();
    }

    private void OnBallSelected(long index)
    {
        if (_isRefreshingBallOptions)
        {
            return;
        }

        var run = RunManagerSingleton;
        if (run == null)
        {
            return;
        }

        var selectedIndex = (int)index;
        if (selectedIndex < 0 || selectedIndex >= _visibleBalls.Count)
        {
            return;
        }

        var selectedBall = _visibleBalls[selectedIndex];
        if (run.SetActiveBall(selectedBall.Id, out var message))
        {
            GameManagerSingleton?.SaveCurrentRunIfAvailable();
            AudioManagerSingleton?.PlaySfx("ui_click");
            SetStatus(message);
            RefreshUnlockedBallOptions();
            RefreshSelectedUpgradeDetails();
            return;
        }

        SetStatus(message);
    }

    private void RefreshSelectedUpgradeDetails()
    {
        if (_upgradeDetailsLabel == null || _purchaseButton == null)
        {
            return;
        }

        var run = RunManagerSingleton;
        if (run == null)
        {
            _upgradeDetailsLabel.Text = "No run loaded.";
            _purchaseButton.Disabled = true;
            return;
        }

        if (_visibleUpgrades.Count == 0)
        {
            _upgradeDetailsLabel.Text = "All currently available upgrades are purchased.";
            _purchaseButton.Disabled = true;
            return;
        }

        if (_upgradeList == null)
        {
            _purchaseButton.Disabled = true;
            return;
        }

        var selectedItems = _upgradeList.GetSelectedItems();
        var selectedIndex = selectedItems.Length > 0 ? selectedItems[0] : 0;
        if (selectedIndex < 0 || selectedIndex >= _visibleUpgrades.Count)
        {
            selectedIndex = 0;
        }

        var upgrade = _visibleUpgrades[selectedIndex];
        _purchaseButton.Disabled = run.Currency < upgrade.Cost;

        if (upgrade.Kind == UpgradeKind.ClubModifier)
        {
            var current = run.GetEffectiveClubData(upgrade.TargetClubType);
            var projected = run.GetProjectedClubDataWithUpgrade(upgrade.TargetClubType, upgrade);

            _upgradeDetailsLabel.Text =
                $"{upgrade.Name}\n" +
                $"{upgrade.Description}\n" +
                $"Cost: {upgrade.Cost}\n\n" +
                $"Current {current.Name}: Min {current.MinPower:0}, Max {current.MaxPower:0}, Friction {current.FrictionMultiplier:0.00}\n" +
                $"After Upgrade: Min {projected.MinPower:0}, Max {projected.MaxPower:0}, Friction {projected.FrictionMultiplier:0.00}";
            return;
        }

        if (upgrade.Kind == UpgradeKind.BallUnlock)
        {
            var currentBall = run.GetActiveBallData();
            var projectedBall = ProgressionCatalog.GetBall(upgrade.UnlockBallId);

            _upgradeDetailsLabel.Text =
                $"{upgrade.Name}\n" +
                $"{upgrade.Description}\n" +
                $"Cost: {upgrade.Cost}\n\n" +
                $"Current Ball: {currentBall.Name}  (Dist x{currentBall.DistanceMultiplier:0.00}, Ctrl x{currentBall.ControlFrictionMultiplier:0.00})\n" +
                $"Unlocked Ball: {projectedBall.Name}  (Dist x{projectedBall.DistanceMultiplier:0.00}, Ctrl x{projectedBall.ControlFrictionMultiplier:0.00})\n" +
                "Unlocked ball is auto-equipped on purchase.";
            return;
        }

        _upgradeDetailsLabel.Text = $"{upgrade.Name}\n{upgrade.Description}\nCost: {upgrade.Cost}";
    }

    private void SetStatus(string message)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = $"Status: {message}";
        }
    }
}
