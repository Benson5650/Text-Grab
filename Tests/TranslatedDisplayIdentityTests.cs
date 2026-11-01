using System.Text.Json;
using Text_Grab.Models;

namespace Tests;

public class TranslatedDisplayIdentityTests
{
    [Fact]
    public void ActionDisplayTranslationPreservesSavedIdentityAndCustomLabels()
    {
        var button = new ButtonInfo { ButtonText = "Copy and Close", ClickEvent = "CopyAndClose_Click" };
        Assert.Equal("複製並關閉", button.DisplayText);
        string json = JsonSerializer.Serialize(button);
        Assert.DoesNotContain("DisplayText", json);
        var restored = JsonSerializer.Deserialize<ButtonInfo>(json)!;
        Assert.Equal("Copy and Close", restored.ButtonText);
        Assert.Equal(button.ClickEvent, restored.ClickEvent);
        Assert.Equal(button, restored);

        restored.ButtonText = "My custom action 123";
        Assert.Equal("My custom action 123", restored.DisplayText);
    }

    [Fact]
    public void ShortcutDisplayTranslationPreservesSavedNameAndAction()
    {
        var shortcut = new ShortcutKeySet { Name = "Fullscreen Grab", Action = ShortcutKeyActions.Fullscreen };
        Assert.Equal("全螢幕擷取", shortcut.DisplayName);
        string json = JsonSerializer.Serialize(shortcut);
        Assert.DoesNotContain("DisplayName", json);
        var restored = JsonSerializer.Deserialize<ShortcutKeySet>(json)!;
        Assert.Equal(shortcut.Name, restored.Name);
        Assert.Equal(shortcut.Action, restored.Action);
    }
}
