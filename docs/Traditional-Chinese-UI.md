# Traditional Chinese UI

This branch provides a fixed Traditional Chinese (Taiwan) interface for Text Grab
v4.16.0. It translates settings, main windows, dialogs, menus, tooltips, selected
accessibility descriptions, file-picker descriptions and progress messages.
It does not add a language switcher.

Product/model names, keyboard shortcuts, input examples, regular expressions,
internal AI prompts, diagnostics and user-authored content retain their original
values. Some OS/library-generated messages can still appear in their own language.

## Compatibility

`ButtonInfo.ButtonText` and `ShortcutKeySet.Name` are persisted identifiers. They
remain in their original form. Their non-serialized `DisplayText` and `DisplayName`
properties use `UiText` for presentation, including previously saved settings.
Custom labels absent from the translation map are displayed unchanged.

The Direct Text traversal selector translates the item template while retaining
the original enum in `SelectedItem`. The dynamic pattern menu uses a stable tag
instead of comparing the translated header. CLI arguments remain in English.

No OCR model, Windows AI architecture check, package identity or default setting
was changed. On x64, the original architecture override is still required for
Windows AI OCR.

## Build

Use Visual Studio MSBuild with the Windows SDK and .NET 10 installed:

```powershell
MSBuild.exe Text-Grab-Package/Text-Grab-Package.wapproj /restore `
  /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 `
  /p:OutputPath=bin\x64\ZhTwRelease\ /p:AppxBundle=Never `
  /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxPackageSigningEnabled=false
```

The MSIX is unsigned. Development registration can use the resulting
`Text-Grab-Package/bin/x64/ZhTwRelease/AppxManifest.xml` with Developer Mode already
enabled. Keep that layout in place while a development registration uses it.

## Validation

- XAML identifiers, commands, event handlers and tags were compared with the base
  version; translated values retain their format placeholders.
- The app test suite ran 618 tests: 611 passed, 3 skipped, 4 failed. A separate
  unmodified v4.16.0 checkout reproduced the same four OCR failures:
  `OcrFontTestImage`, `OcrFontSampleImage`, `ParagraphWrapDetection`, `ReadQrCode`.
- New tests verify that translating action/shortcut labels preserves serialized
  identity and custom labels. Existing UI expectations were updated to Chinese.
