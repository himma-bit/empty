using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

public class SdfGenerater : OdinEditorWindow
{
    [MenuItem("Tools/SDF")]
    public static void OpenGenerater()
    { 
        GetWindow<SdfGenerater>();
    }
    
    public Texture2D source;
    public float alphaThreshold = 0.6f;
    public int targetScale = 1;

    [Button("生成", ButtonSizes.Medium)]
    public void Gen()
    {
        var sourcePath = AssetDatabase.GetAssetPath(source);
        var targetPath = Path.GetDirectoryName(sourcePath).Replace("\\", "/") + "/" +
                         Path.GetFileNameWithoutExtension(sourcePath) + "_sdf" + Path.GetExtension(sourcePath);
        GenerateSDF(source, targetPath, alphaThreshold, targetScale);
    }

    public struct Pixel
    {
        public bool isIn;
        public float distance;
    }
    
    public static void GenerateSDF(Texture2D source, string targetPath, float alphaThreshold, int targetScale)
    {
        int sourceWidth = source.width;
        int sourceHeight = source.height;
        
        int targetWidth = sourceWidth/targetScale;
        int targetHeight = sourceHeight/targetScale;

        var pixels = new Pixel[sourceWidth, sourceHeight];
        var targetPixels = new Pixel[targetWidth, targetHeight];
        
        Debug.Log("sourceWidth" + sourceWidth);
        Debug.Log("sourceHeight" + sourceHeight);

        var sourcePath = AssetDatabase.GetAssetPath(source);
        var sourceColors = FreeImageUtils.LoadTexture(sourcePath, sourceWidth, sourceHeight);
        int x, y;
        for (x = 0; x < sourceWidth; x++)
        {
            for (y = 0; y < sourceHeight; y++)
            {
                pixels[x, y] = new Pixel();
                if (sourceColors[y*sourceWidth+x].a > alphaThreshold)
                    pixels[x, y].isIn = true;
                else
                    pixels[x, y].isIn = false;
            }
        }

        int gapX = sourceWidth / targetWidth;
        int gapY = sourceHeight / targetHeight;
        int MAX_SEARCH_DIST = 64;
        int minx, maxx, miny, maxy;
        float max_distance = -MAX_SEARCH_DIST;
        float min_distance = MAX_SEARCH_DIST;

        for (x = 0; x < targetWidth; x++)
        {
            for (y = 0; y < targetHeight; y++)
            {
                targetPixels[x, y] = new Pixel();
                int sourceX = x * gapX;
                int sourceY = y * gapY;
                int min = MAX_SEARCH_DIST;
                minx = sourceX - MAX_SEARCH_DIST;
                if (minx < 0)
                {
                    minx = 0;
                }
                miny = sourceY - MAX_SEARCH_DIST;
                if (miny < 0)
                {
                    miny = 0;
                }
                maxx = sourceX + MAX_SEARCH_DIST;
                if (maxx > (int)sourceWidth)
                {
                    maxx = sourceWidth;
                }
                maxy = sourceY + MAX_SEARCH_DIST;
                if (maxy > (int)sourceHeight)
                {
                    maxy = sourceHeight;
                }
                int dx, dy, iy, ix, distance;
                bool sourceIsInside = pixels[sourceX, sourceY].isIn;
                if (sourceIsInside)
                {
                    for (iy = miny; iy < maxy; iy++)
                    {
                        dy = iy - sourceY;
                        dy *= dy;
                        for (ix = minx; ix < maxx; ix++)
                        {
                            bool targetIsInside = pixels[ix, iy].isIn;
                            if (targetIsInside)
                            {
                                continue;
                            }
                            dx = ix - sourceX;
                            distance = (int)Mathf.Sqrt(dx * dx + dy);
                            if (distance < min)
                            {
                                min = distance;
                            }
                        }
                    }

                    if (min > max_distance)
                    {
                        max_distance = min;
                    }
                    targetPixels[x, y].distance = min;
                }
                else {
                    for (iy = miny; iy < maxy; iy++)
                    {
                        dy = iy - sourceY;
                        dy *= dy;
                        for (ix = minx; ix < maxx; ix++)
                        {
                            bool targetIsInside = pixels[ix, iy].isIn;
                            if (!targetIsInside)
                            {
                                continue;
                            }
                            dx = ix - sourceX;
                            distance = (int)Mathf.Sqrt(dx * dx + dy);
                            if (distance < min)
                            {
                                min = distance;
                            }
                        }
                    }

                    if (-min < min_distance)
                    {
                        min_distance = -min;
                    }
                    targetPixels[x, y].distance = -min;
                }
            }
        }

        //EXPORT texture
        var destColors = FreeImageUtils.LoadTexture(sourcePath, targetWidth, targetHeight);
        float clampDist = max_distance - min_distance;
        for (x = 0; x < targetWidth; x++)
        {
            for (y = 0; y < targetHeight; y++)
            {
                targetPixels[x, y].distance -= min_distance;
                float value = targetPixels[x, y].distance / clampDist;
                destColors[y * targetWidth + x].a = value;
            }
        }

        FreeImageUtils.SaveTexture(targetPath, FreeImageUtils.PixelFormat.RGBA32, destColors, targetWidth, targetHeight);
        AssetDatabase.Refresh();
    }
}
