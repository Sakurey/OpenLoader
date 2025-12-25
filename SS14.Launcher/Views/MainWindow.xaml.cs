using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SS14.Launcher.ViewModels;
using IDataObject = Avalonia.Input.IDataObject;

namespace SS14.Launcher.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        DarkMode();

#if DEBUG
        this.AttachDevTools();
#endif

        AddHandler(DragDrop.DragEnterEvent, DragEnter);
        AddHandler(DragDrop.DragLeaveEvent, DragLeave);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DropEvent, Drop);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Control = null;
        }

        _viewModel = DataContext as MainWindowViewModel;

        if (_viewModel != null)
        {
            _viewModel.Control = this;
        }

        base.OnDataContextChanged(e);
    }

    private unsafe void DarkMode()
    {
        if (!OperatingSystem.IsWindows() || Environment.OSVersion.Version.Build < 22000)
            return;

        if (TryGetPlatformHandle() is not { HandleDescriptor: "HWND" } handle)
        {
            // No need to log a warning, PJB will notice when this breaks.
            return;
        }

        var hWnd = handle.Handle;

        // Win11: DWMWA_CAPTION_COLOR = 35
        // COLORREF is 0x00BBGGRR
        var captionColor = unchecked((int)0x00262121);
        _ = Win32.DwmSetWindowAttribute(hWnd, Win32.DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

        // Remove top margin of the window on Windows 11, since there's ample space after we recolor the title bar.
        var margin = HeaderPanel.Margin;
        HeaderPanel.Margin = new Thickness(margin.Left, 0, margin.Right, margin.Bottom);
    }

    // ... existing code ...

    private static class Win32
    {
        public const int DWMWA_CAPTION_COLOR = 35;

        [DllImport("dwmapi.dll", ExactSpelling = true)]
        public static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int dwAttribute,
            ref int pvAttribute,
            int cbAttribute);
    }

    // ... existing code ...

    private void Drop(object? sender, DragEventArgs args)
    {
        DragDropOverlay.IsVisible = false;

        if (!IsDragDropValid(args.Data))
            return;

        var file = GetDragDropFile(args.Data)!;
        _viewModel!.Dropped(file);
    }

    private void DragOver(object? sender, DragEventArgs args)
    {
        if (!IsDragDropValid(args.Data))
        {
            args.DragEffects = DragDropEffects.None;
            return;
        }

        args.DragEffects = DragDropEffects.Link;
    }

    private void DragLeave(object? sender, RoutedEventArgs args)
    {
        DragDropOverlay.IsVisible = false;
    }

    private void DragEnter(object? sender, DragEventArgs args)
    {
        if (!IsDragDropValid(args.Data))
            return;

        DragDropOverlay.IsVisible = true;
    }

    private bool IsDragDropValid(IDataObject dataObject)
    {
        if (_viewModel == null)
            return false;

        if (GetDragDropFile(dataObject) is not { } fileName)
            return false;

        return _viewModel.IsContentBundleDropValid(fileName);
    }

    private static IStorageFile? GetDragDropFile(IDataObject dataObject)
    {
        if (!dataObject.Contains(DataFormats.Files))
            return null;

        return dataObject.GetFiles()?.OfType<IStorageFile>().FirstOrDefault();
    }
}
