using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class GrassTool: MonoBehaviour
{
    [System.Serializable]
    public class TreeData
    {
        public Mesh treeMesh;
        public Material treeMaterial;
        public Mesh lodTreeMesh;
        public Material lodTreeMaterial;
        public BlockData[] blockDatas;
        [NonSerialized]
        public SearchInfo searchInfo;
        public SearchInfo[] searchInfos;
    }

    [System.Serializable]
    public class SearchInfo
    {
        public OBB obbBound;
        [NonSerialized]
        public List<SearchInfo> nextSearchInfos = new List<SearchInfo>();
        public List<int> nextSearchInfoIndexs = new List<int>();
        public List<int> blockIndexs = new List<int>();
#if UNITY_EDITOR
        [NonSerialized]
        public Bounds totalBounds_Editor;
#endif
    }

    [System.Serializable]
    public class BlockData
    {
        public OBB packedObbBound;
        public Vector4 packedSphere;
        public Matrix4x4[] treeTransform;
        public OBB[] obbBounds;
        public OBB.OBBLocal[] obbLocalBounds;
        public int drawSize;
#if UNITY_EDITOR
        [NonSerialized]
        public Bounds totalBounds_Editor;
#endif

        public void SwapData(int x, int y)
        {
            Matrix4x4 temp = treeTransform[x];
            treeTransform[x] = treeTransform[y];
            treeTransform[y] = temp;

            OBB tempOBB = obbBounds[x];
            obbBounds[x] = obbBounds[y];
            obbBounds[y] = tempOBB;

            if (obbLocalBounds != null && x < obbLocalBounds.Length && y < obbLocalBounds.Length)
            {
                OBB.OBBLocal tempOBBLocal = obbLocalBounds[x];
                obbLocalBounds[x] = obbLocalBounds[y];
                obbLocalBounds[y] = tempOBBLocal;
            }
        }
    }

    [ReadOnly]
    public TreeData[] treeBlocks;
    [ReadOnly]
    public int totalBlockDataCount = 0;

    protected BoundingSphere[] boundingSpheres;
    protected LinkedList<StateChangedMethodClass> cullingGroups = new LinkedList<StateChangedMethodClass>();
    protected HashSet<Camera> usingCameras = new HashSet<Camera>();

    [NonSerialized]
    public Matrix4x4[] tempMatrix4X4s = new Matrix4x4[1023]; // 1023 - max size for gpu instance
    [NonSerialized]
    public Matrix4x4[] tempLODMatrix4X4s = new Matrix4x4[1023]; // 1023 - max size for gpu instance

    protected bool forceUpdateCommandBuffer = false;

    protected HashSet<BuildingArea> buildingAreas;
    protected int treeAreaCullIndex = 0;
    // protected bool finishUpdateImportant = false;
    protected bool needUpdateBuildingAreaCull = false;

    [HideInInspector]
    public float buildingAreaUpdateTime = 0.015f;
    [HideInInspector]
    public bool useSearchInfo = false;
    private void OnEnable()
    {
        Camera.onPreRender += BeforeCameraRender;
        BuildingAreaUtility.Instance.AddGrassTool(this);
    }

    private void OnDisable()
    {
        Camera.onPreRender -= BeforeCameraRender;
        Reset();
        BuildingAreaUtility.Instance.RemoveGrassTool(this);
    }

    public virtual void Reset()
    {
        foreach (StateChangedMethodClass cullingGroup in cullingGroups)
        {
            cullingGroup.Dispose();
        }
        cullingGroups.Clear();
        usingCameras.Clear();
        boundingSpheres = null;
    }

    private bool IsReady(Camera camera)
    {
        return camera != null && treeBlocks != null && treeBlocks.Length > 0 && totalBlockDataCount > 0 && SystemInfo.supportsInstancing;
    }

    protected virtual void BeforeCameraRender(Camera camera)
    {
        if (IsReady(camera))
        {
            InitCullGroupUtility(camera);
            InitBoundingSpheres();
            if (needUpdateBuildingAreaCull)
            {
                CalBuildingAreasCulling(camera);
            }
        }
    }

    void InitCullGroupUtility(Camera camera)
    {
        if (camera == null)
        {
            return;
        }
        if (usingCameras.Add(camera))
        {
            InitCullGroupUtilityBase(camera);
        }
    }

    void InitCullGroupUtilityBase(Camera camera)
    {
        StateChangedMethodClass cullingGroup = new StateChangedMethodClass(camera, this, CameraEvent.BeforeForwardOpaque);
        SetBoundingSpheres(cullingGroup);
        cullingGroups.AddLast(cullingGroup);
        forceUpdateCommandBuffer = true;
    }

    protected virtual void InitBoundingSpheres()
    {
        boundingSpheres = new BoundingSphere[totalBlockDataCount];
        int index = 0;
        for (int i = 0; i < treeBlocks.Length; i++)
        {
            TreeData block = treeBlocks[i];
            for (int j = 0; j < block.blockDatas.Length; j++)
            {
                BlockData blockData = block.blockDatas[j];
                if (index < totalBlockDataCount)
                {
                    boundingSpheres[index] = new BoundingSphere(GetBlockDataPackedSphere(blockData));
                    index++;
                }
                else
                {
                    break;
                }
            }
        }
        foreach (StateChangedMethodClass cullingGroup in cullingGroups)
        {
            SetBoundingSpheres(cullingGroup);
        }
    }

    void SetBoundingSpheres(StateChangedMethodClass cullingGroup)
    {
        if (boundingSpheres != null)
        {
            cullingGroup.SetBoundingSpheres(boundingSpheres, Mathf.Min(totalBlockDataCount, boundingSpheres.Length));
        }
    }

    protected virtual OBB GetSearchInfoOBBBounds(SearchInfo searchInfo)
    {
        return searchInfo.obbBound;
    }

    protected virtual Vector4 GetBlockDataPackedSphere(BlockData blockData)
    {
        return blockData.packedSphere;
    }

    protected virtual void CalMatrix4X4s(Matrix4x4[] matrix4X4s, int count) { }

    protected virtual OBB GetBlockDataOBBBounds(BlockData blockData, int index)
    {
        return blockData.obbBounds[index];
    }

    // 设置建筑占地数组
    public void SetBuildingAreas(HashSet<BuildingArea> areas)
    {
        buildingAreas = areas;
        needUpdateBuildingAreaCull = true;
        treeAreaCullIndex = 0;
        // finishUpdateImportant = false;
    }

    void CalBuildingAreasCulling()
    {
        if (needUpdateBuildingAreaCull == false)
        {
            return;
        }
        float startTime = Time.realtimeSinceStartup;
        int currentIndex = 0;

        foreach (TreeData block in treeBlocks)
        {
            int j = 0;
            if (treeAreaCullIndex >= currentIndex + block.blockDatas.Length)
            {
                currentIndex += block.blockDatas.Length;
                continue;
            }
            else
            {
                j = treeAreaCullIndex - currentIndex;
            }

            for (; j < block.blockDatas.Length; j++)
            {
                float deltaTime = Time.realtimeSinceStartup - startTime;
                if (deltaTime > 0.03f)
                {
                    return;
                }

                treeAreaCullIndex++;
                BlockData blockData = block.blockDatas[j];
                int oldDrawSize = blockData.drawSize;
                blockData.drawSize = blockData.treeTransform.Length;
                foreach (BuildingArea area in buildingAreas)
                {
                    if (area == null || blockData.drawSize == 0)
                    {
                        continue;
                    }
                    if (!IsBlockDataInBuildingArea(blockData, area))
                    {
                        continue;
                    }
                    for (int x = 0; x < blockData.drawSize; x++)
                    {
                        if (IsTreeInBuildingArea(GetBlockDataOBBBounds(blockData, x), area))
                        {
                            blockData.drawSize--;
                            blockData.SwapData(x, blockData.drawSize);
                            x--;
                        }
                    }
                }
                if (blockData.drawSize != oldDrawSize)
                {
                    forceUpdateCommandBuffer = true;
                    return;
                }
            }
        }
        needUpdateBuildingAreaCull = false;
        forceUpdateCommandBuffer = true;
    }

    protected virtual void CalBuildingAreasCulling(Camera camera)
    {
        if (needUpdateBuildingAreaCull == false)
        {
            return;
        }
        float startTime = Time.realtimeSinceStartup;
        int currentIndex = 0;

        foreach (TreeData block in treeBlocks)
        {
            int j = 0;
            if (treeAreaCullIndex >= currentIndex + block.blockDatas.Length)
            {
                currentIndex += block.blockDatas.Length;
                continue;
            }
            else
            {
                j = treeAreaCullIndex - currentIndex;
            }

            for (; j < block.blockDatas.Length; j++)
            {
                float deltaTime = Time.realtimeSinceStartup - startTime;
                if (deltaTime > 0.03f)
                {
                    return;
                }

                treeAreaCullIndex++;
                BlockData blockData = block.blockDatas[j];
                int oldDrawSize = blockData.drawSize;
                blockData.drawSize = blockData.treeTransform.Length;
                foreach (BuildingArea area in buildingAreas)
                {
                    if (area == null || blockData.drawSize == 0)
                    {
                        continue;
                    }
                    if (!IsBlockDataInBuildingArea(blockData, area))
                    {
                        continue;
                    }
                    for (int x = 0; x < blockData.drawSize; x++)
                    {
                        if (IsTreeInBuildingArea(GetBlockDataOBBBounds(blockData, x), area))
                        {
                            blockData.drawSize--;
                            blockData.SwapData(x, blockData.drawSize);
                            x--;
                        }
                    }
                }
                if (blockData.drawSize != oldDrawSize)
                {
                    forceUpdateCommandBuffer = true;
                    return;
                }
            }
        }
        needUpdateBuildingAreaCull = false;
        forceUpdateCommandBuffer = true;
    }

    protected virtual bool IsSearchInfoInBuildingArea(SearchInfo searchInfo, BuildingArea area)
    {
        return area.Intersects(GetSearchInfoOBBBounds(searchInfo));
    }

    protected virtual bool IsBlockDataInBuildingArea(BlockData blockData, BuildingArea area)
    {
        Vector4 packedSphere = GetBlockDataPackedSphere(blockData);
        return area.Intersects(packedSphere);
    }

    protected bool IsTreeInBuildingArea(OBB treeBound, BuildingArea area)
    {
        return area.Intersects(treeBound);
    }

    protected void UpdateCommandBuffer(bool forced)
    {
        foreach (StateChangedMethodClass cullingGroup in cullingGroups)
        {
            cullingGroup.UpdateCommandBuffer(forced);
        }
    }

    public class StateChangedMethodClass : IDisposable
    {
        private Camera usingCamera;
        GrassTool grassTool;
        CommandBuffer commandBuffer;
        string commandBufferName;
        CameraEvent usingCameraEvent = CameraEvent.BeforeForwardOpaque;
        CullingGroup cullingGroup;
        Lazy<Dictionary<int, CullingGroupEvent>> blockState = new Lazy<Dictionary<int, CullingGroupEvent>>();
        bool needUpdateCommandBuffer = false;

        public StateChangedMethodClass(Camera camera, GrassTool grass, CameraEvent cameraEvent)
        {
            grassTool = grass;
            usingCamera = camera;
            cullingGroup = new CullingGroup
            {
                targetCamera = usingCamera,
                onStateChanged = StateChangedMethod
            };
            cullingGroup.SetDistanceReferencePoint(camera.transform);
            commandBufferName = string.Format("DrawGrass_{0}", grass.GetInstanceID());
            commandBuffer = new CommandBuffer
            {
                name = commandBufferName
            };
            usingCameraEvent = cameraEvent;
            usingCamera.AddCommandBuffer(usingCameraEvent, commandBuffer);
        }

        public void SetBoundingSpheres(BoundingSphere[] spheres, int sphereCount)
        {
            cullingGroup.SetBoundingSpheres(spheres);
            cullingGroup.SetBoundingSphereCount(sphereCount);
        }

        public void SetLODDistance(float[] lodDistance)
        {
            cullingGroup.SetBoundingDistances(lodDistance);
        }

        public void UpdateCommandBuffer(bool forced)
        {
            if (needUpdateCommandBuffer || forced)
            {
                needUpdateCommandBuffer = false;
                commandBuffer.Clear();
                DrawGrassCommandBuffer(0);//Draw PreZ
                DrawGrassCommandBuffer(1);
            }
        }

        private void DrawGrassCommandBuffer(int drawShaderPass)
        {
            int index = -1;
            for (int i = 0; i < grassTool.treeBlocks.Length; i++)
            {
                TreeData block = grassTool.treeBlocks[i];
                int drawCount = 0;
                int lodDrawCount = 0;
                for (int j = 0; j < block.blockDatas.Length; j++)
                {
                    index++;
                    BlockData blockData = block.blockDatas[j];
                    if (blockState.Value.TryGetValue(index, out CullingGroupEvent evt))
                    {
                        if (evt.isVisible == false)
                        {
                            continue;
                        }
                    }
                    if (evt.currentDistance == 0)
                    {
                        if (drawCount + blockData.drawSize >= grassTool.tempMatrix4X4s.Length)
                        {
                            DrawMeshInstanced(block.treeMesh, block.treeMaterial, grassTool.tempMatrix4X4s, drawCount,drawShaderPass);
                            drawCount = 0;
                        }
                        Array.Copy(blockData.treeTransform, 0, grassTool.tempMatrix4X4s, drawCount, blockData.drawSize);
                        drawCount += blockData.drawSize;
                    }
                    else
                    {
                        if (lodDrawCount + blockData.drawSize >= grassTool.tempLODMatrix4X4s.Length)
                        {
                            DrawMeshInstanced(block.lodTreeMesh, block.lodTreeMaterial, grassTool.tempLODMatrix4X4s, lodDrawCount,drawShaderPass);
                            lodDrawCount = 0;
                        }
                        Array.Copy(blockData.treeTransform, 0, grassTool.tempLODMatrix4X4s, lodDrawCount, blockData.drawSize);
                        lodDrawCount += blockData.drawSize;
                    }
                }
                DrawMeshInstanced(block.treeMesh, block.treeMaterial, grassTool.tempMatrix4X4s, drawCount,drawShaderPass);
                DrawMeshInstanced(block.lodTreeMesh, block.lodTreeMaterial, grassTool.tempLODMatrix4X4s, lodDrawCount,drawShaderPass);
            }
        }

        private void DrawMeshInstanced(Mesh mesh, Material material, Matrix4x4[] matrix4X4s, int count,int shaderPass)
        {
            if (mesh != null && material != null && count > 0)
            {
                grassTool.CalMatrix4X4s(matrix4X4s, count);
                 if (material.passCount > shaderPass)
                 {
                    commandBuffer.DrawMeshInstanced(mesh, 0, material, shaderPass, matrix4X4s, count);
                }
            }
        }

        private void StateChangedMethod(CullingGroupEvent evt)
        {
            if (blockState.Value.ContainsKey(evt.index))
            {
                blockState.Value[evt.index] = evt;
            }
            else
            {
                blockState.Value.Add(evt.index, evt);
            }
            needUpdateCommandBuffer = true;
            // InitCommandBuffer(); // 在下一次Update时更新
        }

        public void Dispose()
        {
            if (usingCamera != null)
            {
                CommandBuffer[] commandBuffers = usingCamera.GetCommandBuffers(usingCameraEvent);
                for(int i = 0; i < commandBuffers.Length; i++)
                {
                    if (commandBuffers[i].name == commandBufferName)
                    {
                        usingCamera.RemoveCommandBuffer(usingCameraEvent, commandBuffer);
                        break;
                    }
                }
            }
            if (commandBuffer != null)
            {
                commandBuffer.Dispose();
                commandBuffer = null;
            }
            blockState.Value.Clear();
            blockState = null;
            cullingGroup.Dispose();
            cullingGroup = null;
        }
    }
}
