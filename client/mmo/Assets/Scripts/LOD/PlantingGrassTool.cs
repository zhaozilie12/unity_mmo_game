#if UNITY_EDITOR
using UnityEditor;
using Sirenix.OdinInspector.Editor;
#endif
using UnityEngine;
using Sirenix.OdinInspector;

[ExecuteInEditMode]
public class PlantingGrassTool : MonoBehaviour
{
#if UNITY_EDITOR
    public Vector2 center { get { return new Vector2(transform.position.x, transform.position.z); } }
    public Vector2 size { get { return new Vector2(Mathf.Abs(transform.localScale.x), Mathf.Abs(transform.localScale.z)); } }
    public float rotation { get { return transform.rotation.eulerAngles.y; } }

    public Color gizmosColor = Color.green;
    [AssetsOnly, PropertySpace, Required]
    public GameObject grassPrefab;
    [MinValue(0)]
    public int count = 10;
    public bool enableRandomSize = true;
    [ShowIf("enableRandomSize"), MinMaxSlider(0.5f, 2f)]
    public Vector2 randomSize = Vector2.one;
    [SceneObjectsOnly, InfoBox("scale isn't 1", InfoMessageType.Warning, "CheckPlantingGrassParent")]
    public Transform plantingGrassParent;
    
    private bool CheckPlantingGrassParent()
    {
        if (plantingGrassParent == null)
        {
            return false;
        }
        else
        {
            return plantingGrassParent.localScale != Vector3.one;
        }
    }

    [Button(ButtonSizes.Medium), PropertySpace]
    public void PlantingGrass()
    {
        if (plantingGrassParent == null)
        {
            plantingGrassParent = new GameObject("PlantingGrass").transform;
            plantingGrassParent.parent = transform.parent;
            plantingGrassParent.localPosition = Vector3.zero;
            plantingGrassParent.localRotation = Quaternion.identity;
            plantingGrassParent.localScale = Vector3.one;
        }
        for(int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(Random.Range(size.x * -0.5f, size.x * 0.5f), 0, Random.Range(size.y * -0.5f, size.y * 0.5f));
            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Vector3 scale = Vector3.one * (enableRandomSize ? Random.Range(randomSize.x, randomSize.y) : 1f);
            Matrix4x4 matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0, rotation, 0), Vector3.one);
            pos = matrix.MultiplyPoint3x4(pos);
            GameObject grassObj = PrefabUtility.InstantiatePrefab(grassPrefab, plantingGrassParent) as GameObject;
            grassObj.transform.position = pos;
            grassObj.transform.rotation = rot;
            grassObj.transform.localScale = scale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Selection.activeGameObject != gameObject)
        {
            return;
        }
        var cacheMatrix = Gizmos.matrix;
        var cacheColor = Gizmos.color;

        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0, rotation, 0), Vector3.one);

        Gizmos.color = gizmosColor;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0, size.y));

        Gizmos.color = cacheColor;
        Gizmos.matrix = cacheMatrix;

        // Gizmos.DrawSphere(new Vector3(P0.x, transform.position.y, P0.y), 0.1f);
        // Gizmos.DrawSphere(new Vector3(P1.x, transform.position.y, P1.y), 0.1f);
        // Gizmos.DrawSphere(new Vector3(P2.x, transform.position.y, P2.y), 0.1f);
        // Gizmos.DrawSphere(new Vector3(P3.x, transform.position.y, P3.y), 0.1f);
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlantingGrassTool))]
public class PlantingGrassToolEditor : OdinEditor
{
    PlantingGrassTool plantingGrassTool;

    protected override void OnEnable()
    {
        base.OnEnable();
        plantingGrassTool = target as PlantingGrassTool;
    }

    public override void OnInspectorGUI()
    {
        if(plantingGrassTool.gameObject.tag != "EditorOnly")
        {
            EditorGUILayout.HelpBox("需要将当前物体的Tag设置为EditorOnly，不要在此物体上实现其他功能", MessageType.Error);
        }
        else
        {
            base.OnInspectorGUI();
        }
    }
}
#endif