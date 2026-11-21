using NetPdf.Fluent;
using NetPdf.Layout;

// Same namespaces as the types being extended, so callers need no extra "using".
namespace NetPdf.Layout
{
    /// <summary>SVG loading for <see cref="ImageSource"/>, provided by NetPdfKit.Svg.</summary>
    public static class ImageSourceSvgExtensions
    {
        extension(ImageSource)
        {
            /// <summary>
            /// Rasterizes SVG markup to a PNG at <paramref name="scale"/> times the SVG's intrinsic
            /// size (default 2× for crisp print output). The result is a raster image, not vector
            /// artwork; raise the scale for higher fidelity. Reuse the returned instance to embed the
            /// bitmap only once.
            /// </summary>
            public static ImageSource FromSvg(string markup, double scale = 2)
            {
                ArgumentException.ThrowIfNullOrEmpty(markup);
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scale);

                using var svg = new global::Svg.Skia.SKSvg();
                try
                {
                    if (svg.FromSvg(markup) is null || svg.Picture is null)
                        throw new ArgumentException("The SVG markup could not be parsed.", nameof(markup));
                }
                catch (System.Xml.XmlException e)
                {
                    throw new ArgumentException("The SVG markup could not be parsed.", nameof(markup), e);
                }

                var bounds = svg.Picture.CullRect;
                var width = (int)Math.Ceiling(bounds.Width * scale);
                var height = (int)Math.Ceiling(bounds.Height * scale);
                if (width <= 0 || height <= 0)
                    throw new ArgumentException("The SVG has no drawable area.", nameof(markup));

                using var bitmap = new SkiaSharp.SKBitmap(width, height);
                using (var skCanvas = new SkiaSharp.SKCanvas(bitmap))
                {
                    skCanvas.Clear(SkiaSharp.SKColors.Transparent);
                    skCanvas.Scale((float)scale);
                    skCanvas.DrawPicture(svg.Picture);
                }

                using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, quality: 100);
                return ImageSource.FromBytes(data.ToArray());
            }

            /// <summary>Rasterizes an SVG file to a PNG; see <see cref="FromSvg"/> for the caveats.</summary>
            public static ImageSource FromSvgFile(string path, double scale = 2)
            {
                ArgumentException.ThrowIfNullOrEmpty(path);
                return FromSvg(File.ReadAllText(path), scale);
            }
        }
    }
}

namespace NetPdf.Fluent
{
    /// <summary>SVG placement for <see cref="ContainerDescriptor"/>, provided by NetPdfKit.Svg.</summary>
    public static class ContainerDescriptorSvgExtensions
    {
        /// <summary>
        /// Places an SVG in the slot, rasterized to a PNG at <paramref name="scale"/>× its intrinsic
        /// size and scaled to the available width like an image. Output is raster, not vector.
        /// </summary>
        public static void Svg(this ContainerDescriptor container, string markup, double scale = 2)
        {
            ArgumentNullException.ThrowIfNull(container);
            container.Image(ImageSource.FromSvg(markup, scale));
        }
    }
}
