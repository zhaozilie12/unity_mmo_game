using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs;
using MightyTerrainMesh;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;

public class MTLODSetting : MeshLODCreate
{
    public bool bEditorUIFoldout = true;
    public virtual void OnGUIDraw(int idx)
    {
        bEditorUIFoldout = EditorGUILayout.Foldout(bEditorUIFoldout, string.Format("LOD {0}", idx));
        if (bEditorUIFoldout)
        {
            EditorGUI.indentLevel++;
            int curRate = Mathf.FloorToInt(Mathf.Pow(2, Subdivision));
            int sampleRate = EditorGUILayout.IntField("Sample(NxN)", curRate);
            if (curRate != sampleRate)
            {
                curRate = Mathf.NextPowerOfTwo(sampleRate);
                Subdivision = Mathf.FloorToInt(Mathf.Log(curRate, 2));
            }
            var error = EditorGUILayout.FloatField("Slope Angle Error", SlopeAngleError);
            SlopeAngleError = Mathf.Max(0.01f, error);
            EditorGUI.indentLevel--;
        }
    }
}
/*
internal class MeshTextureBaker
{
    public Vector2 uvMin { get; private set; }
    public Vector2 uvMax { get; private set; }
    public Material layer0 { get; private set; }
    public Material layer1 { get; private set; }
    public Texture2D BakeResult { get; set; }
    public int Size { get { return texSize; } }
    private int texSize = 32;
    public MeshTextureBaker(int size, Vector2 min, Vector2 max, Material m0, Material m1)
    {
        texSize = size;
        uvMin = min;
        uvMax = max;
        layer0 = m0;
        layer1 = m1;
        BakeResult = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);
    }
}
*/
internal class MeshPrefabBaker
{
    public int lod { get; private set; }
    public int meshId { get; private set; }
    public Mesh mesh { get; private set; }
    public Vector4 scaleOffset { get; private set; }
    public MeshPrefabBaker(int i, int mid, Mesh m, Vector2 uvMin, Vector2 uvMax)
    {
        lod = i;
        meshId = mid;
        mesh = m;
        var v = new Vector4(1, 1, 0, 0);
        v.x = uvMax.x - uvMin.x;
        v.y = uvMax.y - uvMin.y;
        v.z = uvMin.x;
        v.w = uvMin.y;
        scaleOffset = v;
    }
}

