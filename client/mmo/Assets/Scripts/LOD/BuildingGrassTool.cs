using UnityEngine;

[ExecuteInEditMode]
public class BuildingGrassTool : GrassTool
{
    Matrix4x4 lastLocalToWorldMatrix;
    Matrix4x4 lastLocalToWorldMatrixForObb;

    protected override void BeforeCameraRender(Camera camera)
    {
        if (camera.CompareTag("MainCamera") || camera.cameraType == CameraType.SceneView)
        {
            if (transform.localToWorldMatrix != lastLocalToWorldMatrix)
            {
                BuildingAreaUtility.Instance.UpdateBuildingAreaForGrassTool(this);
            }
            base.BeforeCameraRender(camera);
            if (!needUpdateBuildingAreaCull)
            {
                UpdateCommandBuffer(forceUpdateCommandBuffer);
                forceUpdateCommandBuffer = false;
            }
            lastLocalToWorldMatrix = transform.localToWorldMatrix;
        }
    }

    protected override void InitBoundingSpheres()
    {
        if (boundingSpheres != null && transform.localToWorldMatrix == lastLocalToWorldMatrix)
        {
            return;
        }
        base.InitBoundingSpheres();
    }

    protected override Vector4 GetBlockDataPackedSphere(BlockData blockData)
    {
        Vector4 packedSphere = transform.localToWorldMatrix.MultiplyPoint3x4(blockData.packedSphere);
        packedSphere.w = blockData.packedSphere.w;
        return packedSphere;
    }

    protected override void CalMatrix4X4s(Matrix4x4[] matrix4X4s, int count)
    {
        for (int i = 0; i < count; i++)
        {
            matrix4X4s[i] = transform.localToWorldMatrix * matrix4X4s[i];
        }
    }

    protected override OBB GetBlockDataOBBBounds(BlockData blockData, int index)
    {
        if (lastLocalToWorldMatrixForObb != transform.localToWorldMatrix)
        {
            lastLocalToWorldMatrixForObb = transform.localToWorldMatrix;
            for(int i = 0; i < blockData.obbLocalBounds.Length; i++)
            {
                blockData.obbBounds[i] = OBB.TransformToWorld(lastLocalToWorldMatrixForObb, blockData.obbLocalBounds[i]);
            }
        }
        return blockData.obbBounds[index];
    }

    protected override bool IsBlockDataInBuildingArea(BlockData blockData, BuildingArea area)
    {
        if (area.areaType == BuildingArea.BuildingAreaType.building)
        {
            return false;
        }
        return base.IsBlockDataInBuildingArea(blockData, area);
    }

    public override void Reset()
    {
        base.Reset();
        lastLocalToWorldMatrix = Matrix4x4.zero;
        lastLocalToWorldMatrixForObb = Matrix4x4.zero;
    }
}
