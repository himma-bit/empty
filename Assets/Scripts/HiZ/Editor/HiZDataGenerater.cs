using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine;
using HiZRunTime;
using System.IO;
using DotLiquid.Tags;
using Unity.Collections;
using UnityEditor.VersionControl;

namespace HiZEditTime
{
    public class HiZDataGenerater : OdinEditorWindow
    {
        [MenuItem("Tools/HiZ")]
        public static void OpenGenerater()
        { 
            GetWindow<HiZDataGenerater>();
        }

        public List<GameObject> collectRoots = new List<GameObject>();

        public bool convertTerrainTree = true;
        public bool convertTerrainDetails = true;
        
        [Button("转成HiZ数据", ButtonSizes.Medium)]
        public void Collect()
        {
            List<string> collectRootNames = new List<string>();
            foreach (var root in collectRoots)
            {
                collectRootNames.Add(root.name);
            }
            collectRoots.Clear();
            
            // 保存原場景
            var scene = SceneManager.GetActiveScene();
            var oriScenePath = scene.path;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            
            // 复制一个hiz场景出来
            var hizScenePath = scene.path.Replace(".unity", "_hiz.unity");
            EditorSceneManager.SaveScene(scene, hizScenePath);
            
            // 打开hiz场景
            scene = EditorSceneManager.OpenScene(hizScenePath, OpenSceneMode.Single);
            
            // 将地表数据转成prefab实例
            ConvertTerrainData(collectRootNames);
            
            // 收集prefab实例hiz数据
            List<GameObject> willBeDestroy = new List<GameObject>();
            Dictionary<DrawParamsKey, DrawParams> allDraws = new Dictionary<DrawParamsKey, DrawParams>();
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
                if (collectRootNames.Contains(root.name))
                    CollectPrefab(root, allDraws, willBeDestroy);
            
            foreach (var temp in willBeDestroy)
                DestroyImmediate(temp);

            // 保存hiz数据
            var hizDataPath = hizScenePath.Replace(".unity", "_data.asset");
            SaveHiZData(hizDataPath, allDraws);
            
            // 给hiz场景添加Hiz data 组件
            var hizGo = new GameObject("HiZGO");
            var hizDataMono = hizGo.AddComponent<HiZDataMonoBehaviour>();
            hizDataMono.hizData = AssetDatabase.LoadAssetAtPath<HiZData>(hizDataPath);

            // 保存hiz场景
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            
            // 回复原场景
            EditorSceneManager.OpenScene(oriScenePath, OpenSceneMode.Single);
        }

        private void ConvertTerrainData(List<string> collectRootNames)
        {
            var terrain = GameObject.FindObjectOfType<Terrain>();
            if (terrain)
            {
                var terrainDataPath = AssetDatabase.GetAssetPath(terrain.terrainData);
                var hizTerrainDatapath = terrainDataPath.Replace(".asset", "_hiz.asset");
                AssetDatabase.CopyAsset(terrainDataPath, hizTerrainDatapath);
                terrain.terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(hizTerrainDatapath);
                var terrainData = terrain.terrainData;

                var terrainCollider = FindObjectOfType<TerrainCollider>();
                if (terrainCollider) terrainCollider.terrainData = terrainData;

                if (convertTerrainTree)
                {
                    var treeRoot = GameObject.Find("Trees");
                    if (treeRoot == null)
                        treeRoot = new GameObject("Trees");

                    if (!collectRootNames.Contains("Trees"))
                        collectRootNames.Add("Trees");
                
                    foreach (var treeInstance in terrainData.treeInstances)
                    {
                        var prop = terrainData.treePrototypes[treeInstance.prototypeIndex];
                        var inst = PrefabUtility.InstantiatePrefab(prop.prefab) as GameObject;
                        inst.transform.parent = treeRoot.transform;
                        inst.transform.position = new Vector3(treeInstance.position.x * terrainData.size.x, treeInstance.position.y * terrainData.size.y, treeInstance.position.z * terrainData.size.z);
                        inst.transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * treeInstance.rotation, Vector3.up);
                        inst.transform.localScale = new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale);
                    }
                
                    // 清空地表tree
                    terrainData.treeInstances =  Array.Empty<TreeInstance>();
                    terrainData.treePrototypes = Array.Empty<TreePrototype>();
                }

