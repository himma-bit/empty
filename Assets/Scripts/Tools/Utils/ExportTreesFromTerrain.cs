using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class ExportTreesFromTerrain : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public TerrainData terrainData;

    [Button("Export Trees")]
    public void ExportTrees()
    {
        var treeRoot = GameObject.Find("Trees").transform;
        foreach (var treeInstance in terrainData.treeInstances)
        {
            var prop = terrainData.treePrototypes[treeInstance.prototypeIndex];
            var inst = PrefabUtility.InstantiatePrefab(prop.prefab) as GameObject;
            inst.transform.parent = treeRoot;
            inst.transform.position = new Vector3(treeInstance.position.x * terrainData.size.x, treeInstance.position.y * terrainData.size.y, treeInstance.position.z * terrainData.size.z);
            inst.transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * treeInstance.rotation, Vector3.up);
            inst.transform.localScale = new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale);
        }
    }
    
    [Button("Export Details")]
    public void ExportDetails()
    {
        var details = GameObject.Find("Details");

        if (details == null)
            details = new GameObject("Details");
        var detailsRoot = details.transform;
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
                        prefab.transform.parent = detailsRoot;
                        prefab.transform.position = new Vector3(inst.posX+terrainPosOffset.x, inst.posY+terrainPosOffset.y, inst.posZ+terrainPosOffset.z);
                        prefab.transform.rotation = Quaternion.AngleAxis(Mathf.Rad2Deg * inst.rotationY, Vector3.up);
                        prefab.transform.localScale = new Vector3(inst.scaleXZ, inst.scaleY, inst.scaleXZ);
                    }
                }
            }
            
        }
    }
}
