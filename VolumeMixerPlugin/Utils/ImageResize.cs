using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace VolumeMixerPlugin.Utils;

public static class ImageResize
{
    public static Bitmap Resize(Bitmap source, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        destImage.SetResolution(source.HorizontalResolution, source.VerticalResolution);

        using var graphics = Graphics.FromImage(destImage);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using var wrapMode = new ImageAttributes();
        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
        graphics.DrawImage(source, destRect, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, wrapMode);

        return destImage;
    }
}
