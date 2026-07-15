using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NetPdf.Previewer.App.ViewModels;

/// <summary>A rendered page: full-size PNG for the main view and a small PNG for the sidebar.</summary>
public sealed record PageImage(int PageNumber, byte[] ImageBytes, byte[] ThumbnailBytes)
{
    private Avalonia.Media.Imaging.Bitmap? _image;
    private Avalonia.Media.Imaging.Bitmap? _thumbnail;

    /// <summary>Decoded full-size bitmap for the main view.</summary>
    public Avalonia.Media.Imaging.Bitmap Image =>
        _image ??= new Avalonia.Media.Imaging.Bitmap(new MemoryStream(ImageBytes));

    /// <summary>Decoded small bitmap for the thumbnail sidebar.</summary>
    public Avalonia.Media.Imaging.Bitmap Thumbnail =>
        _thumbnail ??= new Avalonia.Media.Imaging.Bitmap(new MemoryStream(ThumbnailBytes));
}

/// <summary>Holds the previewed document and drives rendering, zoom, status, and save.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private const double MinZoom = 0.25;
    private const double MaxZoom = 4.0;
    private const double ZoomStep = 1.25;
    private const int BaseDpi = 96;
    private const int ThumbnailDpi = 24;

    private byte[]? _pdf;

    /// <summary>Rendered pages in document order.</summary>
    public ObservableCollection<PageImage> Pages { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomPercent))]
    private double _zoom = 1.0;

    [ObservableProperty]
    private string _status = "Waiting for document…";

    [ObservableProperty]
    private string? _errorText;

    [ObservableProperty]
    private bool _hasDocument;

    /// <summary>Zoom formatted for the toolbar, e.g. "125%".</summary>
    public string ZoomPercent => $"{Zoom:P0}";

    /// <summary>Renders a freshly received PDF, replacing the current pages.</summary>
    public void LoadDocument(byte[] pdf)
    {
        _pdf = pdf;
        ErrorText = null;
        RenderPages();
        HasDocument = true;
        Status = $"Updated {DateTime.Now:HH:mm:ss} — {Pages.Count} page(s)";
    }

    /// <summary>Shows a generation error pushed by the client; keeps the last good pages.</summary>
    public void ShowError(string message)
    {
        ErrorText = message;
        Status = $"Generation failed {DateTime.Now:HH:mm:ss}";
    }

    /// <summary>Writes the last received PDF to <paramref name="path"/>.</summary>
    public void Save(string path)
    {
        if (_pdf is null)
        {
            throw new InvalidOperationException("No document has been received yet.");
        }

        File.WriteAllBytes(path, _pdf);
        Status = $"Saved to {path}";
    }

    [RelayCommand]
    private void ZoomIn() => SetZoom(Zoom * ZoomStep);

    [RelayCommand]
    private void ZoomOut() => SetZoom(Zoom / ZoomStep);

    /// <summary>Sets zoom so the first page fills the given viewport width.</summary>
    public void FitToWidth(double viewportWidth)
    {
        if (_pdf is null || Pages.Count == 0 || viewportWidth <= 0)
        {
            return;
        }

        using var bitmap = SkiaSharp.SKBitmap.Decode(Pages[0].ImageBytes);
        SetZoom(Zoom * viewportWidth / bitmap.Width);
    }

    private void SetZoom(double zoom)
    {
        var clamped = Math.Clamp(zoom, MinZoom, MaxZoom);
        if (Math.Abs(clamped - Zoom) < 0.001)
        {
            return;
        }

        Zoom = clamped;
        if (_pdf is not null)
        {
            RenderPages();
        }
    }

    private void RenderPages()
    {
        using var document = PdfFile.Open(_pdf!);
        var dpi = Math.Max(1, (int)(BaseDpi * Zoom));

        Pages.Clear();
        for (var i = 0; i < document.PageCount; i++)
        {
            Pages.Add(new PageImage(
                PageNumber: i + 1,
                ImageBytes: document.RenderPage(i, dpi),
                ThumbnailBytes: document.RenderPage(i, ThumbnailDpi)));
        }
    }
}
