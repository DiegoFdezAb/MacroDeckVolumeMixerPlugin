using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace VolumeMixerPlugin.Utils;

/// <summary>
/// Extracts high-quality icons from Windows executable files using Shell API.
/// Based on Mauricio DIAZ ORLICH's ShellIcon implementation.
/// </summary>
public static class ShellIcon
{
    private const string IID_IImageList = "46EB5926-582E-4017-9FDF-E8998DAA0950";

    #region Shell32 Constants and Imports

    private const int SHIL_JUMBO = 0x4;
    private const int ILD_TRANSPARENT = 0x00000001;
    private const int ILD_IMAGE = 0x00000020;

    [Flags]
    private enum SHGFI : uint
    {
        Icon = 0x000000100,
        SysIconIndex = 0x000004000,
        LargeIcon = 0x000000000,
        UseFileAttributes = 0x000000010,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("shell32.dll", EntryPoint = "#727")]
    private static extern int SHGetImageList(int iImageList, ref Guid riid, ref IImageList ppv);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    #endregion

    #region IImageList Interface

    [ComImport]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IImageList
    {
        [PreserveSig]
        int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
        [PreserveSig]
        int ReplaceIcon(int i, IntPtr hicon, ref int pi);
        [PreserveSig]
        int SetOverlayImage(int iImage, int iOverlay);
        [PreserveSig]
        int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
        [PreserveSig]
        int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
        [PreserveSig]
        int Draw(ref IMAGELISTDRAWPARAMS pimldp);
        [PreserveSig]
        int Remove(int i);
        [PreserveSig]
        int GetIcon(int i, int flags, ref IntPtr picon);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGELISTDRAWPARAMS
    {
        public int cbSize;
        public IntPtr himl;
        public int i;
        public IntPtr hdcDst;
        public int x, y, cx, cy;
        public int xBitmap, yBitmap;
        public int rgbBk, rgbFg;
        public int fStyle;
        public int dwRop;
        public int fState;
        public int Frame;
        public int crEffect;
    }

    #endregion

    /// <summary>
    /// Gets the system icon index for a file path.
    /// </summary>
    private static int GetIconIndex(string filePath)
    {
        var sfi = new SHFILEINFO();
        SHGetFileInfo(
            filePath,
            0,
            ref sfi,
            (uint)Marshal.SizeOf(sfi),
            (uint)(SHGFI.SysIconIndex | SHGFI.LargeIcon | SHGFI.UseFileAttributes));
        return sfi.iIcon;
    }

    /// <summary>
    /// Gets a jumbo (256x256) icon handle from the system image list.
    /// </summary>
    private static IntPtr GetJumboIconHandle(int iconIndex)
    {
        IImageList? imageList = null;
        var guid = new Guid(IID_IImageList);
        SHGetImageList(SHIL_JUMBO, ref guid, ref imageList!);

        if (imageList == null)
            return IntPtr.Zero;

        var hIcon = IntPtr.Zero;
        imageList.GetIcon(iconIndex, ILD_TRANSPARENT | ILD_IMAGE, ref hIcon);
        return hIcon;
    }

    /// <summary>
    /// Extracts the icon from an executable file path as a Bitmap.
    /// Returns a 256x256 jumbo icon if available.
    /// </summary>
    /// <param name="filePath">Path to the executable file.</param>
    /// <returns>Bitmap of the icon, or null if extraction failed.</returns>
    public static Bitmap? ExtractIconFromFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        try
        {
            int iconIndex = GetIconIndex(filePath);
            IntPtr hIcon = GetJumboIconHandle(iconIndex);

            if (hIcon == IntPtr.Zero)
            {
                // Fallback to standard icon extraction
                using var icon = Icon.ExtractAssociatedIcon(filePath);
                return icon?.ToBitmap();
            }

            try
            {
                using var icon = (Icon)Icon.FromHandle(hIcon).Clone();
                return icon.ToBitmap();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
        catch
        {
            // Last resort fallback
            try
            {
                using var icon = Icon.ExtractAssociatedIcon(filePath);
                return icon?.ToBitmap();
            }
            catch
            {
                return null;
            }
        }
    }
}
