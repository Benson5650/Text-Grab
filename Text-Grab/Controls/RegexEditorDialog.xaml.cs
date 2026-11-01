using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using Text_Grab.Models;
using Wpf.Ui.Controls;

namespace Text_Grab.Controls;

public partial class RegexEditorDialog : FluentWindow
{
    public StoredRegex? EditedRegex { get; private set; }
    private StoredRegex? _originalRegex;

    public RegexEditorDialog()
    {
        InitializeComponent();
        _originalRegex = null;
        RegexReferenceList.ItemsSource = BuildRegexReference();
    }

    public RegexEditorDialog(StoredRegex regexToEdit)
    {
        InitializeComponent();
        RegexReferenceList.ItemsSource = BuildRegexReference();
        _originalRegex = regexToEdit;

        // Populate fields
        NameTextBox.Text = regexToEdit.Name;
        PatternTextBox.Text = regexToEdit.Pattern;
        DescriptionTextBox.Text = regexToEdit.Description;

        Title = "編輯規則運算式";
        ValidateInput(null, null);
    }

    private void ValidateInput(object? sender, System.Windows.Controls.TextChangedEventArgs? e)
    {
        bool isValid = true;
        string errorMessage = string.Empty;

        // Validate name
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            isValid = false;
            errorMessage = "必須輸入名稱";
        }
        // Validate pattern
        else if (string.IsNullOrWhiteSpace(PatternTextBox.Text))
        {
            isValid = false;
            errorMessage = "必須輸入規則運算式";
        }
        else
        {
            // Test if pattern is valid regex
            try
            {
                _ = new Regex(PatternTextBox.Text);
            }
            catch (ArgumentException)
            {
                isValid = false;
                errorMessage = "規則運算式無效";
            }
        }

        SaveButton.IsEnabled = isValid;

        if (!isValid && !string.IsNullOrEmpty(errorMessage))
        {
            ErrorText.Text = errorMessage;
            ErrorText.Visibility = Visibility.Visible;
        }
        else
        {
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_originalRegex is not null)
        {
            // Editing existing pattern
            EditedRegex = new StoredRegex
            {
                Id = _originalRegex.Id,
                Name = NameTextBox.Text.Trim(),
                Pattern = PatternTextBox.Text.Trim(),
                Description = DescriptionTextBox.Text.Trim(),
                IsDefault = _originalRegex.IsDefault,
                CreatedDate = _originalRegex.CreatedDate,
                LastUsedDate = _originalRegex.LastUsedDate
            };
        }
        else
        {
            // Creating new pattern
            EditedRegex = new StoredRegex
            {
                Name = NameTextBox.Text.Trim(),
                Pattern = PatternTextBox.Text.Trim(),
                Description = DescriptionTextBox.Text.Trim(),
                IsDefault = false
            };
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Inserts the clicked reference token into the pattern box at the current caret
    /// position (replacing any selection), then returns focus to the pattern box.
    /// </summary>
    private void InsertToken_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Wpf.Ui.Controls.Button { Tag: string token } || string.IsNullOrEmpty(token))
            return;

        int caret = PatternTextBox.CaretIndex;
        if (PatternTextBox.SelectionLength > 0)
        {
            caret = PatternTextBox.SelectionStart;
            PatternTextBox.SelectedText = token;
        }
        else
        {
            PatternTextBox.Text = PatternTextBox.Text.Insert(caret, token);
        }

        PatternTextBox.CaretIndex = caret + token.Length;
        PatternTextBox.Focus();
    }

    private static List<RegexReferenceCategory> BuildRegexReference() =>
    [
        new("字元類別",
        [
            new(@"\d", "任一數字（0–9）"),
            new(@"\D", "任一非數字字元"),
            new(@"\w", "單字字元：字母、數字或底線"),
            new(@"\W", "任一非單字字元"),
            new(@"\s", "任一空白字元（空格、定位字元、換行）"),
            new(@"\S", "任一非空白字元"),
            new(".", "任一字元（換行除外）"),
            new("[abc]", "清單中的任一字元"),
            new("[^abc]", "清單以外的任一字元"),
            new("[a-z]", "a 至 z 範圍內的任一字元"),
        ]),
        new("Anchors & boundaries",
        [
            new("^", "行／字串開頭"),
            new("$", "行／字串結尾"),
            new(@"\b", "單字邊界（單字邊緣）"),
            new(@"\B", "非單字邊界"),
        ]),
        new("Quantifiers",
        [
            new("*", "前一項目出現零次或多次"),
            new("+", "前一項目出現一次或多次"),
            new("?", "零次或一次（設為選用）"),
            new("{3}", "前一項目恰好出現 3 次"),
            new("{2,}", "前一項目出現 2 次以上"),
            new("{2,5}", "前一項目出現 2 到 5 次"),
            new("*?", "非貪婪：盡可能少（也適用於 +? 和 ??）"),
        ]),
        new("Groups & alternation",
        [
            new("(...)", "擷取群組－保留符合結果"),
            new("(?:...)", "不擷取的群組"),
            new("(?<name>...)", "具名擷取群組"),
            new("a|b", "符合 a 或 b"),
        ]),
        new("環視（比對上下文但不消耗文字）",
        [
            new("(?=...)", "向前檢查：後面接著……（符合項目後的文字）"),
            new("(?!...)", "負向向前檢查：後面不接著……"),
            new("(?<=...)", "向後檢查：前面接著……（符合項目前的文字）"),
            new("(?<!...)", "負向向後檢查：前面不接著……"),
        ]),
        new("Escapes & literals",
        [
            new(@"\.", "句點字元（使用 \\ 跳脫特殊字元）"),
            new(@"\\", "反斜線字元"),
            new(@"\n", "Newline"),
            new(@"\t", "Tab"),
        ]),
    ];

    /// <summary>A named group of regex reference rows shown in the quick-reference expander.</summary>
    public sealed record RegexReferenceCategory(string CategoryName, IReadOnlyList<RegexReferenceItem> Items);

    /// <summary>A single regex token and a plain-language description of what it does.</summary>
    public sealed record RegexReferenceItem(string Token, string Description);
}
