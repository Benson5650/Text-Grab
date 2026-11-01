using System;
using System.Windows;

namespace Text_Grab.Models;

public record WindowSelectionCandidate(IntPtr Handle, Rect Bounds, string Title, int ProcessId, string AppName = "")
{
    public bool Contains(Point point) => Bounds.Contains(point);

    public string DisplayAppName => string.IsNullOrWhiteSpace(AppName) ? "應用程式" : AppName;

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "未命名視窗" : Title;
}
