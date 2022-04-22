using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.TerrainTools;
using UnityEngine.TerrainUtils;
using Sirenix.OdinInspector;
using System.IO;
using System;

public class SliceGenerater : MonoBehaviour
{
    public int SliceCount = 128;
    private int SliceCountCache = 128;
    private GameObject controlGameObject;
    private Camera bakeCamera;
    private Texture2D[] Texture3DArray;
    private Texture3D BuildTexture3D;

    [LabelText("输出路径")]
    public string _outPath;
    public string OutPath
    {
        get {
            if (string.IsNullOrEmpty(_outPath))
                _outPath = "Assets/Scripts/Tools/3DTextureGenerate/Output";

            if (!Directory.Exists(_outPath))
                Directory.CreateDirectory(_outPath);

            return _outPath;
        }
        set { _outPath = value; }
    }

    [Button("创建烘焙相机")]
    public void CreateCamera()
    {
        if (controlGameObject != null && bakeCamera != null && SliceCount == SliceCountCache)
        {
            return;
        }
        else
        {
            if (controlGameObject != null)
                GameObject.DestroyImmediate(controlGameObject);

            SliceCountCache = SliceCount;
            Bounds aabb = new Bounds();
            bool bInit = false;

            var mrs = gameObject.GetComponentsInChildren<MeshFilter>();
            foreach (var mr in mrs)
            {
                if (!bInit)
                {
                    aabb = mr.sharedMesh.bounds;
                    bInit = true;
                }
                else
                    aabb.Encapsulate(mr.sharedMesh.bounds);
            }
            
            controlGameObject = new GameObject("SliceCam");
            controlGameObject.hideFlags = HideFlags.DontSave;
            controlGameObject.transform.parent = gameObject.transform;
            controlGameObject.transform.localPosition = aabb.center + new Vector3(0, aabb.extents.y, 0);
            controlGameObject.transform.localEulerAngles = new Vector3(90, 0, 0);
            bakeCamera = controlGameObject.AddComponent<Camera>();
            bakeCamera.clearFlags = CameraClearFlags.SolidColor;
            bakeCamera.backgroundColor = Color.black;//背景要设置黑色
            bakeCamera.orthographic = true;
            bakeCamera.orthographicSize = Mathf.Max(aabb.extents.x, aabb.extents.z);
            bakeCamera.nearClipPlane = 0;//box最顶面
            bakeCamera.farClipPlane = aabb.size.y;//box最底面
            bakeCamera.enabled = true;
            bakeCamera.renderingPath = RenderingPath.Forward;
            bakeCamera.cullingMask = 1<<LayerMask.NameToLayer("SliceGenerate");//设置一个专用层
            bakeCamera.targetTexture = RenderTexture.GetTemporary(SliceCount, SliceCount, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);//使用R8减少贴图大小

            gameObject.layer = LayerMask.NameToLayer("SliceGenerate");

            return;
        }
    }

    private void CreateTextureArray()
    {
        int sliceCount = this.SliceCount;
        Texture3DArray = new Texture2D[sliceCount];

        var mrs = gameObject.GetComponentsInChildren<MeshRenderer>();
        var oldMats = new List<Material[]>();
        var newMats = new List<Material[]>();
        foreach (var mr in mrs)
        {
            oldMats.Add(mr.sharedMaterials);
            
            var mats = new Material[mr.sharedMaterials.Length];
            for (var i = 0; i < mr.sharedMaterials.Length; i++)
                mats[i] = new Material(Shader.Find("SliceBake"));
            mr.sharedMaterials = mats;
        }

        try
        {
            for (int i = 0; i < sliceCount; i++)
            {
                Shader.SetGlobalFloat("_ClipHeight", (float)i/(float)sliceCount);

#if UNITY_EDITOR
                // bool bRenderDocLoaded =  UnityEditorInternal.RenderDoc.IsLoaded();
                // if (bRenderDocLoaded)  UnityEditorInternal.RenderDoc.BeginCaptureRenderDoc(SceneView.lastActiveSceneView);
#endif
                bakeCamera.Render();
#if UNITY_EDITOR
                // if (bRenderDocLoaded) UnityEditorInternal.RenderDoc.EndCaptureRenderDoc(SceneView.lastActiveSceneView);
#endif

                Texture2D texture2D = new Texture2D(sliceCount, sliceCount, TextureFormat.R8, false);
                RenderTexture.active = bakeCamera.targetTexture;
                texture2D.ReadPixels(new Rect(0, 0, sliceCount, sliceCount), 0, 0);
                texture2D.Apply();

                Texture3DArray[i] = texture2D;

                //这时也可以保存每层出来看一下
                // File.WriteAllBytes($"{OutPath}/slice_{i}.png", texture2D.EncodeToPNG());
            }
        }
        catch (Exception e){
            Debug.LogError(e);
        }

        for (var i = 0; i < mrs.Length; i++)
            mrs[i].sharedMaterials = oldMats[i];
        
        AssetDatabase.Refresh();
    }

    [Button("生成3DTexture")]
    private void Create3DTexture()
    {
        CreateCamera();
        CreateTextureArray();

        int width = Texture3DArray[0].width;
        int height = Texture3DArray[0].height;
        int depth = Texture3DArray.Length;

        BuildTexture3D = new Texture3D(width, height, depth, TextureFormat.R8, true);
        var color3d = new Color[width * height * depth];
        int idx = 0;

        for (int z = depth-1; z >= 0; z--) //反向读取 写入3D贴图
        {
            Texture2D loadedtexture = Texture3DArray[z];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x, ++idx)
                {
                    color3d[idx] = loadedtexture.GetPixel(x, y);
                }
            }
        }

        BuildTexture3D.SetPixels(color3d);
        BuildTexture3D.Apply();
        
        var savePath = $"{OutPath}/My3DTexture.asset";
        if (File.Exists(savePath))
            AssetDatabase.DeleteAsset(savePath);
            
        AssetDatabase.CreateAsset(BuildTexture3D, savePath);
    }
}
