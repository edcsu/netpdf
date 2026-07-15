using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NetPdf.Previewer.App.ViewModels;

namespace NetPdf.Previewer.App.Views;

/// <summary>Main previewer window: toolbar, thumbnail sidebar, page view, status bar.</summary>
public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly PreviewServer _server;

    /// <summary>Designer-only constructor.</summary>
    public MainWindow() : this(12500)
    {
    }

    /// <summary>Creates the window and starts the preview server on <paramref name="port"/>.</summary>
    public MainWindow(int port)
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = _viewModel;

        _server = new PreviewServer(port);
        _server.DocumentReceived += pdf => Dispatcher.UIThread.Post(() => _viewModel.LoadDocument(pdf));
        _server.ErrorReceived += message => Dispatcher.UIThread.Post(() => _viewModel.ShowError(message));
        _server.Start();

        Closed += (_, _) => _server.Dispose();
    }

    private void OnFitWidth(object? sender, RoutedEventArgs e)
    {
        var scroller = this.FindControl<ScrollViewer>("PageScroller");
        if (scroller is not null)
        {
            // Leave room for page margins and the scrollbar.
            _viewModel.FitToWidth(scroller.Bounds.Width - 40);
        }
    }

    private async void OnSave(object? sender, RoutedEventArgs e)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save PDF",
            SuggestedFileName = "document.pdf",
            DefaultExtension = "pdf",
            FileTypeChoices = [new FilePickerFileType("PDF document") { Patterns = ["*.pdf"] }],
        });

        if (file?.TryGetLocalPath() is { } path)
        {
            _viewModel.Save(path);
        }
    }

    private void OnThumbnailSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox { SelectedIndex: >= 0 and var index })
        {
            return;
        }

        var list = this.FindControl<ItemsControl>("PageList");
        if (list?.ContainerFromIndex(index) is { } container)
        {
            container.BringIntoView();
        }
    }
}