/*
public class GrassProcessWindow : OdinEditorWindow
{
    [MenuItem("Tools/Homeland/草地转换工具")]
    public static GrassProcessWindow ShowWindow()
    {
        return OdinEditorWindow.GetWindow<GrassProcessWindow>("草地转换工具");
    }

    [System.Serializable]
    public class TreeInfo
    {
        [AssetsOnly, PropertyOrder(0)]
        public MeshRenderer prefab;
        [SceneObjectsOnly, PropertyOrder(1)]
        public Transform instanceParent;
    }

    [System.Serializable]
    public class HomelandTreeInfo : TreeInfo
    {
        [AssetsOnly, PropertyOrder(0)]
        public MeshRenderer lodPrefab;
    }

    [BoxGroup("地表草地转换"), SceneObjectsOnly]
    public HomelandGrassTool homelandgrassTool;
    [BoxGroup("地表草地转换")]
    public List<HomelandTreeInfo> treeInfos = new List<HomelandTreeInfo>();
    Vector4 blockPosition = new Vector4(-1000, 0, -1000, 10000);
    [BoxGroup("地表草地转换"), PropertyRange(1, 32)]
    public int combineBlockCount = 4;
    [BoxGroup("地表草地转换"), PropertyRange(1.5f, 50f)]
    public float maxBlockRadius = 4f;

    [BoxGroup("地表草地转换"), Button("Generate", ButtonSizes.Medium)]
    public void GenerateHomelandGrassTool()
    {
        if (homelandgrassTool == null)
        {
            MTLog.LogError("no HomelandGrassTool");
            return;
        }
        List<TreeInfo> infos = new List<TreeInfo>();
        foreach(HomelandTreeInfo homelandTreeInfo in treeInfos)
        {
            infos.Add(homelandTreeInfo);
        }
        GetGrassPosSetData(homelandgrassTool, infos, combineBlockCount, maxBlockRadius);
    }

    [HideLabel, DisplayAsString]
    public string space = ""; // 空格占位

    [BoxGroup("建筑草地转换"), SceneObjectsOnly]
    public BuildingGrassTool buildingGrassTool;
    [BoxGroup("建筑草地转换")]
    public List<TreeInfo> buildingTreeInfos = new List<TreeInfo>();

    [BoxGroup("建筑草地转换"), Button("Generate", ButtonSizes.Medium)]
    public void GenerateBuildingGrassTool()
    {
        if (buildingGrassTool == null)
        {
            MTLog.LogError("no BuildingGrassTool");
            return;
        }
        GetGrassPosSetData(buildingGrassTool, buildingTreeInfos, 1, 50f);
        for (int i = 0; i < buildingGrassTool.treeBlocks.Length; i++)
        {
            for (int j = 0; j < buildingGrassTool.treeBlocks[i].blockDatas.Length; j++)
            {
                Vector3 localPos = buildingGrassTool.transform.worldToLocalMatrix.MultiplyPoint3x4(buildingGrassTool.treeBlocks[i].blockDatas[j].packedSphere);
                buildingGrassTool.treeBlocks[i].blockDatas[j].packedSphere = new Vector4(localPos.x, localPos.y, localPos.z, buildingGrassTool.treeBlocks[i].blockDatas[j].packedSphere.w);
                for (int k = 0; k < buildingGrassTool.treeBlocks[i].blockDatas[j].treeTransform.Length; k++)
                {
                    buildingGrassTool.treeBlocks[i].blockDatas[j].treeTransform[k] = buildingGrassTool.transform.worldToLocalMatrix * buildingGrassTool.treeBlocks[i].blockDatas[j].treeTransform[k];
                }
                for (int k = 0; k < buildingGrassTool.treeBlocks[i].blockDatas[j].obbBounds.Length; k++)
                {
                    buildingGrassTool.treeBlocks[i].blockDatas[j].obbBounds[k] = OBB.TransformToLocal(buildingGrassTool.transform.worldToLocalMatrix, buildingGrassTool.treeBlocks[i].blockDatas[j].obbBounds[k]);
                }
            }
        }
    }

    public void GetGrassPosSetData(GrassTool grassTool, List<TreeInfo> treeInfos, int combineBlockCount, float maxBlockRadius)
    {
        // 按树的种类进行分类
        int treeTypeCount = treeInfos.Count;
        List<Transform>[] treeTypes = new List<Transform>[treeTypeCount];
        for (int i = 0; i < treeTypeCount; i++)
        {
            treeTypes[i] = new List<Transform>();
        }
        for (int i = 0; i < treeTypeCount; i++)
        {
            for (int j = 0; j < treeInfos[i].instanceParent.childCount; j++)
            {
                treeTypes[i].Add(treeInfos[i].instanceParent.GetChild(j));
            }
        }

        // 划分方块
        List<GrassTool.TreeData> TreeBlocks = new List<GrassTool.TreeData>();
        for (int i = 0; i < treeTypes.Length; i++)
        {
            List<Transform> treeType = treeTypes[i];
            GrassTool.TreeData treeBlock = new GrassTool.TreeData();
            MeshRenderer meshRenderer = treeInfos[i].prefab;
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
            treeBlock.treeMesh = meshFilter.sharedMesh;
            treeBlock.treeMaterial = meshRenderer.sharedMaterial;
            if ((treeInfos[i] as HomelandTreeInfo) != null && (treeInfos[i] as HomelandTreeInfo).lodPrefab != null)
            {
                MeshRenderer lodMeshRenderer = (treeInfos[i] as HomelandTreeInfo).lodPrefab;
                MeshFilter lodMeshFilter = lodMeshRenderer.GetComponent<MeshFilter>();
                treeBlock.lodTreeMesh = lodMeshFilter.sharedMesh;
                treeBlock.lodTreeMaterial = lodMeshRenderer.sharedMaterial;
            }
            List<GrassTool.BlockData> blockDatas = new List<GrassTool.BlockData>();
            SplitTreeInstances(meshRenderer.bounds, ref blockDatas, treeType, blockPosition, combineBlockCount, maxBlockRadius);
            if (blockDatas.Count != 0)
            {
                treeBlock.blockDatas = blockDatas.ToArray();
                TreeBlocks.Add(treeBlock);
            }
        }
        SetTreeBlocksData(grassTool, TreeBlocks.ToArray());
    }

    public void SplitTreeInstances(Bounds treeBounds, ref List<GrassTool.BlockData> blockDatas, List<Transform> treeType, Vector4 blockPosition, int combineBlockCount, float maxBlockRadius)
    {
        // 选出在区域内的草
        List<int> inBlockTreeIndexs = new List<int>();
        for (int i = 0; i < treeType.Count; i++)
        {
            Transform treeInstance = treeType[i];
            Vector3 pos = treeInstance.position;
            if (pos.x >= blockPosition.x && pos.x < blockPosition.x + blockPosition.w && pos.z >= blockPosition.z && pos.z < blockPosition.z + blockPosition.w)
            {
                inBlockTreeIndexs.Add(i);
            }
        }

        int maxGrassCount = 1023 / combineBlockCount; // 1023 - max size for gpu instance
        float currentRad = blockPosition.w * Mathf.Sqrt(0.5f);// (Mathf.Sqrt(2) * 0.5f);
        float halfSize = blockPosition.w * 0.5f;
        // 决定是否生成新分块
        if (inBlockTreeIndexs.Count != 0 && (inBlockTreeIndexs.Count > maxGrassCount || currentRad > maxBlockRadius))
        {
            Vector4 smallBlockPosition1 = new Vector4(blockPosition.x, blockPosition.y, blockPosition.z, halfSize);
            Vector4 smallBlockPosition2 = new Vector4(blockPosition.x + halfSize, blockPosition.y, blockPosition.z, halfSize);
            Vector4 smallBlockPosition3 = new Vector4(blockPosition.x, blockPosition.y, blockPosition.z + halfSize, halfSize);
            Vector4 smallBlockPosition4 = new Vector4(blockPosition.x + halfSize, blockPosition.y, blockPosition.z + halfSize, halfSize);
            SplitTreeInstances(treeBounds, ref blockDatas, treeType, smallBlockPosition1, combineBlockCount, maxBlockRadius);
            SplitTreeInstances(treeBounds, ref blockDatas, treeType, smallBlockPosition2, combineBlockCount, maxBlockRadius);
            SplitTreeInstances(treeBounds, ref blockDatas, treeType, smallBlockPosition3, combineBlockCount, maxBlockRadius);
            SplitTreeInstances(treeBounds, ref blockDatas, treeType, smallBlockPosition4, combineBlockCount, maxBlockRadius);
        }
        else
        {
            GrassTool.BlockData blockData = new GrassTool.BlockData();
            Bounds totalBounds = new Bounds();
            List<Matrix4x4> matrix4X4s = new List<Matrix4x4>();
            List<OBB> obbBounds = new List<OBB>();
            for (int i = 0; i < inBlockTreeIndexs.Count; i++)
            {
                Transform treeInstance = treeType[inBlockTreeIndexs[i]];
                Vector3 pos = treeInstance.position;
                Vector3 scale = treeInstance.localScale;
                matrix4X4s.Add(treeInstance.localToWorldMatrix);
                obbBounds.Add(new OBB(new Vector2(treeInstance.position.x, treeInstance.position.z), new Vector2(treeBounds.size.x, treeBounds.size.z), treeInstance.rotation.eulerAngles.y));
                float width = new Vector2(treeBounds.size.x, treeBounds.size.z).magnitude * scale.x;
                float height = treeBounds.size.y * scale.y;
                Vector3 size = new Vector3(width, height, width);
                Bounds newBounds = new Bounds(pos + treeBounds.center, size);
                if (i == 0)
                {
                    totalBounds = newBounds;
                }
                totalBounds.Encapsulate(pos);
            }
            // blockData.packedSphere = new Vector4(blockPosition.x + halfSize, blockPosition.y, blockPosition.z + halfSize, currentRad);
            blockData.packedSphere = new Vector4(totalBounds.center.x, totalBounds.center.y, totalBounds.center.z, totalBounds.extents.magnitude);
            if (matrix4X4s.Count != 0)
            {
                blockData.treeTransform = matrix4X4s.ToArray();
                blockData.drawSize = matrix4X4s.Count;
                blockData.obbBounds = obbBounds.ToArray();
                blockDatas.Add(blockData);
            }
        }
    }

    public bool SetTreeBlocksData(GrassTool grassTool, GrassTool.TreeData[] datas)
    {
        // 检查数据合法性
        if (datas == null)
        {
            return false;
        }
        for (int i = 0; i < datas.Length; i++)
        {
            GrassTool.TreeData treeBlock = datas[i];
            if (treeBlock == null)
            {
                return false;
            }
            GrassTool.BlockData[] blockDatas = treeBlock.blockDatas;
            if (blockDatas == null)
            {
                return false;
            }
            for (int j = 0; j < blockDatas.Length; j++)
            {
                GrassTool.BlockData blockData = blockDatas[j];
                if (blockData == null || blockData.treeTransform == null)
                {
                    return false;
                }
            }
        }
        // 设置数据并预处理
        grassTool.treeBlocks = datas;
        grassTool.totalBlockDataCount = CalBlockCount(grassTool);
        grassTool.Reset();
        return true;
    }

    int CalBlockCount(GrassTool grassTool)
    {
        int total = 0;
        for (int i = 0; i < grassTool.treeBlocks.Length; i++)
        {
            total += grassTool.treeBlocks[i].blockDatas.Length;
        }
        return total;
    }
}
*/