                if (convertTerrainDetails)
                {
                    var detailsRoot = GameObject.Find("Details");
                    if (detailsRoot == null)
                        detailsRoot = new GameObject("Details");
           
                    if (!collectRootNames.Contains("Details"))
                        collectRootNames.Add("Details");
                
                    var patchCount = Mathf.Ceil((float)terrainData.detailResolution / terrainData.detailResolutionPerPatch);
                    var terrainPosOffset = GameObject.FindObjectOfType<Terrain>().transform.position;
                    for (int layer = 0; layer < terrainData.detailPrototypes.Length; layer++)
                    {
                        var layerProp = terrainData.detailPrototypes[layer];
                        for (int i = 0; i < patchCount; i++)
                        {
                            for (int j = 0; j < patchCount; j++)
                            {
                                var insts = terrainData.ComputeDetailInstanceTransforms(i, j, layer, 1, out Bounds bounds);
                                foreach (var inst in insts)
                                {
                                    var prefab = PrefabUtility.InstantiatePrefab(layerProp.prototype) as GameObject;
                                    prefab.transform.parent = detailsRoot.transform;
                                    prefab.transform.position = new Vector3(inst.posX+terrainPosOffset.x, inst.posY+terrainPosOffset.y, inst.posZ+terrainPosOffset.z);
                                    prefab.transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * inst.rotationY, Vector3.up);
                                    prefab.transform.localScale = new Vector3(inst.scaleXZ, inst.scaleY, inst.scaleXZ);
                                }
                            }
                        }
                        
                        // 清空地表Details
                        // Get all of layer zero.
                        var map = terrainData.GetDetailLayer(0, 0, terrainData.detailWidth, terrainData.detailHeight, layer);

                        // For each pixel in the detail map...
                        for (int y = 0; y < terrainData.detailHeight; y++)
                        {
                            for (int x = 0; x < terrainData.detailWidth; x++)
                            {
                                map[x, y] = 0;
                            }
                        }
                        // Assign the modified map back.
                        terrainData.SetDetailLayer(0, 0, 0, map);
                    }
                    
