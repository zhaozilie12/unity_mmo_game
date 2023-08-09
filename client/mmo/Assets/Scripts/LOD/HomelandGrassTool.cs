using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class HomelandGrassTool : GrassTool
{
    //[Space, LabelText("LOD Distance Curve(x-LOD Bias, Y-Distance)")]
    //public AnimationCurve lodDistanceCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0, 0, 0), new Keyframe(0.4f, 0, 0, 62.5f) , new Keyframe(2, 100, 62.5f, 0)});
    public float DistanceLOD0 = 45;
    public float DistanceLOD1 = 20;
    float lodDistance { get; set; }
    float[] lod = new float[2] { float.PositiveInfinity, float.PositiveInfinity};

    protected override void BeforeCameraRender(Camera camera)
    {
        //float lodIndex = (int) QualitySettings.currentLevel;
        float lodIndex = 2f;
        //lodDistance = Mathf.Max(0, lodDistanceCurve.Evaluate(QualitySettings.lodBias));
        lodDistance = (lodIndex == 2 ? DistanceLOD0 : (lodIndex == 1 ? DistanceLOD1 : 0));
        
        if (lodDistance <= float.Epsilon)
        {
            Reset();
            return;
        }
        
        Shader.SetGlobalFloat("_GrassLODFadeDistance", (lodDistance));

        if (camera.CompareTag("MainCamera") || camera.cameraType == CameraType.SceneView)
        {
            base.BeforeCameraRender(camera);
            UpdateLodDistance();
            if (!needUpdateBuildingAreaCull)
            {
                UpdateCommandBuffer(forceUpdateCommandBuffer);
                forceUpdateCommandBuffer = false;
            }
        }
    }
    protected override void InitBoundingSpheres()
    {
        if (boundingSpheres != null)
        {
            return;
        }
        base.InitBoundingSpheres();
    }

    protected override void CalBuildingAreasCulling(Camera camera)
    {
        if(useSearchInfo == false)
        {
            base.CalBuildingAreasCulling(camera);
            return;
        }
        if (needUpdateBuildingAreaCull == false)
        {
            return;
        }
        // Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        float startTime = Time.realtimeSinceStartup;

        // 重置每一块的剔除状�?
        if (treeAreaCullIndex == 0)// && finishUpdateImportant == false)
        {
            foreach (TreeData block in treeBlocks)
            {
                foreach (BlockData blockData in block.blockDatas)
                {
                    blockData.drawSize = blockData.treeTransform.Length;
                }
            }
        }

        if (buildingAreas.Count > 0)
        {
            int i = treeAreaCullIndex / buildingAreas.Count;
            int currentIndex = i * buildingAreas.Count - 1;
            for (; i < treeBlocks.Length; i++)
            {
                TreeData block = treeBlocks[i];
                SearchInfo[] searchInfos = block.searchInfos;
                if (searchInfos == null || searchInfos.Length == 0)
                {
                    continue;
                }
                foreach (BuildingArea area in buildingAreas)
                {
                    currentIndex++;
                    if (currentIndex < treeAreaCullIndex)
                    {
                        continue;
                    }
                    treeAreaCullIndex++;
                    if (area == null)
                    {
                        continue;
                    }
                    // if (finishUpdateImportant == false)
                    // {
                    //     if (!area.Intersects(planes))
                    //     {
                    //         continue;
                    //     }
                    // }
                    CalBuildingAreasCulling(block.blockDatas, searchInfos, searchInfos[0], area);
                    float deltaTime = Time.realtimeSinceStartup - startTime;
                    if (deltaTime > buildingAreaUpdateTime)
                    {
                        return;
                    }
                }
            }
        }
        // if (finishUpdateImportant == false)
        // {
        //     finishUpdateImportant = true;
        //     finishedCullIndex = 0;
        //     forceUpdateCommandBuffer = true;
        //     return;
        // }
        needUpdateBuildingAreaCull = false;
        forceUpdateCommandBuffer = true;
    }

    void CalBuildingAreasCulling(BlockData[] blockDatas, SearchInfo[] searchInfos, SearchInfo searchInfo, BuildingArea area)
    {
        if (!IsSearchInfoInBuildingArea(searchInfo, area))
        {
            return;
        }
        else
        {
            for (int i = 0; i < searchInfo.nextSearchInfoIndexs.Count; i++)
            {
                CalBuildingAreasCulling(blockDatas, searchInfos, searchInfos[searchInfo.nextSearchInfoIndexs[i]], area);
            }
            for (int i = 0; i < searchInfo.blockIndexs.Count; i++)
            {
                BlockData blockData = blockDatas[searchInfo.blockIndexs[i]];
                if (blockData.drawSize <= 0)
                {
                    continue;
                }
                bool isBlockDataInBuildingArea = IsBlockDataInBuildingArea(blockData, area);
                if (!isBlockDataInBuildingArea)
                {
                    continue;
                }
                else if (blockData.drawSize == 1)
                {
                    blockData.drawSize = 0;
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
        }
    }

    protected override bool IsBlockDataInBuildingArea(BlockData blockData, BuildingArea area)
    {
        OBB packedObbBound = blockData.drawSize == 1 ? blockData.obbBounds[0] : blockData.packedObbBound;
        return area.Intersects(packedObbBound);
    }

    private void UpdateLodDistance()
    {
        if (Mathf.Abs(lod[0] - lodDistance) > 0.001f || forceUpdateCommandBuffer)
        {
            lod[0] = lodDistance;
            foreach (StateChangedMethodClass cullingGroup in cullingGroups)
            {
                cullingGroup.SetLODDistance(lod);
            }
        }
    }

    public override void Reset()
    {
        base.Reset();
        lod[0] = float.PositiveInfinity;

    }
}
