using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Unity.Mathematics;

namespace HiZRunTime
{
    public class HiZMgr
    {
        private static HiZMgr _Instance;
        public static HiZMgr Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new HiZMgr();
                }
                return _Instance;
            }
            private set { }
        }
        
        private HiZData m_hizData;

        public HiZData hizData
        {
            get
            {
                return m_hizData;
            }
            set
            {
                m_hizData = value;
                m_bEnable = m_hizData != null;
                OnHiZDataChanged();
            }
        }
        
        private int m_Clearkernel;
        private int m_Cullkernel;
        
        private ComputeShader m_cullCS;

        private Matrix4x4 m_lastVPs;
        private RenderTexture m_hizDepthRT;
        private int m_hizDepthRTWidth;
        private int m_hizDepthRTMip;
        
        private ComputeBuffer m_clusterBuffer;
        private ComputeBuffer m_drawArgsBuffer;
        private ComputeBuffer m_resultBuffer;
        private bool m_bEnable = false;
        
        private int m_OriDepthTexID = Shader.PropertyToID("_OriDepthTex");
        private int m_SourceTexID = Shader.PropertyToID("_SourceTex");
        private int m_DestTexId = Shader.PropertyToID("_DestTex");
        private int m_DepthRTSize = Shader.PropertyToID("_DepthRTSize");
        private int m_OriDepthTexSizeID = Shader.PropertyToID("_OriDepthTexSize");

        private Material m_genHiZRTMat;

        private ComputeShader m_generateMipmapCS;
        private int m_genMipmapKernel;
        
        private (int, int) GetDepthRTWidthFromScreen(int screenWidth)
        {
            if (screenWidth >= 2048)
            {
                return (1024, 10);
            }
            else if (screenWidth >= 1024)
            {
                return (512, 9);
            }
            else
            {
                return (256, 8);
            }
        }

        private void EnsureResourceReady(RenderingData renderingData)
        {
            if (!m_cullCS)
            {
                m_cullCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Scripts/HiZ/Res/GpuFrustumCulling.compute");
                m_Clearkernel = m_cullCS.FindKernel("Clear"); 
                m_Cullkernel = m_cullCS.FindKernel("Cull");
            }

            if (m_hizDepthRT == null)
                CreateHiZDepthRT(renderingData.cameraData.camera.pixelWidth);
            
            if (m_genHiZRTMat == null)
                m_genHiZRTMat = new Material(Shader.Find("Hidden/GenerateDepthRT"));

            if (m_generateMipmapCS == null)
            {
                m_generateMipmapCS = AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Scripts/HiZ/Res/GenerateMipmap.compute");
                m_genMipmapKernel = m_generateMipmapCS.FindKernel("GenMip");
            }

            UpdateHiZDepthRTWidth(renderingData.cameraData.camera.pixelWidth);
        }
        
        public void CreateHiZDepthRT(int screenWidth)
        {
            if (m_hizDepthRT == null)
            {
                (int w, int mip) = GetDepthRTWidthFromScreen(screenWidth);
                
                m_hizDepthRTWidth = w;
                m_hizDepthRTMip = mip;
                
                var depthRT = new RenderTexture(w, w / 2, 0, RenderTextureFormat.RHalf, mip);
                depthRT.name = "hizDepthRT";
                depthRT.useMipMap = true;
                depthRT.autoGenerateMips = false;
                depthRT.enableRandomWrite = true;
                depthRT.wrapMode = TextureWrapMode.Clamp;
                depthRT.filterMode = FilterMode.Point;
                depthRT.Create();
                m_hizDepthRT = depthRT;
            }
        }

        public void UpdateHiZDepthRTWidth(int screenWidth)
        {
            (int width, int mip) = GetDepthRTWidthFromScreen(screenWidth);
            
            if (width != m_hizDepthRTWidth)
            {
                m_hizDepthRTWidth = width;
                m_hizDepthRTMip = mip;
                
                m_hizDepthRT.Release();
                m_hizDepthRT = null;
                CreateHiZDepthRT(screenWidth);
            }
        }

        public bool IsEnable
        {
            get
            {
                return m_bEnable;
            }
            private set{}
        }
        
        public enum DepthRTType
        {
            Opaque,
            Shadow,
            PreZ,
        }

        private Dictionary<RenderPassEvent, DepthRTType> RenderPassEvent2DepthRTType = new Dictionary<RenderPassEvent, DepthRTType>()
        {
            {RenderPassEvent.BeforeRenderingOpaques, DepthRTType.Opaque},
            {RenderPassEvent.AfterRenderingOpaques+1, DepthRTType.Opaque},
            {RenderPassEvent.AfterRenderingSkybox-1, DepthRTType.Opaque},
        };
        
        private void SafeDesposeCB(ref ComputeBuffer cb)
        {
            if (cb != null)
            {
                cb.Dispose();
                cb = null;
            }
        }

        private void OnHiZDataChanged()
        {
            if (!IsEnable)
            {
                SafeDesposeCB(ref m_clusterBuffer);
                SafeDesposeCB(ref m_drawArgsBuffer);
                SafeDesposeCB(ref m_resultBuffer);
                
                return;
            }

            m_resultBuffer = new ComputeBuffer(m_hizData.m_clustersCount, sizeof(uint));
            
            m_clusterBuffer = new ComputeBuffer(m_hizData.m_clustersCount, Marshal.SizeOf(typeof(ClusterData)));
            m_clusterBuffer.SetData(m_hizData.m_clusters);

            var args = new uint[m_hizData.m_allDraws.Count * 5];
            for (int i = 0; i < m_hizData.m_allDraws.Count; i++)
            {
                var drawParams = m_hizData.m_allDraws[i];

                if (drawParams.m_meshIndex < 0)
                {
                    var meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(drawParams.m_meshPath);
                    drawParams.m_mesh = GameObject.Instantiate(meshAsset);
                }
                else
                {
                    var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(drawParams.m_meshPath);
                    var allMFsInFbx = fbx.GetComponentsInChildren<MeshFilter>();
                    if (drawParams.m_meshIndex >= allMFsInFbx.Length)
                    {
                        Debug.LogError("mesh index error!!!");
                        continue;
                    }

                    drawParams.m_mesh = GameObject.Instantiate(allMFsInFbx[drawParams.m_meshIndex].sharedMesh);
                }

                drawParams.m_mat = GameObject.Instantiate(AssetDatabase.LoadAssetAtPath<Material>(drawParams.m_matPath));
                
                drawParams.m_instanceBuffer?.Release();
                drawParams.m_instanceBuffer = new ComputeBuffer(drawParams.m_instanceCount, Marshal.SizeOf(typeof(InstanceData)));
                drawParams.m_instanceBuffer.SetData(drawParams.m_runtimeInstances);
                drawParams.m_mat.SetInt(HiZConst._ClusterOffset, (int)drawParams.m_clusterOffset);
                drawParams.m_mat.SetBuffer(HiZConst._InstanceBuffer, drawParams.m_instanceBuffer);
                
                var mesh = drawParams.m_mesh;
                var submeshIndex = drawParams.m_submeshIndex;
                args[i * 5 + 0] = mesh.GetIndexCount(submeshIndex);
                args[i * 5 + 1] = 0;
                args[i * 5 + 2] = mesh.GetIndexStart(submeshIndex);
                args[i * 5 + 3] = mesh.GetBaseVertex(submeshIndex);
                args[i * 5 + 4] = 0;
            }
            
            m_drawArgsBuffer = new ComputeBuffer(m_hizData.m_allDraws.Count*5, sizeof(uint), ComputeBufferType.IndirectArguments);
            m_drawArgsBuffer.SetData(args);
        }
        
        private string m_HiZCullTestProfileTag = "HiZCullTest";
        private ProfilingSampler m_HiZCullTestProfile;

        public Vector3 m_minFrustumPlanes;
        public Vector3 m_maxFrustumPlanes;
        
        public void ExecuteCull(ScriptableRenderContext context, RenderPassEvent passEvent, ref RenderingData renderingData)
        {
            if (!Instance.IsEnable)
                return;
            
            // scene view下不做cull，直接使用GameView下的cull result
            if (renderingData.cameraData.camera.name == "SceneCamera" ||
                renderingData.cameraData.camera.name == "Preview Camera")
                return;

            EnsureResourceReady(renderingData);
            
            if (m_HiZCullTestProfile == null)
                m_HiZCullTestProfile = new ProfilingSampler(m_HiZCullTestProfileTag);
            
            var camera = renderingData.cameraData.camera;
            var frustumCorners = new Vector3[8];
            Transform trans = camera.transform;
            PerspCam perspCam = new PerspCam
            {
                fov = camera.fieldOfView,
                nearClipPlane = camera.nearClipPlane,
                farClipPlane = camera.farClipPlane,
                aspect = camera.aspect,
                forward = trans.forward,
                right = trans.right,
                up = trans.up,
                position = trans.position,
            };

            HiZUtility.GetFrustumCorner(ref perspCam, frustumCorners);
            Vector3 minFrustumPlanes = frustumCorners[0];
            Vector3 maxFrustumPlanes = frustumCorners[0];
            for (int i = 1; i < 8; ++i)
            {
                minFrustumPlanes = math.min(minFrustumPlanes, frustumCorners[i]);
                maxFrustumPlanes = math.max(maxFrustumPlanes, frustumCorners[i]);
            }
            
            m_minFrustumPlanes = minFrustumPlanes;
            m_maxFrustumPlanes = maxFrustumPlanes;

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var frustumPlanes = new Vector4[6];
            for (int i = 0; i < 6; ++i)
            {
                Plane p = planes[i];
                frustumPlanes[i] = new float4(-p.normal, -p.distance);
            }
            
            CommandBuffer cmd = CommandBufferPool.Get(m_HiZCullTestProfileTag);
            using (new ProfilingScope(cmd, m_HiZCullTestProfile))
            {
                // 清空上一帧测试结果
                cmd.SetComputeBufferParam(m_cullCS, m_Clearkernel, HiZConst._ArgsBuffer, m_drawArgsBuffer);
                cmd.SetComputeIntParam(m_cullCS, HiZConst._MaxCount, m_hizData.m_allDraws.Count);
                cmd.DispatchCompute(m_cullCS, m_Clearkernel, Mathf.CeilToInt(m_hizData.m_allDraws.Count/64.0f), 1, 1);
                
                // 做这一帧测试
                cmd.SetComputeBufferParam(m_cullCS, m_Cullkernel, HiZConst._ArgsBuffer, m_drawArgsBuffer);
                cmd.SetComputeBufferParam(m_cullCS, m_Cullkernel, HiZConst._ClusterBuffer, m_clusterBuffer);
                cmd.SetComputeBufferParam(m_cullCS, m_Cullkernel, HiZConst._ResultBuffer, m_resultBuffer);
                cmd.SetComputeTextureParam(m_cullCS, m_Cullkernel, HiZConst._HizDepthTex, m_hizDepthRT);
                cmd.SetComputeVectorArrayParam(m_cullCS, HiZConst._Planes, frustumPlanes);
                cmd.SetComputeVectorParam(m_cullCS, HiZConst._FrustumMinPoint, minFrustumPlanes);
                cmd.SetComputeVectorParam(m_cullCS, HiZConst._FrustumMaxPoint, maxFrustumPlanes);
                cmd.SetComputeVectorParam(m_cullCS, HiZConst._CameraPos, camera.transform.position);
                cmd.SetComputeMatrixParam(m_cullCS, HiZConst._LastVp, m_lastVPs);
                cmd.SetComputeVectorParam(m_cullCS, HiZConst._HizScreenRes, new Vector4(m_hizDepthRT.width, m_hizDepthRT.height, m_hizDepthRT.mipmapCount-0.5f, m_hizDepthRT.mipmapCount-1));
                cmd.SetComputeIntParam(m_cullCS, HiZConst._MaxCount, m_hizData.m_clustersCount);
                cmd.DispatchCompute(m_cullCS, m_Cullkernel, Mathf.CeilToInt(m_hizData.m_clustersCount/64.0f), 1, 1);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        private string m_HiZDrawProfileTag = "HiZDraw";
        private ProfilingSampler m_HiZDrawProfile;
        
        public void ExecuteDraw(ScriptableRenderContext context, RenderPassEvent passEvent, ref RenderingData renderingData, string passName)
        {
            EnsureResourceReady(renderingData);
            
            if (m_HiZDrawProfile == null)
                m_HiZDrawProfile = new ProfilingSampler(m_HiZDrawProfileTag);
            
            if (!Instance.IsEnable)
                return;
            
            CommandBuffer cmd = CommandBufferPool.Get(m_HiZDrawProfileTag);
            using (new ProfilingScope(cmd, m_HiZDrawProfile))
            {
                Shader.SetGlobalBuffer(HiZConst._ResultBuffer, m_resultBuffer);
                // copy depth to hiz depth RT
                for (int i = 0; i < m_hizData.m_allDraws.Count; i++)
                {
                    var drawParams = m_hizData.m_allDraws[i];
                    var passIndex = drawParams.m_mat.FindPass(passName);
                    cmd.DrawMeshInstancedIndirect(drawParams.m_mesh, drawParams.m_submeshIndex, drawParams.m_mat, passIndex, m_drawArgsBuffer, (int)(sizeof(uint) * 5 * drawParams.m_drawIndex));
                }
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        private string m_DepthRTGeneratePassTag = "HiZDepthGeneratePass";
        private ProfilingSampler m_DepthRTGeneratePassSampler;

        public void ExecuteDepthGenerate(ScriptableRenderContext context, RenderPassEvent passEvent, ref RenderingData renderingData)
        {
            if (!Instance.IsEnable)
                return;
            
            // scene view下不生成深度图
            if (renderingData.cameraData.camera.name == "SceneCamera" ||
                renderingData.cameraData.camera.name == "Preview Camera")
                return;
            
            EnsureResourceReady(renderingData);

            if (m_DepthRTGeneratePassSampler == null)
                m_DepthRTGeneratePassSampler = new ProfilingSampler(m_DepthRTGeneratePassTag);

            var proj = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix, true);
            Matrix4x4 lastVp = proj * renderingData.cameraData.camera.worldToCameraMatrix;
            
            m_lastVPs = lastVp;
            
            CommandBuffer cmd = CommandBufferPool.Get(m_DepthRTGeneratePassTag);
            using (new ProfilingScope(cmd, m_DepthRTGeneratePassSampler))
            {
                // copy depth to hiz depth RT
                cmd.Blit(Texture2D.blackTexture, m_hizDepthRT, m_genHiZRTMat);
                
                float w = m_hizDepthRTWidth;
                float h = m_hizDepthRTWidth / 2.0f;
                for (int i = 1; i < m_hizDepthRTMip; ++i)
                {
                    w = Mathf.Max(1, w / 2);
                    h = Mathf.Max(1, h / 2);
                    cmd.SetComputeTextureParam(m_generateMipmapCS, m_genMipmapKernel, m_SourceTexID, m_hizDepthRT, i - 1);
                    cmd.SetComputeTextureParam(m_generateMipmapCS, m_genMipmapKernel, m_DestTexId, m_hizDepthRT, i);
                    cmd.SetComputeVectorParam(m_generateMipmapCS, m_DepthRTSize, new Vector4(w, h, 0f, 0f));

                    int x, y;
                    x = Mathf.CeilToInt(w / 8f);
                    y = Mathf.CeilToInt(h / 8f);
                    cmd.DispatchCompute(m_generateMipmapCS, 0, x, y, 1);
                }
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}

