using System.Collections.Generic;
using Godot;

public static class PixelUiStyler
{
    private static Texture2D? _menuBackdropTexture;

    private static readonly Color MenuBackdropDark = new Color(0.09f, 0.19f, 0.15f);
    private static readonly Color MenuBackdropLight = new Color(0.12f, 0.26f, 0.20f);
    private static readonly Color PanelFill = new Color(0.10f, 0.13f, 0.12f, 0.90f);
    private static readonly Color PanelBorder = new Color(0.40f, 0.57f, 0.37f, 1.0f);
    private static readonly Color AccentButton = new Color(0.90f, 0.40f, 0.07f);
    private static readonly Color AccentButtonHover = new Color(0.96f, 0.52f, 0.11f);
    private static readonly Color AccentButtonPressed = new Color(0.82f, 0.30f, 0.04f);
    private static readonly Color DisabledFill = new Color(0.35f, 0.35f, 0.35f);
    private static readonly Color TextPrimary = new Color(0.94f, 0.96f, 0.90f);
    private static readonly Color TextSecondary = new Color(0.81f, 0.86f, 0.76f);
    private static readonly Color OutlineColor = new Color(0.04f, 0.06f, 0.05f, 0.95f);

    public static void ApplyMenuStyle(Control root)
    {
        EnsureBackdrop(root);
        ApplySharedStyle(root);
    }

    public static void ApplyOverlayStyle(Control root)
    {
        ApplySharedStyle(root);
    }

    public static void ApplyHudStyle(Control root)
    {
        ApplySharedStyle(root);
        ApplyHudStylePass(root);
    }

    public static void StylePanelContainer(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", BuildPanelStyle());
    }