                    // 清空地表Details
                    terrainData.detailPrototypes = Array.Empty<DetailPrototype>();
                    terrainData.RefreshPrototypes();
                }
            }
        }

        private void SaveHiZData(string savePath, Dictionary<DrawParamsKey, DrawParams> allDraws)
        {
            int clusterCount = 0;
            foreach (var drawParams in allDraws)
            {
                clusterCount += drawParams.Value.m_clusters.Count;
            }

            var allDrawsList = new List<DrawParams>();
            var hizData = ScriptableObject.CreateInstance<HiZData>();
            int clusterOffset = 0;
            var clusters = new NativeArray<ClusterData>(clusterCount, Allocator.Temp);
            foreach (var drawParams in allDraws)
            {
                drawParams.Value.m_instanceCount = drawParams.Value.m_instances.Count;
                drawParams.Value.m_runtimeInstances = drawParams.Value.m_instances.ToRawBytes();
                drawParams.Value.m_clusterOffset = (uint)clusterOffset;
                var count = drawParams.Value.m_clusters.Count;
                for (int i = 0; i < count; i++)
                {
                    drawParams.Value.m_clusters[i] = new ClusterData(drawParams.Value.m_clusters[i], (uint)clusterOffset);
                }
                    
                NativeArray<ClusterData>.Copy(drawParams.Value.m_clusters.ToArray(), 0, clusters, clusterOffset, count);
                clusterOffset += count;
                
                allDrawsList.Add(drawParams.Value);
            }

            hizData.m_allDraws = allDrawsList;
            hizData.m_clusters = clusters.ToRawBytes();
            hizData.m_clustersCount = clusterCount;
            
            AssetDatabase.CreateAsset(hizData, savePath);

            clusters.Dispose();
            allDraws.Clear();
            
            AssetDatabase.Refresh();
        }

        // 只收集connected的prefab
        private void CollectPrefab(GameObject root, Dictionary<DrawParamsKey, DrawParams> allDraws, List<GameObject> willBeDestroy)
        {
            if (root == null)
                return;
            
            if (PrefabUtility.GetPrefabInstanceStatus(root) != PrefabInstanceStatus.Connected)
            {
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    var child = root.transform.GetChild(i);
                    CollectPrefab(child.gameObject, allDraws, willBeDestroy);
                }

                return;
            }

            var lodGroup = root.GetComponent<LODGroup>();
            if (lodGroup && lodGroup.lodCount > 1)
            {
                var lods = lodGroup.GetLODs();
                
                // 拿0算一个DisplayDistanceMax
                var bounds = lods[0].renderers[0].bounds;
                foreach (var renderer in lods[0].renderers)
                    bounds.Encapsulate(renderer.bounds);
                
                var maxDis = HiZUtility.CalculateDisplayDistanceMax(bounds);
                var stepDis = maxDis / lods.Length;

                for (int i = 0; i < lods.Length; i++)
                {
                    if (lods[i].renderers == null || lods[i].renderers.Length == 0)
                    {
                        Debug.LogError("lod group renderer is null");
                        return;
                    }
                    
                    var displayDistanceMin = i*stepDis;
                    var displayDistanceMax = (i+1)*stepDis;

                    foreach (var renderer in lods[i].renderers)
                    {
                        AddOneDraw(renderer.gameObject, ref allDraws, displayDistanceMin, displayDistanceMax);
                    }
                }
            }
            else
            {
                var mrs = root.GetComponentsInChildren<MeshRenderer>();
                if (mrs.Length > 0)
                {
                    Bounds bounds = mrs[0].bounds;
                    foreach (var mr in mrs)
                        bounds.Encapsulate(mr.bounds);
                    
                    var displayDistanceMax = HiZUtility.CalculateDisplayDistanceMax(bounds);
                    
                    AddOneDraw(root, ref allDraws, 0, displayDistanceMax);
                }
            }
            
            willBeDestroy.Add(root);
        }

        public void AddOneDraw(GameObject root, ref Dictionary<DrawParamsKey, DrawParams> allDraws, float displayDistanceMin, float displayDistanceMax)
        {
            var mfs = root.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in mfs)
            {
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr.sharedMaterials.Length != mf.sharedMesh.subMeshCount)
                {
                    Debug.LogError("mesh count, mat count, mismatch");
                    continue;
                }

                var meshPath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                int meshIndex = -1;
                Mesh mesh;
                if (meshPath.EndsWith(".asset"))
                {
                    mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    meshIndex = -1;
                }
                else if (meshPath.ToLower().EndsWith(".fbx"))
                {
                    var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);
                    var allMFsInFbx = fbx.GetComponentsInChildren<MeshFilter>();

                    for (int i = 0; i < allMFsInFbx.Length; i++)
                    {
                        if (allMFsInFbx[i].sharedMesh == mf.sharedMesh)
                        {
                            meshIndex = i;
                            break;
                        }
                    }

                    if (meshIndex == -1)
                    {
                        throw new Exception("no foud mesh index");
                    }

                    mesh = mf.sharedMesh;
                }
                else
                {
                    Debug.LogError($"{root.name}, {meshPath}, {mf.name}, {mf.sharedMesh.name}");
                    continue;
                }

                for (int submeshIndex = 0; submeshIndex < mesh.subMeshCount; submeshIndex++)
                {
                    var mat = mr.sharedMaterials[submeshIndex];
                    var matPath = AssetDatabase.GetAssetPath(mat);
                    if (mat == null || matPath == null)
                    {
                        Debug.LogError("mat error!");
                        break;
                    }

                    var drawParamsKey = new DrawParamsKey(meshPath, meshIndex, submeshIndex, matPath);
                    if (!allDraws.TryGetValue(drawParamsKey, out DrawParams drawParams))
                    {
                        allDraws.Add(drawParamsKey, new DrawParams(drawParamsKey, (uint)(allDraws.Count - 1)));
                        drawParams = allDraws[drawParamsKey];
                        drawParams.m_drawIndex = (uint)(allDraws.Count-1);
                    }
                    
                    var matrix = mr.transform.localToWorldMatrix;
                    // matrix.m00 = Mathf.Abs(matrix.m00);
                    // matrix.m11 = Mathf.Abs(matrix.m11);
                    // matrix.m22 = Mathf.Abs(matrix.m22);
                    
                    drawParams.m_clusters.Add(new ClusterData(mr.bounds, displayDistanceMin, displayDistanceMax, drawParams.m_drawIndex));
                    drawParams.m_instances.Add(new InstanceData(matrix, matrix.inverse));
                }
            }
        }
    }
}

