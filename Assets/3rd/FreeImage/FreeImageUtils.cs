#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using FreeImageAPI;
using System;
using System.Runtime.InteropServices;

public class FreeImageUtils
{
    public static void GetTextureSize(string path, out int width, out int height)
    {
        FREE_IMAGE_FORMAT format = FreeImage.GetFileType(path, 0);
        FIBITMAP dib = FreeImage.Load(format, path, FREE_IMAGE_LOAD_FLAGS.DEFAULT);

        width = (int)FreeImage.GetWidth(dib);
        height = (int)FreeImage.GetHeight(dib);
        FreeImage.Unload(dib);
    }

    static Color[] LoadTextureColors(FIBITMAP dib, out int width, out int height)
    {
        width = (int)FreeImage.GetWidth(dib);
        height = (int)FreeImage.GetHeight(dib);
        uint bpp = FreeImage.GetBPP(dib) / 8;
        IntPtr ptr = FreeImage.GetBits(dib);
        Color[] colors = null;
        switch (bpp)
        {
            case 1:
                {
                    colors = new Color[width * height];
                    for (int i = 0; i < width * height; ++i)
                    {
                        byte a = Marshal.ReadByte((IntPtr)(ptr.ToInt64() + i * bpp));
                        colors[i] = (Color)(new Color32(0, 0, 0, a));
                    }
                }
                break;
            case 2:
            case 3:
            case 4:
                {
                    colors = new Color[width * height];
                    byte[] c = new byte[bpp];
                    for (int i = 0; i < width * height; ++i)
                    {
                        Marshal.Copy((IntPtr)(ptr.ToInt64() + i * bpp), c, 0, (int)bpp);
                        byte b = c[0];
                        byte g = c[1];
                        byte r = bpp > 2 ? c[2] : (byte)255;
                        byte a = bpp > 3 ? c[3] : (byte)255;
                        colors[i] = (Color)(new Color32(r, g, b, a));
                    }
                }
                break;
            case 12:
            case 16:
                {
                    colors = new Color[width * height];
                    float[] f = new float[bpp / 4];
                    for (int i = 0; i < width * height; ++i)
                    {
                        Marshal.Copy((IntPtr)(ptr.ToInt64() + i * bpp), f, 0, (int)bpp / 4);
                        Color color = new Color();
                        color.r = f[0];
                        color.g = f[1];
                        color.b = f[2];
                        color.a = 1f;
                        if (bpp == 16)
                        {
                            color.a = f[3];
                        }
                        colors[i] = color;
                    }
                }
                break;
            default:
                Debug.LogError("Error bpp = " + bpp);
                break;
        }
        return colors;
    }

    static FIBITMAP SaveTextureColors(PixelFormat format, Color[] colors, int width, int height)
    {
        int bpp = (int)format;
        switch (format)
        {
            case PixelFormat.A8:
                {
                    var dib = FreeImage.Allocate(width, height, bpp * 8);
                    var ptr = FreeImage.GetBits(dib);
                    for (int i = 0; i < width * height; ++i)
                    {
                        Color32 color = colors[i];
                        Marshal.WriteByte((IntPtr)(ptr.ToInt64() + i * bpp), color.a);
                    }
                    return dib;
                }
            case PixelFormat.PGM:
            case PixelFormat.RGB24:
            case PixelFormat.RGBA32:
                {
                    var dib = FreeImage.Allocate(width, height, bpp * 8);
                    var ptr = FreeImage.GetBits(dib);
                    byte[] c = new byte[bpp];
                    for (int i = 0; i < width * height; ++i)
                    {
                        Color32 color = colors[i];
                        c[0] = color.b;
                        c[1] = color.g;
                        if (bpp > 2)
                        {
                            c[2] = color.r;
                        }
                        if (bpp > 3)
                        {
                            c[3] = color.a;
                        }
                        Marshal.Copy(c, 0, (IntPtr)(ptr.ToInt64() + i * bpp), (int)bpp);
                    }
                    return dib;
                }
            case PixelFormat.RGBFloat:
            case PixelFormat.RGBAFloat:
                {
                    var dib = FreeImage.Allocate(width, height, bpp * 8);
                    var ptr = FreeImage.GetBits(dib);
                    float[] f = new float[bpp / 4];
                    for (int i = 0; i < width * height; ++i)
                    {
                        Color color = colors[i];
                        f[0] = color.r;
                        f[1] = color.g;
                        f[2] = color.b;
                        if (bpp == 16)
                        {
                            f[3] = color.a;
                        }
                        Marshal.Copy(f, 0, (IntPtr)(ptr.ToInt64() + i * bpp), (int)bpp / 4);
                    }
                    return dib;
                }
            default:
                return FIBITMAP.Zero;
        }
    }

    public static Color[] LoadTexture(string path, int width, int height)
    {
        FREE_IMAGE_FORMAT format = FreeImage.GetFileType(path, 0);
        FIBITMAP dib = FreeImage.Load(format, path, FREE_IMAGE_LOAD_FLAGS.DEFAULT);
        int imgWidth = (int)FreeImage.GetWidth(dib);
        int imgHeight = (int)FreeImage.GetHeight(dib);
        if (width != imgWidth || height != imgHeight)
        {
            FIBITMAP odib = dib;
            dib = FreeImage.Rescale(odib, width, height, FREE_IMAGE_FILTER.FILTER_LANCZOS3);
            FreeImage.Unload(odib);
        }

        var colors = LoadTextureColors(dib, out width, out height);
        FreeImage.Unload(dib);
        return colors;
    }

    public static Color[] LoadTexture(string path, out int width, out int height)
    {
        FREE_IMAGE_FORMAT format = FreeImage.GetFileType(path, 0);
        FIBITMAP dib = FreeImage.Load(format, path, FREE_IMAGE_LOAD_FLAGS.DEFAULT);

        var colors = LoadTextureColors(dib, out width, out height);
        FreeImage.Unload(dib);
        return colors;
    }

    public enum PixelFormat
    {
        RGB24 = 3,
        RGBA32 = 4,
        PGM = 2,
        A8 = 1,
        RGBFloat = 12,
        RGBAFloat = 16,
    }

    public static bool SaveTexture(string path, PixelFormat format, Color[] colors, int width, int height)
    {
        var dib = SaveTextureColors(format, colors, width, height);
        if (dib == FIBITMAP.Zero)
        {
            return false;
        }
        FreeImage.SaveEx(dib, path);
        FreeImage.Unload(dib);
        return true;
    }

    public static Color[] ResizeTexture(Color[] colors, PixelFormat format, int inWidth, int inHeight, int outWidth, int outHeight)
    {
        var dib = SaveTextureColors(format, colors, inWidth, inHeight);
        if (dib == FIBITMAP.Zero)
        {
            return colors;
        }


        FIBITMAP odib = FreeImage.Rescale(dib, outWidth, outHeight, FREE_IMAGE_FILTER.FILTER_LANCZOS3);
        FreeImage.Unload(dib);

        var outColors = LoadTextureColors(odib, out outWidth, out outHeight);
        FreeImage.Unload(odib);
        return outColors;
    }
}
#endif