public class TerrainMeshEditor : EditorWindow
{
    [MenuItem("DevTools/家园/地形导出工具")]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow<TerrainMeshEditor>("地形导出工具");
    }
    //properties
    private int QuadTreeDepth = 2;
    private MTLODSetting[] LODSettings = new MTLODSetting[0];
    private Terrain terrainTarget;
    private bool genUV2 = false;
    private int lodCount = 1;
    // private bool bakeMaterial = false;
    // private int bakeTextureSize = 2048;
    //
    private CreateMeshJob dataCreateJob;
    private TessellationJob tessellationJob;

    Vector2 slider = Vector2.zero;

    private void OnGUI()
    {
        slider = EditorGUILayout.BeginScrollView(slider);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("导出Mesh");

        EditorGUI.indentLevel++;
        Terrain curentTarget = EditorGUILayout.ObjectField("Convert Target", terrainTarget, typeof(Terrain), true) as Terrain;
        if (curentTarget != terrainTarget)
        {
            terrainTarget = curentTarget;
        }

        int curSliceCount = Mathf.FloorToInt(Mathf.Pow(2, QuadTreeDepth));
        int sliceCount = 1;// EditorGUILayout.IntField("Slice Count(NxN)", curSliceCount);
        if (sliceCount != curSliceCount)
        {
            curSliceCount = Mathf.NextPowerOfTwo(sliceCount);
            QuadTreeDepth = Mathf.FloorToInt(Mathf.Log(curSliceCount, 2));
        }
        if (lodCount != LODSettings.Length)
        {
            MTLODSetting[] old = LODSettings;
            LODSettings = new MTLODSetting[lodCount];
            for (int i=0; i<Mathf.Min(lodCount, old.Length); ++i)
            {
                LODSettings[i] = old[i];
            }
            for (int i = Mathf.Min(lodCount, old.Length); i < Mathf.Max(lodCount, old.Length); ++i)
            {
                LODSettings[i] = new MTLODSetting();
            }
        }
        // lodCount = EditorGUILayout.IntField("LOD Count", LODSettings.Length);
        if (LODSettings.Length > 0)
        {
            for (int i = 0; i < LODSettings.Length; ++i)
            {
                LODSettings[i].OnGUIDraw(i);
            }
        }

        EditorGUI.indentLevel--;
        // bakeMaterial = EditorGUILayout.ToggleLeft("Bake Material", bakeMaterial);
        // if (bakeMaterial)
        // {
        //     bakeTextureSize = EditorGUILayout.IntField("Bake Texture Size", bakeTextureSize);
        //     bakeTextureSize = Mathf.NextPowerOfTwo(bakeTextureSize);
        // }
        // genUV2 = EditorGUILayout.ToggleLeft("Generate UV2", genUV2);
        if (GUILayout.Button("Generate"))
        {
            GenerateMesh(terrainTarget, LODSettings, dataCreateJob, tessellationJob, QuadTreeDepth, genUV2);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("导出植物");
        EditorGUI.indentLevel++;
        curentTarget = EditorGUILayout.ObjectField("Convert Target", terrainTarget, typeof(Terrain), true) as Terrain;
        if (curentTarget != terrainTarget)
        {
            terrainTarget = curentTarget;
        }
        EditorGUI.indentLevel--;
        if (GUILayout.Button("Generate"))
        {
            GenerateTree(terrainTarget);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    public static string GenerateMesh(Terrain currentTarget, MTLODSetting[] LODSettings, CreateMeshJob dataCreateJob, TessellationJob tessellationJob, int QuadTreeDepth, bool genUV2)
    {
        if (LODSettings == null || LODSettings.Length == 0)
        {
            MTLog.LogError("no lod setting");
            return null;
        }
        if (currentTarget == null)
        {
            MTLog.LogError("no target terrain");
            return null;
        }
        if (string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
        {
            MTLog.LogError("scene not saved");
            return null;
        }
        int gridMax = 1 << QuadTreeDepth;
        var tBnd = new Bounds(currentTarget.transform.TransformPoint(currentTarget.terrainData.bounds.center),
        currentTarget.terrainData.bounds.size);
        dataCreateJob = new CreateMeshJob(currentTarget, tBnd, gridMax, gridMax, LODSettings);
        for (int i = 0; i < int.MaxValue; ++i)
        {
            dataCreateJob.Update();
            EditorUtility.DisplayProgressBar("creating data", "scaning volumn", dataCreateJob.progress);
            if (dataCreateJob.IsDone)
                break;
        }
        dataCreateJob.EndProcess();
        //caculate min_tri size
        int max_sub = 1;
        foreach (var setting in LODSettings)
        {
            if (setting.Subdivision > max_sub)
                max_sub = setting.Subdivision;
        }
        float max_sub_grids = gridMax * (1 << max_sub);
        float minArea = Mathf.Max(currentTarget.terrainData.bounds.size.x, currentTarget.terrainData.bounds.size.z) / max_sub_grids;
        minArea = minArea * minArea / 8f;
        //
        tessellationJob = new TessellationJob(dataCreateJob.LODs, minArea);
        for (int i = 0; i < int.MaxValue; ++i)
        {
            tessellationJob.Update();
            EditorUtility.DisplayProgressBar("creating data", "tessellation", tessellationJob.progress);
            if (tessellationJob.IsDone)
                break;
        }
        string[] lodFolder = new string[LODSettings.Length];
        for (int i = 0; i < LODSettings.Length; ++i)
        {
            string scenePath = SceneManager.GetActiveScene().path;
            scenePath = scenePath.Remove(scenePath.LastIndexOf('/'));
            string folderName = string.Format("{0}_LOD{1}", currentTarget.name, i);
            lodFolder[i] = scenePath + '/' + folderName;
            if (!AssetDatabase.IsValidFolder(lodFolder[i]))
            {
                AssetDatabase.CreateFolder(scenePath, folderName);
                AssetDatabase.Refresh();
            }
        }
        //save meshes
        List<MeshPrefabBaker> bakers = new List<MeshPrefabBaker>();
        for (int i = 0; i < tessellationJob.mesh.Length; ++i)
        {
            EditorUtility.DisplayProgressBar("saving data", "processing", (float)i / tessellationJob.mesh.Length);
            MTMeshData data = tessellationJob.mesh[i];
            for (int lod = 0; lod < data.lods.Length; ++lod)
            {
                var folder = lodFolder[lod];
                if (!AssetDatabase.IsValidFolder(Path.Combine(folder, "Meshes")))
                {
                    AssetDatabase.CreateFolder(folder, "Meshes");
                    AssetDatabase.Refresh();
                }
                var mesh = SaveMesh(folder + "/Meshes", data.meshId, data.lods[lod], genUV2);
                var baker = new MeshPrefabBaker(lod, data.meshId, mesh, data.lods[lod].uvmin, data.lods[lod].uvmax);
                bakers.Add(baker);
            }
        }
        //bake mesh prefab
        GameObject[] prefabRoot = new GameObject[LODSettings.Length];
        // if (bakeMaterial)
        // {
        //     FullBakeMeshes(bakers, curentTarget, lodFolder, prefabRoot);
        // }
        // else
        {
            List<Material> mats = new List<Material>();
            var folder = lodFolder[0];
            List<string> matPath = new List<string>();
            MTMatUtils.SaveMixMaterials(folder, currentTarget.name, currentTarget, matPath);
            foreach (var p in matPath)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                mats.Add(m);
            }
            for (int i = 0; i < bakers.Count; ++i)
            {
                EditorUtility.DisplayProgressBar("saving data", "processing", (float)i / bakers.Count);
                var baker = bakers[i];
                //prefab
                if (prefabRoot[baker.lod] == null)
                {
                    prefabRoot[baker.lod] = new GameObject(currentTarget.name);
                }
                GameObject meshGo = new GameObject(baker.meshId.ToString());
                var filter = meshGo.AddComponent<MeshFilter>();
                filter.mesh = baker.mesh;
                var renderer = meshGo.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = mats.ToArray();
                meshGo.transform.parent = prefabRoot[baker.lod].transform;
                if (currentTarget.gameObject.layer == LayerMask.NameToLayer("NavmeshVisible"))
                {
                    MeshCollider collider = meshGo.AddComponent<MeshCollider>();
                    collider.sharedMesh = baker.mesh;
                    meshGo.gameObject.layer = LayerMask.NameToLayer("NavmeshVisible");
                }
            }
        }
        //
        for (int i = prefabRoot.Length - 1; i >= 0; --i)
        {
            var folder = lodFolder[i];
            PrefabUtility.SaveAsPrefabAsset(prefabRoot[i], folder + "/" + currentTarget.name + ".prefab");
            DestroyImmediate(prefabRoot[i]);
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        return lodFolder[0] + "/" + currentTarget.name + ".prefab";
    }

    public static Transform GenerateTree(Terrain curentTarget, float startProgress = 0f, float maxProgress = 1f)
    {
        if (curentTarget == null)
        {
            MTLog.LogError("no target terrain");
            return null;
        }
        List<GameObject> treePrefabs = new List<GameObject>();
        for (int i = 0; i < curentTarget.terrainData.treePrototypes.Length; i++)
        {
            TreePrototype treePrototype = curentTarget.terrainData.treePrototypes[i];
            treePrefabs.Add(treePrototype.prefab);
        }
        Transform allTrees = new GameObject("Trees").transform;
        List<Transform> treeNodes = new List<Transform>();
        for (int i = 0; i < treePrefabs.Count; i++)
        {
            Transform treeNode = new GameObject("Tree_" + (i + 1).ToString()).transform;
            treeNode.SetParent(allTrees);
            treeNodes.Add(treeNode);
        }
        TreeInstance[] treeInstances = curentTarget.terrainData.treeInstances;
        for (int i = 0; i < treeInstances.Length; i++)
        {
            TreeInstance treeInstance = treeInstances[i];
            if ((i >> 8) << 8 == i)
            {
                EditorUtility.DisplayProgressBar("导出植物", (i + 1).ToString(), startProgress + i * (maxProgress - startProgress) / treeInstances.Length);
            }
            Vector3 pos = GetTerrainTreePos(treeInstance, curentTarget);
            Vector3 scale = new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale);
            GameObject tree = PrefabUtility.InstantiatePrefab(treePrefabs[treeInstance.prototypeIndex], treeNodes[treeInstance.prototypeIndex]) as GameObject;
            tree.transform.position = pos;
            tree.transform.rotation = Quaternion.Euler(0, Mathf.Rad2Deg * treeInstance.rotation, 0);
            tree.transform.localScale = scale;
        }
        EditorUtility.ClearProgressBar();
        return allTrees;
    }

    static Vector3 GetTerrainTreePos(TreeInstance treeInstance, Terrain terrain)
    {
        Vector3 pos = treeInstance.position;
        pos.x *= terrain.terrainData.size.x;
        pos.y *= terrain.terrainData.size.y;
        pos.z *= terrain.terrainData.size.z;
        pos += terrain.transform.position;
        return pos;
    }

    /*
    void FullBakeMeshes(List<MeshPrefabBaker> bakers, Terrain curentTarget, string[] lodFolder, GameObject[] prefabRoot)
    {
        //reload Mats
        var arrAlbetoMats = new Material[2];
        var arrNormalMats = new Material[2];
        MTMatUtils.GetBakeMaterials(curentTarget, arrAlbetoMats, arrNormalMats);
        var texture = new Texture2D(bakeTextureSize, bakeTextureSize, TextureFormat.RGBA32, false);
        RenderTexture renderTexture = RenderTexture.GetTemporary(bakeTextureSize, bakeTextureSize);
        //
        for (int i = 0; i < bakers.Count; ++i)
        {
            EditorUtility.DisplayProgressBar("saving data", "processing", (float)i / bakers.Count);
            var baker = bakers[i];
            var folder = lodFolder[baker.lod];
            if (!AssetDatabase.IsValidFolder(Path.Combine(folder, "Textures")))
            {
                AssetDatabase.CreateFolder(folder, "Textures");
                AssetDatabase.Refresh();
            }
            //Debug.Log("mesh id : " + baker.meshId + " scale offset : " + baker.scaleOffset);
            var albeto = string.Format("{0}/Textures/albeto_{1}.png", folder, baker.meshId);
            SaveBakedTexture(albeto, renderTexture, texture, arrAlbetoMats, baker.scaleOffset);
            var normal = string.Format("{0}/Textures/normal_{1}.png", folder, baker.meshId);
            SaveBakedTexture(normal, renderTexture, texture, arrNormalMats, baker.scaleOffset);
            AssetDatabase.Refresh();
            var albetoTex = AssetDatabase.LoadAssetAtPath<Texture2D>(albeto);
            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
            if (!AssetDatabase.IsValidFolder(Path.Combine(folder, "Materials")))
            {
                AssetDatabase.CreateFolder(folder, "Materials");
                AssetDatabase.Refresh();
            }
            var matPath = string.Format("{0}/Materials/mat_{1}.mat", folder, baker.meshId);
            SaveBakedMaterial(matPath, albetoTex, normalTex, new Vector2(baker.scaleOffset.x, baker.scaleOffset.y));
            AssetDatabase.Refresh();
            //prefab
            if (prefabRoot[baker.lod] == null)
            {
                prefabRoot[baker.lod] = new GameObject(curentTarget.name);
            }
            GameObject meshGo = new GameObject(baker.meshId.ToString());
            var filter = meshGo.AddComponent<MeshFilter>();
            filter.mesh = baker.mesh;
            var renderer = meshGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            meshGo.transform.parent = prefabRoot[baker.lod].transform;
        }
        RenderTexture.ReleaseTemporary(renderTexture);
        foreach (var mat in arrAlbetoMats)
            DestroyImmediate(mat);
        foreach (var mat in arrNormalMats)
            DestroyImmediate(mat);
        DestroyImmediate(texture);
    }
    */
    static Mesh SaveMesh(string folder, int dataID, MTMeshData.LOD data, bool genUV2)
    {
        Mesh mesh = new Mesh();
        if (data.vertices.Length > System.UInt16.MaxValue)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        mesh.vertices = data.vertices;
        mesh.normals = data.normals;
        mesh.uv = data.uvs;
        if (genUV2)
        {
            mesh.uv2 = data.uvs;
        }
        mesh.triangles = data.faces;
        AssetDatabase.CreateAsset(mesh, string.Format("{0}/{1}.mesh", folder, dataID));
        return mesh;
    }
    /*
    void SaveBakedTexture(string path, RenderTexture renderTexture, Texture2D texture, Material[] arrMats, Vector4 scaleOffset)
    {
        //don't know why, need render twice to make the uv work correct
        for (int loop=0; loop<2; ++loop)
        {
            Graphics.Blit(null, renderTexture, arrMats[0]);
            arrMats[0].SetVector("_BakeScaleOffset", scaleOffset);
            if (arrMats[1] != null)
            {
                Graphics.Blit(null, renderTexture, arrMats[1]);
                arrMats[1].SetVector("_BakeScaleOffset", scaleOffset);
            }
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(Vector2.zero, new Vector2(texture.width, texture.height)), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
        }
        byte[] tga = texture.EncodeToTGA();
        File.WriteAllBytes(path, tga);
    }
    void SaveBakedMaterial(string path, Texture2D albeto, Texture2D normal, Vector2 size)
    {
        var scale = new Vector2(1f / size.x, 1f / size.y);
        Material tMat = new Material(Shader.Find("MT/TerrainVTLit"));
        tMat.SetTexture("_Diffuse", albeto);
        tMat.SetTextureScale("_Diffuse", scale);
        tMat.SetTexture("_Normal", normal);
        tMat.SetTextureScale("_Normal", scale);
        tMat.EnableKeyword("_NORMALMAP");
        AssetDatabase.CreateAsset(tMat, path);
    }
    */
}
