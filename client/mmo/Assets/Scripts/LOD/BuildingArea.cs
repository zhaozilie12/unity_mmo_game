using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BuildingArea : MonoBehaviour
{
    private Matrix4x4 lastLocalToWorldMatrix;
    private Matrix4x4 cacheLocalToWorldMatrix;
    private int frameCount = 0;

    public enum BuildingAreaType
    {
        building,
        brick
    }

    [DisableInPlayMode]
    public BuildingAreaType areaType = BuildingAreaType.brick;
    [DisableInPlayMode]
    public OBB.OBBLocal[] areas;
    private List<OBB> cacheAreas;

    private void UpdateCacheAreas()
    {
        bool needUpdate = cacheLocalToWorldMatrix != transform.localToWorldMatrix;
        cacheLocalToWorldMatrix = transform.localToWorldMatrix;
        if (cacheAreas == null)
        {
            cacheAreas = new List<OBB>(areas.Length);
        }
        for (int i = 0; i < areas.Length; i++)
        {
            OBB.OBBLocal obb = areas[i];
            if (i == cacheAreas.Count)
            {
                cacheAreas.Add(OBB.TransformToWorld(transform.localToWorldMatrix, obb));
            }
            else if (needUpdate)
            {
                cacheAreas[i] = OBB.TransformToWorld(transform.localToWorldMatrix, obb);
            }
        }
    }

    public bool Intersects(Plane[] planes)
    {
        if (areas != null)
        {
            UpdateCacheAreas();
            for (int i = 0; i < areas.Length; i++)
            {
                OBB area = cacheAreas[i];
                Bounds bound = new Bounds(area.Center, area.Size);
                if(GeometryUtility.TestPlanesAABB(planes, bound))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool Intersects(Vector4 packedSphere)
    {
        if (areas != null)
        {
            UpdateCacheAreas();
            Vector2 blockDataPos = new Vector2(packedSphere.x, packedSphere.z);
            // foreach (OBB.OBBLocal obb in areas)
            for (int i = 0; i < areas.Length; i++)
            {
                OBB area = cacheAreas[i];// OBB.TransformToWorld(transform.localToWorldMatrix, obb);
                Vector2 closestPoint = area.ClosestPointOBB(blockDataPos);
                if ((closestPoint - blockDataPos).sqrMagnitude < packedSphere.w * packedSphere.w)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool Intersects(OBB other)
    {
        if (areas != null)
        {
            UpdateCacheAreas();
            // foreach (OBB.OBBLocal obb in areas)
            for (int i = 0; i < areas.Length; i++)
            {
                OBB area = cacheAreas[i];// OBB.TransformToWorld(transform.localToWorldMatrix, obb);
                if (area.Intersects(other))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void OnEnable()
    {
        BuildingAreaUtility.Instance.AddBuildingArea(this);
    }

    private void OnDisable()
    {
        BuildingAreaUtility.Instance.RemoveBuildingArea(this);
    }

    private void OnValidate()
    {
        if (areas != null)
        {
            for (int i = 0; i < areas.Length; i++)
            {
                if (areas[i].size.x < 0f)
                {
                    areas[i].size.x = 0f;
                }
                if (areas[i].size.y < 0f)
                {
                    areas[i].size.y = 0f;
                }
                if(areas[i].center == Vector2.zero && areas[i].size == Vector2.zero && areas[i].rotation == 0f)
                {
                    areas[i].size = Vector2.one;
                }
            }
        }
        lastLocalToWorldMatrix = Matrix4x4.zero;
        cacheLocalToWorldMatrix = Matrix4x4.zero;
    }

    private void Update()
    {
        if (lastLocalToWorldMatrix != transform.localToWorldMatrix)
        {
            lastLocalToWorldMatrix = transform.localToWorldMatrix;
            frameCount = 0;
        }
        else if (frameCount > -1)
        {
            frameCount++;
        }
        if(frameCount > 5)
        {
            frameCount = -1;
            BuildingAreaUtility.Instance.UpdateBuildingAreaForGrassTool();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (areas == null)
        {
            return;
        }

        var cacheMatrix = Gizmos.matrix;
        var cacheColor = Gizmos.color;

        foreach (OBB.OBBLocal obb in areas)
        {
            OBB area = OBB.TransformToWorld(transform.localToWorldMatrix, obb);
            Vector3 pos = area.Center;
            pos.y = transform.position.y;
            Gizmos.matrix = Matrix4x4.TRS(pos, area.Rotation, Vector3.one);
            Gizmos.color = areaType == BuildingAreaType.building ? Color.magenta : Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, area.Size);
        }

        Gizmos.color = cacheColor;
        Gizmos.matrix = cacheMatrix;

        // Gizmos.DrawSphere(new Vector3(P0.x, transform.position.y, P0.y), 0.1f);
        // Gizmos.DrawSphere(new Vector3(P1.x, transform.position.y, P1.y), 0.1f);
        // Gizmos.DrawSphere(new Vector3(P2.x, transform.position.y, P2.y), 0.1f);
        // Gizmos.DrawSphere(new Vector3(P3.x, transform.position.y, P3.y), 0.1f);
    }
}

public class BuildingAreaUtility
{
    private static BuildingAreaUtility buildingAreaUtility;

    public static BuildingAreaUtility Instance
    {
        get
        {
            if(buildingAreaUtility == null)
            {
                buildingAreaUtility = new BuildingAreaUtility();
            }
            return buildingAreaUtility;
        }
    }

    HashSet<BuildingArea> buildingAreas = new HashSet<BuildingArea>();
    HashSet<GrassTool> grassTools = new HashSet<GrassTool>();

    public void AddBuildingArea(BuildingArea buildingArea)
    {
        if (buildingAreas.Add(buildingArea))
        {
            UpdateBuildingAreaForGrassTool();
        }
    }

    public void RemoveBuildingArea(BuildingArea buildingArea)
    {
        if (buildingAreas.Remove(buildingArea))
        {
            UpdateBuildingAreaForGrassTool();
        }
    }

    public void AddGrassTool(GrassTool grassTool)
    {
        grassTools.Add(grassTool);
        UpdateBuildingAreaForGrassTool(grassTool);
    }

    public void RemoveGrassTool(GrassTool grassTool)
    {
        grassTools.Remove(grassTool);
    }

    public void UpdateBuildingAreaForGrassTool()
    {
        foreach(GrassTool grassTool in grassTools)
        {
            UpdateBuildingAreaForGrassTool(grassTool);
        }
    }

    public void UpdateBuildingAreaForGrassTool(GrassTool grassTool)
    {
        if(grassTool != null)
        {
            grassTool.SetBuildingAreas(buildingAreas);
        }
    }
}

[System.Serializable]
public struct OBB
{
    [SerializeField, HideInInspector]
    private Vector2 center;
    [SerializeField, HideInInspector]
    private Vector2 size;
    [SerializeField, HideInInspector]
    private float rotation;

    public Vector3 Center
    {
        get
        {
            return new Vector3(center.x, 0, center.y);
        }
    }

    public Vector3 Size
    {
        get
        {
            return new Vector3(size.x, 0, size.y);
        }
    }

    public Quaternion Rotation
    {
        get
        {
            return Quaternion.Euler(0, rotation, 0);
        }
    }

    [SerializeField, HideInInspector]
    Vector2 halfSize;

    [SerializeField, HideInInspector]
    Vector2 p0;
    [SerializeField, HideInInspector]
    Vector2 p1;
    [SerializeField, HideInInspector]
    Vector2 p2;
    [SerializeField, HideInInspector]
    Vector2 p3;

    [SerializeField, HideInInspector]
    Vector2 axisX;
    [SerializeField, HideInInspector]
    Vector2 axisY;

    [SerializeField, HideInInspector]
    float minAxisX;
    [SerializeField, HideInInspector]
    float maxAxisX;
    [SerializeField, HideInInspector]
    float minAxisY;
    [SerializeField, HideInInspector]
    float maxAxisY;

    [System.Serializable]
    public struct OBBLocal
    {
        public Vector2 center;
        public Vector2 size;
        public float rotation;
        public OBBLocal(Vector2 currentCenter, Vector2 currentSize, float currentRotation)
        {
            center = currentCenter;
            size = currentSize;
            rotation = currentRotation;
        }
    }

    public OBB(Vector2 currentCenter, Vector2 currentSize, float currentRotation)
    {
        center = currentCenter;
        size = currentSize;
        rotation = currentRotation;

        Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(center.x, 0, center.y), Quaternion.Euler(0, rotation, 0), Vector3.one);
        halfSize = size * 0.5f;

        Vector3 vector3 = matrix.MultiplyPoint3x4(new Vector3(-halfSize.x, 0, -halfSize.y));
        p0 = new Vector2(vector3.x, vector3.z);
        vector3 = matrix.MultiplyPoint3x4(new Vector3(halfSize.x, 0, -halfSize.y));
        p1 = new Vector2(vector3.x, vector3.z);
        vector3 = matrix.MultiplyPoint3x4(new Vector3(halfSize.x, 0, halfSize.y));
        p2 = new Vector2(vector3.x, vector3.z);
        vector3 = matrix.MultiplyPoint3x4(new Vector3(-halfSize.x, 0, halfSize.y));
        p3 = new Vector2(vector3.x, vector3.z);
        axisX = (p1 - p0).normalized;
        axisY = (p3 - p0).normalized;
        Vector3 p0ProjectAxisX = Vector3.Project(p0, axisX);
        Vector3 p1ProjectAxisX = Vector3.Project(p1, axisX);
        Vector3 p2ProjectAxisX = Vector3.Project(p2, axisX);
        Vector3 p3ProjectAxisX = Vector3.Project(p3, axisX);
        float x_p0 = p0ProjectAxisX.sqrMagnitude * Mathf.Sign(Vector3.Dot(p0ProjectAxisX, axisX));
        float x_p1 = p1ProjectAxisX.sqrMagnitude * Mathf.Sign(Vector3.Dot(p1ProjectAxisX, axisX));
        float x_p2 = p2ProjectAxisX.sqrMagnitude * Mathf.Sign(Vector3.Dot(p2ProjectAxisX, axisX));
        float x_p3 = p3ProjectAxisX.sqrMagnitude * Mathf.Sign(Vector3.Dot(p3ProjectAxisX, axisX));
        minAxisX = Mathf.Min(Mathf.Min(Mathf.Min(x_p0, x_p1), x_p2), x_p3);
        maxAxisX = Mathf.Max(Mathf.Max(Mathf.Max(x_p0, x_p1), x_p2), x_p3);
        Vector3 p0ProjectAxisY = Vector3.Project(p0, axisY);
        Vector3 p1ProjectAxisY = Vector3.Project(p1, axisY);
        Vector3 p2ProjectAxisY = Vector3.Project(p2, axisY);
        Vector3 p3ProjectAxisY = Vector3.Project(p3, axisY);
        float y_p0 = p0ProjectAxisY.sqrMagnitude * Mathf.Sign(Vector3.Dot(p0ProjectAxisY, axisY));
        float y_p1 = p1ProjectAxisY.sqrMagnitude * Mathf.Sign(Vector3.Dot(p1ProjectAxisY, axisY));
        float y_p2 = p2ProjectAxisY.sqrMagnitude * Mathf.Sign(Vector3.Dot(p2ProjectAxisY, axisY));
        float y_p3 = p3ProjectAxisY.sqrMagnitude * Mathf.Sign(Vector3.Dot(p3ProjectAxisY, axisY));
        minAxisY = Mathf.Min(Mathf.Min(Mathf.Min(y_p0, y_p1), y_p2), y_p3);
        maxAxisY = Mathf.Max(Mathf.Max(Mathf.Max(y_p0, y_p1), y_p2), y_p3);
    }

    public Vector2 ClosestPointOBB(Vector2 p)
    {
        Vector2 d = p - center;
        Vector2 q = center;

        var axis1 = axisX;// (p1 - p0).normalized;
        var axis2 = axisY;// (p3 - p0).normalized;

        float distX = Vector2.Dot(d, axis1);       // dist为x轴中p到中心点的距离，由x=(P-C)·Ux得来
        if (distX > halfSize.x)                    // 距离超过边界时，为OBB范围的一半
        {
            distX = halfSize.x;
        }
        else if (distX < -halfSize.x)
        {
            distX = -halfSize.x;
        }
        q += distX * axis1;

        float distY = Vector2.Dot(d, axis2);
        if (distY > halfSize.y)
        {
            distY = halfSize.y;
        }
        else if (distY < -halfSize.y)
        {
            distY = -halfSize.y;
        }
        q += distY * axis2;
        return q;
    }

    public bool Intersects(OBB other)
    {
        // var axis1 = (p1 - p0).normalized;
        // var axis2 = (p3 - p0).normalized;
        // var axis3 = (other.p1 - other.p0).normalized;
        // var axis4 = (other.p3 - other.p0).normalized;

        var isNotIntersect = false;
        isNotIntersect |= ProjectionIsNotIntersect(this, other, axisX, false, minAxisX, maxAxisX);
        isNotIntersect |= ProjectionIsNotIntersect(this, other, axisY, false, minAxisY, maxAxisY);
        isNotIntersect |= ProjectionIsNotIntersect(this, other, other.axisX, true, other.minAxisX, other.maxAxisX);
        isNotIntersect |= ProjectionIsNotIntersect(this, other, other.axisY, true, other.minAxisY, other.maxAxisY);

        return !isNotIntersect;
    }

    bool ProjectionIsNotIntersect(OBB x, OBB y, Vector2 axis, bool isCalYAxis, float min, float max)
    {
        Vector3 p0ProjectAxisX = Vector3.Project(x.p0, axis);
        Vector3 p1ProjectAxisX = Vector3.Project(x.p1, axis);
        Vector3 p2ProjectAxisX = Vector3.Project(x.p2, axis);
        Vector3 p3ProjectAxisX = Vector3.Project(x.p3, axis);
        Vector3 p0ProjectAxisY = Vector3.Project(y.p0, axis);
        Vector3 p1ProjectAxisY = Vector3.Project(y.p1, axis);
        Vector3 p2ProjectAxisY = Vector3.Project(y.p2, axis);
        Vector3 p3ProjectAxisY = Vector3.Project(y.p3, axis);
        float x_p0 = p0ProjectAxisX.sqrMagnitude * Mathf.Sign(Vector3.Dot(p0ProjectAxisX, axis));
        float x_p1 = p1ProjectAxisX.sqrMagnitude * Mathf.Sign(Vector3.Dot(p1ProjectAxisX, axis));
        float x_p2 = p2ProjectAxisX.sqrMagnitude * Mathf.Sign(Vector3.Dot(p2ProjectAxisX, axis));
        float x_p3 = p3ProjectAxisX.sqrMagnitude * Mathf.Sign(Vector3.Dot(p3ProjectAxisX, axis));
        float y_p0 = p0ProjectAxisY.sqrMagnitude * Mathf.Sign(Vector3.Dot(p0ProjectAxisY, axis));
        float y_p1 = p1ProjectAxisY.sqrMagnitude * Mathf.Sign(Vector3.Dot(p1ProjectAxisY, axis));
        float y_p2 = p2ProjectAxisY.sqrMagnitude * Mathf.Sign(Vector3.Dot(p2ProjectAxisY, axis));
        float y_p3 = p3ProjectAxisY.sqrMagnitude * Mathf.Sign(Vector3.Dot(p3ProjectAxisY, axis));

        float xMin = min;
        float xMax = max;
        float yMin = min;
        float yMax = max;
        if (isCalYAxis)
        {
            xMin = Mathf.Min(Mathf.Min(Mathf.Min(x_p0, x_p1), x_p2), x_p3);
            xMax = Mathf.Max(Mathf.Max(Mathf.Max(x_p0, x_p1), x_p2), x_p3);
        }
        else
        {
            yMin = Mathf.Min(Mathf.Min(Mathf.Min(y_p0, y_p1), y_p2), y_p3);
            yMax = Mathf.Max(Mathf.Max(Mathf.Max(y_p0, y_p1), y_p2), y_p3);
        }
        
        if (yMin > xMin && yMin < xMax) return false;
        if (yMax > xMin && yMax < xMax) return false;
        if (xMin > yMin && xMin < yMax) return false;
        if (xMax > yMin && xMax < yMax) return false;

        return true;
    }

    public static OBBLocal TransformToLocal(Matrix4x4 worldToLocal, OBB obb)
    {
        Vector3 center = new Vector3(obb.center.x, 0, obb.center.y);
        center = worldToLocal.MultiplyPoint3x4(center);
        float rotation = obb.rotation - worldToLocal.rotation.eulerAngles.y;
        return new OBBLocal(new Vector2(center.x, center.z), obb.size, rotation);
    }

    public static OBB TransformToWorld(Matrix4x4 localToWorld, OBBLocal obb)
    {
        Vector3 center = new Vector3(obb.center.x, 0, obb.center.y);
        center = localToWorld.MultiplyPoint3x4(center);
        float rotation = obb.rotation + localToWorld.rotation.eulerAngles.y;
        return new OBB(new Vector2(center.x, center.z), obb.size, rotation);
    }
}