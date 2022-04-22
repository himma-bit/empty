using UnityEngine;
using Sirenix.OdinInspector;

public class SlicePreview : MonoBehaviour
{
    public Material sliceMaterial;//使用模糊shader 创建一个材质球指引到这
    public int textureLength = 64;

    [Button("Preview")]
    void Preview()
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.rotation = Quaternion.Euler(90, 0, 0);
        for (int i = 0; i < textureLength - 1; i++)
        {
            Vector3 pos = new Vector3(transform.position.x, transform.position.y + i * (1f / textureLength) - 0.5f, transform.position.z);
            var quadInst = Instantiate(quad);
            quadInst.transform.position = pos;
            quadInst.transform.SetParent(transform);
            quadInst.GetComponent<MeshRenderer>().sharedMaterial = sliceMaterial;
            var block = new MaterialPropertyBlock();
            block.SetFloat("_offset", i * (1f / textureLength));
            quadInst.GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }
        GameObject.DestroyImmediate(quad);
    }
     
}