    private static void ApplySharedStyle(Control root)
    {
        var queue = new Queue<Node>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            for (var i = 0; i < current.GetChildCount(); i += 1)
            {
                if (current.GetChild(i) is Node childNode)
                {
                    queue.Enqueue(childNode);
                }
            }

            switch (current)
            {
                case PanelContainer panel:
                    panel.AddThemeStyleboxOverride("panel", BuildPanelStyle());
                    break;
                case OptionButton optionButton:
                    ApplyButtonStyle(optionButton);
                    ApplyOptionButtonStyle(optionButton);
                    break;
                case Button button:
                    ApplyButtonStyle(button);
                    break;
                case Label label:
                    ApplyLabelStyle(label);
                    break;
                case ItemList itemList:
                    ApplyItemListStyle(itemList);
                    break;
                case ScrollContainer scrollContainer:
                    scrollContainer.AddThemeStyleboxOverride("panel", BuildPanelStyle(alpha: 0.82f));
                    break;
                case ProgressBar progressBar:
                    ApplyProgressBarStyle(progressBar);
                    break;
            }
        }
    }

    private static void ApplyButtonStyle(Button button)
    {
        button.AddThemeStyleboxOverride("normal", BuildButtonStyle(AccentButton, new Color(0.20f, 0.10f, 0.03f)));
        button.AddThemeStyleboxOverride("hover", BuildButtonStyle(AccentButtonHover, new Color(0.24f, 0.12f, 0.04f)));
        button.AddThemeStyleboxOverride("pressed", BuildButtonStyle(AccentButtonPressed, new Color(0.18f, 0.08f, 0.03f)));
        button.AddThemeStyleboxOverride("disabled", BuildButtonStyle(DisabledFill, new Color(0.18f, 0.18f, 0.18f)));
        button.AddThemeStyleboxOverride("focus", BuildFocusStyle());
        button.AddThemeColorOverride("font_color", TextPrimary);
        button.AddThemeColorOverride("font_hover_color", TextPrimary);
        button.AddThemeColorOverride("font_pressed_color", TextPrimary);
        button.AddThemeColorOverride("font_focus_color", TextPrimary);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.78f, 0.78f, 0.78f));
        button.AddThemeColorOverride("font_outline_color", OutlineColor);
        button.AddThemeConstantOverride("outline_size", 1);
        button.AddThemeConstantOverride("h_separation", 4);
        button.CustomMinimumSize = new Vector2(Mathf.Max(button.CustomMinimumSize.X, 84.0f), Mathf.Max(button.CustomMinimumSize.Y, 30.0f));
    }

    private static void ApplyLabelStyle(Label label)
    {
        var lowerName = label.Name.ToString().ToLowerInvariant();
        var secondary = lowerName.Contains("subtitle") || lowerName.Contains("status");
        label.AddThemeColorOverride("font_color", secondary ? TextSecondary : TextPrimary);
        label.AddThemeColorOverride("font_outline_color", OutlineColor);
        label.AddThemeConstantOverride("outline_size", 1);

        if (lowerName.Contains("title"))
        {
            label.AddThemeFontSizeOverride("font_size", 24);
            label.AddThemeColorOverride("font_color", new Color(0.98f, 0.96f, 0.84f));
        }
        else if (lowerName.Contains("subtitle"))
        {
            label.AddThemeFontSizeOverride("font_size", 18);
        }
        else if (lowerName.Contains("label"))
        {
            label.AddThemeFontSizeOverride("font_size", 16);
        }
    }

    private static void ApplyItemListStyle(ItemList itemList)
    {
        itemList.AddThemeStyleboxOverride("panel", BuildPanelStyle(alpha: 0.86f));
        itemList.AddThemeStyleboxOverride("focus", BuildFocusStyle());
        itemList.AddThemeColorOverride("font_color", TextPrimary);
        itemList.AddThemeColorOverride("font_hovered_color", TextPrimary);
        itemList.AddThemeColorOverride("font_selected_color", TextPrimary);
        itemList.AddThemeColorOverride("guide_color", new Color(0.31f, 0.42f, 0.33f));
    }

    private static void ApplyOptionButtonStyle(OptionButton optionButton)
    {
        var popup = optionButton.GetPopup();
        if (popup == null)
        {
            return;
        }

        popup.AddThemeStyleboxOverride("panel", BuildPanelStyle(alpha: 0.94f));
        popup.AddThemeColorOverride("font_color", TextPrimary);
        popup.AddThemeColorOverride("font_hover_color", TextPrimary);
        popup.AddThemeColorOverride("font_disabled_color", new Color(0.72f, 0.72f, 0.72f));
        popup.AddThemeColorOverride("font_outline_color", OutlineColor);
        popup.AddThemeConstantOverride("outline_size", 1);
    }

    private static void ApplyProgressBarStyle(ProgressBar progressBar)
    {
        var background = new StyleBoxFlat
        {
            BgColor = new Color(0.10f, 0.13f, 0.13f, 1.0f),
            BorderColor = new Color(0.36f, 0.46f, 0.38f, 1.0f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusBottomLeft = 0
        };

        var fill = new StyleBoxFlat
        {
            BgColor = new Color(0.95f, 0.56f, 0.08f, 1.0f),
            BorderColor = new Color(0.63f, 0.28f, 0.04f, 1.0f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusBottomLeft = 0
        };

        progressBar.AddThemeStyleboxOverride("background", background);
        progressBar.AddThemeStyleboxOverride("fill", fill);

        progressBar.AddThemeColorOverride("font_color", TextPrimary);
        progressBar.AddThemeColorOverride("font_outline_color", OutlineColor);
        progressBar.AddThemeConstantOverride("outline_size", 1);
    }

    private static StyleBoxFlat BuildPanelStyle(float alpha = 0.90f)
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(PanelFill.R, PanelFill.G, PanelFill.B, alpha),
            BorderColor = PanelBorder,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusBottomLeft = 0,
            ShadowColor = new Color(0, 0, 0, 0.26f),
            ShadowSize = 2
        };
    }

    private static StyleBoxFlat BuildButtonStyle(Color fill, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 0,
            CornerRadiusTopRight = 0,
            CornerRadiusBottomRight = 0,
            CornerRadiusBottomLeft = 0,
            ShadowColor = new Color(0, 0, 0, 0.24f),
            ShadowSize = 1
        };
    }

    private static StyleBoxFlat BuildFocusStyle()
    {
        return new StyleBoxFlat
        {
            DrawCenter = false,
            BorderColor = new Color(1.0f, 0.93f, 0.63f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1
        };
    }

    private static void EnsureBackdrop(Control root)
    {
        if (root.GetNodeOrNull<TextureRect>("PixelBackdrop") != null)
        {
            return;
        }

        var backdrop = new TextureRect
        {
            Name = "PixelBackdrop",
            Texture = GetMenuBackdropTexture(),
            StretchMode = TextureRect.StretchModeEnum.Tile,
            TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SelfModulate = new Color(1, 1, 1, 0.95f)
        };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(backdrop);
        root.MoveChild(backdrop, 0);
    }

    private static Texture2D GetMenuBackdropTexture()
    {
        if (_menuBackdropTexture != null)
        {
            return _menuBackdropTexture;
        }

        var image = Image.CreateEmpty(24, 24, false, Image.Format.Rgba8);
        image.Fill(MenuBackdropDark);

        for (var y = 0; y < image.GetHeight(); y += 1)
        {
            for (var x = 0; x < image.GetWidth(); x += 1)
            {
                if (((x + y) & 3) == 0)
                {
                    image.SetPixel(x, y, MenuBackdropLight);
                }
                else if ((y & 7) == 0)
                {
                    image.SetPixel(x, y, new Color(MenuBackdropLight.R, MenuBackdropLight.G, MenuBackdropLight.B, 0.72f));
                }
            }
        }

        _menuBackdropTexture = ImageTexture.CreateFromImage(image);
        return _menuBackdropTexture;
    }

    private static void ApplyHudStylePass(Control root)
    {
        var queue = new Queue<Node>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            for (var i = 0; i < current.GetChildCount(); i += 1)
            {
                if (current.GetChild(i) is Node childNode)
                {
                    queue.Enqueue(childNode);
                }
            }

            switch (current)
            {
                case PanelContainer panel:
                    panel.AddThemeStyleboxOverride("panel", BuildPanelStyle(alpha: 0.80f));
                    break;
                case Label label:
                    ApplyHudLabelSize(label);
                    break;
                case Button button:
                    button.CustomMinimumSize = new Vector2(
                        Mathf.Max(64.0f, button.CustomMinimumSize.X),
                        Mathf.Max(24.0f, button.CustomMinimumSize.Y));
                    button.AddThemeFontSizeOverride("font_size", 13);
                    break;
                case ProgressBar progressBar:
                    progressBar.AddThemeFontSizeOverride("font_size", 12);
                    break;
            }
        }
    }

    private static void ApplyHudLabelSize(Label label)
    {
        var lowered = label.Name.ToString().ToLowerInvariant();
        if (lowered.Contains("title"))
        {
            label.AddThemeFontSizeOverride("font_size", 20);
            return;
        }

        if (lowered.Contains("status") || lowered.Contains("wind") || lowered.Contains("shotprofile"))
        {
            label.AddThemeFontSizeOverride("font_size", 12);
            return;
        }

        label.AddThemeFontSizeOverride("font_size", 13);
    }
}
