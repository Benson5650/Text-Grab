using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Text_Grab.Controls;
using Text_Grab.Models;
using Text_Grab.Properties;
using Text_Grab.Utilities;

namespace Text_Grab.Pages;
/// <summary>
/// Interaction logic for FullscreenGrabSettings.xaml
/// </summary>
public partial class FullscreenGrabSettings : Page
{
    private readonly Settings DefaultSettings = AppUtilities.TextGrabSettings;
    private bool _loaded = false;

    public FullscreenGrabSettings()
    {
        InitializeComponent();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // hydrate controls from settings
        SendToEtwCheckBox.IsChecked = DefaultSettings.FsgSendEtwToggle;
        TryInsertCheckBox.IsChecked = DefaultSettings.TryInsert;

        ShadeOverlayCheckBox.IsChecked = DefaultSettings.FsgShadeOverlay;

        InsertDelaySlider.Value = Math.Clamp(DefaultSettings.InsertDelay, InsertDelaySlider.Minimum, InsertDelaySlider.Maximum);
        InsertDelayValueText.Text = DefaultSettings.InsertDelay.ToString("0.0", CultureInfo.InvariantCulture);

        InsertDelaySlider.IsEnabled = TryInsertCheckBox.IsChecked == true;

        // Determine default mode
        FsgDefaultMode mode = FsgDefaultMode.Default;
        if (!string.IsNullOrWhiteSpace(DefaultSettings.FsgDefaultMode))
            Enum.TryParse(DefaultSettings.FsgDefaultMode, true, out mode);

        if (mode == FsgDefaultMode.Table)
        {
            TableModeRadio.IsChecked = true;
        }
        else if (DefaultSettings.FSGMakeSingleLineToggle)
        {
            SingleLineModeRadio.IsChecked = true;
        }
        else
        {
            DefaultModeRadio.IsChecked = true;
        }

        FsgSelectionStyle selectionStyle = FsgSelectionStyle.Region;
        if (!string.IsNullOrWhiteSpace(DefaultSettings.FsgSelectionStyle))
            Enum.TryParse(DefaultSettings.FsgSelectionStyle, true, out selectionStyle);

        switch (selectionStyle)
        {
            case FsgSelectionStyle.Window:
                WindowSelectionStyleRadio.IsChecked = true;
                break;
            case FsgSelectionStyle.Freeform:
                FreeformSelectionStyleRadio.IsChecked = true;
                break;
            case FsgSelectionStyle.AdjustAfter:
                AdjustAfterSelectionStyleRadio.IsChecked = true;
                break;
            case FsgSelectionStyle.Region:
            default:
                RegionSelectionStyleRadio.IsChecked = true;
                break;
        }

        // Update post-grab actions count
        UpdateActionsCountText();

        _loaded = true;
    }

    private void DefaultModeRadio_Click(object sender, RoutedEventArgs e)
    {
        if (!_loaded || DefaultModeRadio.IsChecked != true) return;
        DefaultSettings.FsgDefaultMode = nameof(FsgDefaultMode.Default);
        DefaultSettings.FSGMakeSingleLineToggle = false;
        DefaultSettings.Save();
    }

    private void SingleLineModeRadio_Click(object sender, RoutedEventArgs e)
    {
        if (!_loaded || SingleLineModeRadio.IsChecked != true) return;
        // SingleLine uses the legacy flag, FsgDefaultMode stays Default
        DefaultSettings.FSGMakeSingleLineToggle = true;
        DefaultSettings.FsgDefaultMode = nameof(FsgDefaultMode.Default);
        DefaultSettings.Save();
    }

    private void TableModeRadio_Click(object sender, RoutedEventArgs e)
    {
        if (!_loaded || TableModeRadio.IsChecked != true) return;
        DefaultSettings.FsgDefaultMode = nameof(FsgDefaultMode.Table);
        DefaultSettings.FSGMakeSingleLineToggle = false;
        DefaultSettings.Save();
    }

    private void RegionSelectionStyleRadio_Click(object sender, RoutedEventArgs e)
    {
        SaveSelectionStyle(RegionSelectionStyleRadio, FsgSelectionStyle.Region);
    }

    private void WindowSelectionStyleRadio_Click(object sender, RoutedEventArgs e)
    {
        SaveSelectionStyle(WindowSelectionStyleRadio, FsgSelectionStyle.Window);
    }

    private void FreeformSelectionStyleRadio_Click(object sender, RoutedEventArgs e)
    {
        SaveSelectionStyle(FreeformSelectionStyleRadio, FsgSelectionStyle.Freeform);
    }

    private void AdjustAfterSelectionStyleRadio_Click(object sender, RoutedEventArgs e)
    {
        SaveSelectionStyle(AdjustAfterSelectionStyleRadio, FsgSelectionStyle.AdjustAfter);
    }

    private void SendToEtwCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        DefaultSettings.FsgSendEtwToggle = SendToEtwCheckBox.IsChecked == true;
        DefaultSettings.Save();
    }

    private void TryInsertCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        bool enabled = TryInsertCheckBox.IsChecked == true;
        DefaultSettings.TryInsert = enabled;
        DefaultSettings.Save();
        InsertDelaySlider.IsEnabled = enabled;
    }

    private void ShadeOverlayCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        bool isChecked = sender is System.Windows.Controls.Primitives.ToggleButton cb && cb.IsChecked == true;
        DefaultSettings.FsgShadeOverlay = isChecked;
        DefaultSettings.Save();
    }

    private void InsertDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_loaded) return;
        double newVal = Math.Round(InsertDelaySlider.Value, 1);
        DefaultSettings.InsertDelay = newVal;
        DefaultSettings.Save();
        InsertDelayValueText.Text = newVal.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private void CustomizeActionsButton_Click(object sender, RoutedEventArgs e)
    {
        PostGrabActionEditor editor = new()
        {
            Owner = Window.GetWindow(this)
        };

        bool? result = editor.ShowDialog();

        if (result == true)
        {
            // Update the count text after changes
            UpdateActionsCountText();
        }
    }

    private void ManageTemplatesButton_Click(object sender, RoutedEventArgs e)
    {
        PostGrabActionEditor editor = new()
        {
            Owner = Window.GetWindow(this)
        };

        // ShowDialog returns true only on Save — update count regardless for consistency
        editor.ShowDialog();
        UpdateActionsCountText();
    }

    private void UpdateActionsCountText()
    {
        List<ButtonInfo> enabledActions = PostGrabActionManager.GetEnabledPostGrabActions();
        int count = enabledActions.Count;

        if (count == 0)
        {
            ActionsCountText.Text = "未啟用任何動作";
        }
        else if (count == 1)
        {
            ActionsCountText.Text = $"已啟用 1 個動作：{enabledActions.First().DisplayText}";
        }
        else
        {
            string actionsList = string.Join(", ", enabledActions.Take(3).Select(a => a.DisplayText));
            if (count > 3)
                actionsList += $", and {count - 3} more";

            ActionsCountText.Text = $"已啟用 {count} 個動作：{actionsList}";
        }
    }

    private void SaveSelectionStyle(RadioButton radioButton, FsgSelectionStyle selectionStyle)
    {
        if (!_loaded || radioButton.IsChecked != true)
            return;

        DefaultSettings.FsgSelectionStyle = selectionStyle.ToString();
        DefaultSettings.Save();
    }
}
