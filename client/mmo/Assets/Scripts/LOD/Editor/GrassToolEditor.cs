using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector.Editor;

[CustomEditor(typeof(GrassTool), true)]
public class GrassToolEditor : OdinEditor
{
    GrassTool grassTool;
    bool debug = false;
    int debugTreeBlockIndex = 0;
    int debugBlockDataIndex = 0;
    bool drawTreeObb = false;

    int combineBlockCount = 14;
    float maxBlockRadius = 15f;
    int searchInfoMinSize = 8;
    Terrain terrain;
    string terrainPrefix = "hl_TerrainMesh_";
    List<bool> needSaveTreeInGrassTool = new List<bool>();

    MTLODSetting mtLODSetting = new MTLODSetting();
    int terrainSliceCount = 4;

    protected override void OnEnable()
    {
        base.OnEnable();
        grassTool = target as GrassTool;
        GetTempData();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        SetTempData();
    }

    private void SetTempData()
    {
        if (target == null)
        {
            return;
        }
        EditorPrefs.SetInt(target.name + "GrassToolInstanceID", target.GetInstanceID());
        EditorPrefs.SetInt(target.name + "GrassToolCombineBlockCount", combineBlockCount);
        EditorPrefs.SetFloat(target.name + "GrassToolMaxBlockRadius", maxBlockRadius);
        EditorPrefs.SetFloat(target.name + "GrassToolTerrainSliceCount", terrainSliceCount);
        if (terrain != null)
        {
            string path = terrain.name;
            Transform parent = terrain.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            EditorPrefs.SetString(target.name + "GrassToolTerrainPath", path);
            for (int i = 0; i < needSaveTreeInGrassTool.Count; i++)
            {
                EditorPrefs.SetBool(target.name + "GrassToolNeedSaveTreeInGrassTool_" + i, needSaveTreeInGrassTool[i]);
            }
        }
        EditorPrefs.SetInt(target.name + "GrassToolSubdivision", mtLODSetting.Subdivision);
        EditorPrefs.SetFloat(target.name + "GrassToolSlopeAngleError", mtLODSetting.SlopeAngleError);
}

    private void GetTempData()
    {
        if (EditorPrefs.GetInt(target.name + "GrassToolInstanceID") != target.GetInstanceID())
        {
            return;
        }
        combineBlockCount = EditorPrefs.GetInt(target.name + "GrassToolCombineBlockCount", combineBlockCount);
        maxBlockRadius = EditorPrefs.GetFloat(target.name + "GrassToolMaxBlockRadius", maxBlockRadius);
        terrainSliceCount = EditorPrefs.GetInt(target.name + "GrassToolTerrainSliceCount", terrainSliceCount);
        string terrainPath = EditorPrefs.GetString(target.name + "GrassToolTerrainPath");
        if (!string.IsNullOrWhiteSpace(terrainPath))
        {
            GameObject gameObject = GameObject.Find(terrainPath);
            if (gameObject != null)
            {
                terrain = gameObject.GetComponent<Terrain>();
            }
        }
        if (terrain != null)
        {
            int treeCount = terrain.terrainData.treePrototypes.Length;
            for (int i = 0; i < treeCount; i++)
            {
                if(i >= needSaveTreeInGrassTool.Count)
                {
                    needSaveTreeInGrassTool.Add(false);
                }
                needSaveTreeInGrassTool[i] = EditorPrefs.GetBool(target.name + "GrassToolNeedSaveTreeInGrassTool_" + i, needSaveTreeInGrassTool[i]);
            }
        }
        mtLODSetting.Subdivision = EditorPrefs.GetInt(target.name + "GrassToolSubdivision", mtLODSetting.Subdivision);
        mtLODSetting.SlopeAngleError = EditorPrefs.GetFloat(target.name + "GrassToolSlopeAngleError", mtLODSetting.SlopeAngleError);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        debug = EditorGUILayout.Toggle("Draw Gizmos", debug);
        if (debug && grassTool.treeBlocks != null)
        {
            debugTreeBlockIndex = EditorGUILayout.IntSlider("Debug TreeBlock Index", debugTreeBlockIndex, 0, Mathf.Max(0, grassTool.treeBlocks.Length - 1));
            drawTreeObb = EditorGUILayout.Toggle("Draw Tree Obb", drawTreeObb);
            if (drawTreeObb && grassTool.treeBlocks[debugTreeBlockIndex].blockDatas != null)
            {
                debugBlockDataIndex = EditorGUILayout.IntSlider("Debug BlockData Index", debugBlockDataIndex, 0, Mathf.Max(0, grassTool.treeBlocks[debugTreeBlockIndex].blockDatas.Length - 1));
            }
        }
        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        if (grassTool as HomelandGrassTool)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("导出地形");
            EditorGUI.indentLevel++;
            terrain = EditorGUILayout.ObjectField("terrain", terrain, typeof(Terrain), true) as Terrain;
            if(terrain != null)
            {
                EditorGUILayout.LabelField(" ", "✓导出后可被建筑占地剔除(草地)");
                EditorGUILayout.LabelField(" ", "✘仅导出为子物体(树)");
                int treeCount = terrain.terrainData.treePrototypes.Length;
                for (int i = 0; i < treeCount; i++)
                {
                    if (i >= needSaveTreeInGrassTool.Count)
                    {
                        needSaveTreeInGrassTool.Add(false);
                    }
                    Rect rect = EditorGUILayout.GetControlRect();
                    EditorGUI.LabelField(rect, " ", "︳");
                    rect.x += rect.height * 0.5f;
                    rect.width -= rect.height * 0.5f;
                    needSaveTreeInGrassTool[i] = EditorGUI.Toggle(rect, " ", needSaveTreeInGrassTool[i]);
                    rect.x += rect.height * 1.2f;
                    rect.width -= rect.height * 1.2f;
                    if (terrain.terrainData.treePrototypes[i].prefab)
                    {
                        EditorGUI.LabelField(rect, " ", terrain.terrainData.treePrototypes[i].prefab.name);
                    }
                    else
                    {
                        EditorGUI.LabelField(rect, " ", null);
                    }
                }
                GUILayout.Space(8f);
            }
            combineBlockCount = EditorGUILayout.IntSlider("Combine Block Count", combineBlockCount, 1, 32);
            maxBlockRadius = EditorGUILayout.Slider("Max Block Radius", maxBlockRadius, 1.5f, 50f);
            searchInfoMinSize = EditorGUILayout.IntSlider("SearchInfo Min Size", searchInfoMinSize, 4, 128);
            EditorGUI.BeginDisabledGroup(terrain == null);
            if (GUILayout.Button("处理terrain植被"))
            {
                ConvertTerrainToGrass();
            }
            mtLODSetting.OnGUIDraw(0);
            terrainSliceCount = EditorGUILayout.IntField("Slice Count(NxN)", terrainSliceCount);
            if (GUILayout.Button("转换trrrain为mesh"))
            {
                ConvertTerrainToMesh();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }
        else if(GUILayout.Button("转换子物体为草地"))
        {
            ConvertChildToGrass();
        }
        if (debug)
        {
            if (GUILayout.Button("test"))
            {
                for (int i = 0; i < grassTool.treeBlocks.Length; i++)
                {
                    var a = grassTool.treeBlocks[i].searchInfo;
                    int depth = 0, count = 0;
                    SearchInfoBlockCountCheck(a, ref count);
                    SearchInfoDepthCheck(a, 0, ref depth);
                    Debug.Log((i + 1) + ". " + depth + " " + count);
                }
            }
        }    }

    void ConvertTerrainToGrass()
    {
        DestroyGrassChildImmediate(grassTool.transform);
        List<Transform> childs = new List<Transform>();
        for(int i = 0; i < grassTool.transform.childCount; i++)
        {
            Transform child = grassTool.transform.GetChild(i);
            childs.Add(child);
            // child.SetParent(null);
        }
        for (int i = 0; i < childs.Count; i++)
        {
            Transform child = childs[i];
            child.SetParent(null);
        }
        Transform allTree = TerrainMeshEditor.GenerateTree(terrain, 0f, 0.6f);
        if (allTree != null)
        {
            for (int i = 0; i < needSaveTreeInGrassTool.Count; i++)
            {
                if (needSaveTreeInGrassTool[i])
                {
                    Transform needSaveTree = allTree.Find("Tree_" + (i + 1).ToString());
                    needSaveTree.SetParent(grassTool.transform);
                }
            }
            ConvertChildToGrass(0.6f, 0.9f);
            EditorUtility.DisplayProgressBar("处理普通植物", "", 0.95f);
            DestroyGrassChildImmediate(grassTool.transform);
            for (int i = 0; i < needSaveTreeInGrassTool.Count; i++)
            {
                if (!needSaveTreeInGrassTool[i])
                {
                    Transform needSaveTree = allTree.Find("Tree_" + (i + 1).ToString());
                    needSaveTree.SetParent(grassTool.transform);
                    needSaveTree.name = "Tree_" + terrain.terrainData.treePrototypes[i].prefab.name;
                }
            }
            DestroyGrassChildImmediate(allTree, false);
        }
        for (int i = 0; i < childs.Count; i++)
        {
            Transform child = childs[i];
            child.SetParent(grassTool.transform);
        }
        EditorUtility.ClearProgressBar();
        // 触发保存
        grassTool.enabled = false;
        grassTool.enabled = true;
    }

    void DestroyGrassChildImmediate(Transform parent, bool ignoreParent = true)
    {
        Transform[] allChilds = parent.GetComponentsInChildren<Transform>();
        for(int i = 0; i < allChilds.Length; i++)
        {
            Transform child = allChilds[i];
            if (child != null && (!ignoreParent || child != parent))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(child))
                {
                    GameObject prefabInstance = PrefabUtility.GetNearestPrefabInstanceRoot(child);
                    if (prefabInstance.name.StartsWith(terrainPrefix) == false)
                    {
                        DestroyImmediate(prefabInstance);
                    }
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
    }

    void ConvertChildToGrass(float startProgress = 0f, float maxProgress = 1f)
    {
        MeshRenderer[] mrs = grassTool.GetComponentsInChildren<MeshRenderer>(true);
        Dictionary<string, List<Transform>> gosDic = new Dictionary<string, List<Transform>>();
        for (int i = 0; i < mrs.Length; i++)
        {
            MeshRenderer mr = mrs[i];
            if (PrefabUtility.IsPartOfPrefabInstance(mr))
            {
                Transform go = PrefabUtility.GetOutermostPrefabInstanceRoot(mr).transform;
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                List<Transform> gos;
                if (!gosDic.TryGetValue(path, out gos))
                {
                    gosDic.Add(path, gos);
                }
                if (gos == null)
                {
                    gos = new List<Transform>();
                }
                if (!gos.Contains(go))
                {
                    gos.Add(go);
                    gosDic[path] = gos;
                }
            }
            if ((i >> 8) << 8 == i)
            {
                EditorUtility.DisplayProgressBar("转换植物", (i + 1).ToString(), startProgress + i * (maxProgress - startProgress) / mrs.Length);
            }
        }
        EditorUtility.ClearProgressBar();
        List<TreeInfo> treeInfos = new List<TreeInfo>();
        foreach (string path in gosDic.Keys)
        {
            List<Transform> gos = gosDic[path];
            TreeInfo treeInfo = new TreeInfo();
            MeshRenderer[] meshRenderers = gos[0].GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer mr in meshRenderers)
            {
                if (mr.name.Contains("LOD1"))
                {
                    treeInfo.mrLOD = mr;
                }
                else
                {
                    treeInfo.mr = mr;
                }
            }
            treeInfos.Add(treeInfo);
            treeInfo.instances = gos;
        }
        if (grassTool as BuildingGrassTool)
        {
            combineBlockCount = 1;
            maxBlockRadius = 50f;
        }
        GetGrassPosSetData(grassTool, treeInfos, combineBlockCount, maxBlockRadius);
        if (grassTool as HomelandGrassTool)
        {
            grassTool.useSearchInfo = true;
        }
        else if (grassTool as BuildingGrassTool)
        {
            for (int i = 0; i < grassTool.treeBlocks.Length; i++)
            {
                GrassTool.TreeData treeData = grassTool.treeBlocks[i];
                for (int j = 0; j < treeData.blockDatas.Length; j++)
                {
                    GrassTool.BlockData blockData = treeData.blockDatas[j];
                    Vector3 localPos = grassTool.transform.worldToLocalMatrix.MultiplyPoint3x4(blockData.packedSphere);
                    blockData.packedSphere = new Vector4(localPos.x, localPos.y, localPos.z, blockData.packedSphere.w);
                    for (int k = 0; k < blockData.treeTransform.Length; k++)
                    {
                        blockData.treeTransform[k] = grassTool.transform.worldToLocalMatrix * blockData.treeTransform[k];
                    }
                    blockData.obbLocalBounds = new OBB.OBBLocal[blockData.obbBounds.Length];
                    for (int k = 0; k < blockData.obbBounds.Length; k++)
                    {
                        blockData.obbLocalBounds[k] = OBB.TransformToLocal(grassTool.transform.worldToLocalMatrix, blockData.obbBounds[k]);
                    }
                }
            }
        }
    }

    void ConvertTerrainToMesh()
    {
        MightyTerrainMesh.CreateMeshJob dataCreateJob = null;
        MightyTerrainMesh.TessellationJob tessellationJob = null;
        int sliceCount = terrainSliceCount;// EditorGUILayout.IntField("Slice Count(NxN)", curSliceCount);
        sliceCount = Mathf.NextPowerOfTwo(sliceCount);
        int QuadTreeDepth = Mathf.FloorToInt(Mathf.Log(sliceCount, 2));
        string meshPrefab = TerrainMeshEditor.GenerateMesh(terrain, new MTLODSetting[] { mtLODSetting }, dataCreateJob, tessellationJob, QuadTreeDepth, false);
        if (!string.IsNullOrWhiteSpace(meshPrefab))
        {
            GameObject terrainMesh = AssetDatabase.LoadAssetAtPath<GameObject>(meshPrefab);
            if (terrainMesh)
            {
                List<Transform> childs = new List<Transform>();
                for (int i = 0; i < grassTool.transform.childCount; i++)
                {
                    Transform child = grassTool.transform.GetChild(i);
                    childs.Add(child);
                }
                for (int i = 0; i < childs.Count; i++)
                {
                    Transform child = childs[i];
                    if (child != null && child.name.StartsWith(terrainPrefix))
                    // if (child != null && child.name.StartsWith(terrainPrefix + terrainMesh.name))
                    {
                        DestroyImmediate(child.gameObject);
                    }
                }
                GameObject terrainMeshInstantiate = PrefabUtility.InstantiatePrefab(terrainMesh, grassTool.transform) as GameObject;
                terrainMeshInstantiate.name = terrainPrefix + terrainMeshInstantiate.name;
                terrainMeshInstantiate.transform.position = Vector3.zero;
                terrainMeshInstantiate.transform.rotation = Quaternion.identity;
            }
        }
    }

    private void OnSceneGUI()
    {
        var cacheColor = Handles.color;
        if (debug && grassTool.treeBlocks != null && grassTool.treeBlocks.Length > debugTreeBlockIndex)
        {
            GrassTool.TreeData treeData = grassTool.treeBlocks[debugTreeBlockIndex];
            if(treeData != null && treeData.blockDatas != null && treeData.searchInfos != null && treeData.searchInfos.Length > 0)
            {
                for (int i = 0; i < treeData.blockDatas.Length; i++)
                {
                    GrassTool.BlockData blockData = treeData.blockDatas[i];
                    if (blockData != null)
                    {
                        Handles.color = Color.white;
                        if (grassTool as BuildingGrassTool)
                        {
                            Vector4 packedSphere = blockData.packedSphere;
                            Vector3 pos = grassTool.transform.localToWorldMatrix.MultiplyPoint3x4(packedSphere);
                            packedSphere.x = pos.x;
                            packedSphere.y = pos.y;
                            packedSphere.z = pos.z;
                            Handles.DrawWireDisc(packedSphere, Vector3.up, packedSphere.w);
                        }
                        else if(grassTool as HomelandGrassTool)
                        {
                            OBB packedObbBound = blockData.packedObbBound;
                            Handles.matrix = Matrix4x4.TRS(packedObbBound.Center, packedObbBound.Rotation, Vector3.one);
                            Handles.DrawWireCube(Vector3.zero, packedObbBound.Size);
                        }
                    }
                }
                if (drawTreeObb && treeData.blockDatas[debugBlockDataIndex].obbBounds != null)
                {
                    GrassTool.BlockData blockData = treeData.blockDatas[debugBlockDataIndex];
                    for (int i = 0; i < blockData.obbBounds.Length; i++)
                    {
                        OBB obb = blockData.obbBounds[i];
                        if (grassTool as BuildingGrassTool)
                        {
                            obb = OBB.TransformToWorld(grassTool.transform.localToWorldMatrix, blockData.obbLocalBounds[i]);
                        }

                        var cacheMatrix = Handles.matrix;
                        Handles.matrix = Matrix4x4.TRS(obb.Center, obb.Rotation, Vector3.one);
                        Handles.color = Color.white;
                        Handles.DrawWireCube(Vector3.zero, obb.Size);
                        Handles.matrix = cacheMatrix;
                    }
                }
                DrawSearchInfo(treeData.searchInfos, treeData.searchInfos[0]);
            }
        }
        Handles.color = cacheColor;
    }

    void DrawSearchInfo(GrassTool.SearchInfo[] searchInfos, GrassTool.SearchInfo searchInfo)
    {
        for (int i = 0; i < searchInfo.nextSearchInfoIndexs.Count; i++)
        {
            DrawSearchInfo(searchInfos, searchInfos[searchInfo.nextSearchInfoIndexs[i]]);
        }
        Handles.matrix = Matrix4x4.TRS(searchInfo.obbBound.Center, searchInfo.obbBound.Rotation, Vector3.one);
        Handles.color = Color.green;
        Handles.DrawWireCube(Vector3.zero, searchInfo.obbBound.Size);
    }

    public class TreeInfo
    {
        public MeshRenderer mr;
        public MeshRenderer mrLOD;
        public List<Transform> instances;
    }

    Vector4 blockPosition = new Vector4(-5000, 0, -5000, 10000);

    public void GetGrassPosSetData(GrassTool grassTool, List<TreeInfo> treeInfos, int combineBlockCount, float maxBlockRadius)
    {
        // 按树的种类进行分类
        int treeTypeCount = treeInfos.Count;
        List<Transform>[] treeTypes = new List<Transform>[treeTypeCount];
        for (int i = 0; i < treeTypeCount; i++)
        {
            treeTypes[i] = treeInfos[i].instances;
        }

        // 划分方块
        List<GrassTool.TreeData> TreeBlocks = new List<GrassTool.TreeData>();
        for (int i = 0; i < treeTypes.Length; i++)
        {
            List<Transform> treeType = treeTypes[i];
            GrassTool.TreeData treeBlock = new GrassTool.TreeData();
            MeshRenderer meshRenderer = treeInfos[i].mr;
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
            treeBlock.treeMesh = meshFilter.sharedMesh;
            treeBlock.treeMaterial = meshRenderer.sharedMaterial;
            // treeBlock.treeMaterial.enableInstancing = true;
            if ((grassTool as HomelandGrassTool) != null && treeInfos[i].mrLOD != null)
            {
                MeshRenderer lodMeshRenderer = treeInfos[i].mrLOD;
                MeshFilter lodMeshFilter = lodMeshRenderer.GetComponent<MeshFilter>();
                treeBlock.lodTreeMesh = lodMeshFilter.sharedMesh;
                treeBlock.lodTreeMaterial = lodMeshRenderer.sharedMaterial;
            }
            List<GrassTool.BlockData> blockDatas = new List<GrassTool.BlockData>();
            GrassTool.SearchInfo searchInfo = new GrassTool.SearchInfo();
            SplitTreeInstances(meshFilter.sharedMesh.bounds, ref blockDatas, treeType, blockPosition, combineBlockCount, maxBlockRadius, searchInfo);
            CutSearchInfo(searchInfo, blockDatas);
            int depth = 0;
            SearchInfoDepthCheck(searchInfo, 0, ref depth);
            if (depth > 6)
            {
                Debug.LogError("Over Depth: " + depth);
            }
            List<GrassTool.SearchInfo> searchInfos = new List<GrassTool.SearchInfo>();
            searchInfos.Add(searchInfo);
            SearchInfoToArray(searchInfo, searchInfos);
            if (blockDatas.Count != 0)
            {
                treeBlock.blockDatas = blockDatas.ToArray();
                treeBlock.searchInfo = searchInfo;
                treeBlock.searchInfos = searchInfos.ToArray();
                TreeBlocks.Add(treeBlock);
            }
        }
        SetTreeBlocksData(grassTool, TreeBlocks.ToArray());
    }

    public void SplitTreeInstances(Bounds treeBounds, ref List<GrassTool.BlockData> blockDatas, List<Transform> treeType, Vector4 blockPosition, int combineBlockCount, float maxBlockRadius, GrassTool.SearchInfo searchInfo)
    {
        // 选出在区域内的草
        List<Transform> inBlockTrees = new List<Transform>();
        for (int i = 0; i < treeType.Count; i++)
        {
            Transform treeInstance = treeType[i];
            Vector3 pos = treeInstance.position;
            if (pos.x >= blockPosition.x && pos.x < blockPosition.x + blockPosition.w && pos.z >= blockPosition.z && pos.z < blockPosition.z + blockPosition.w)
            {
                inBlockTrees.Add(treeInstance);
            }
        }

        int maxGrassCount = 1023 / combineBlockCount; // 1023 - max size for gpu instance
        float currentRad = blockPosition.w * Mathf.Sqrt(0.5f);// (Mathf.Sqrt(2) * 0.5f);
        bool overSize = currentRad > maxBlockRadius;
        bool overCount = inBlockTrees.Count > maxGrassCount;

        // searchInfo.sphere = new Vector4(blockPosition.x, blockPosition.y, blockPosition.z, currentRad);

        if (inBlockTrees.Count == 0)
        {
            return;
        }
        else if (overSize || overCount) // 决定是否生成新分块
        {
            GrassTool.SearchInfo searchInfo1 = new GrassTool.SearchInfo();
            GrassTool.SearchInfo searchInfo2 = new GrassTool.SearchInfo();
            GrassTool.SearchInfo searchInfo3 = new GrassTool.SearchInfo();
            GrassTool.SearchInfo searchInfo4 = new GrassTool.SearchInfo();
            searchInfo.nextSearchInfos.Add(searchInfo1);
            searchInfo.nextSearchInfos.Add(searchInfo2);
            searchInfo.nextSearchInfos.Add(searchInfo3);
            searchInfo.nextSearchInfos.Add(searchInfo4);
            float halfSize = blockPosition.w * 0.5f;
            Vector4 smallBlockPosition1 = new Vector4(blockPosition.x, blockPosition.y, blockPosition.z, halfSize);
            Vector4 smallBlockPosition2 = new Vector4(blockPosition.x + halfSize, blockPosition.y, blockPosition.z, halfSize);
            Vector4 smallBlockPosition3 = new Vector4(blockPosition.x, blockPosition.y, blockPosition.z + halfSize, halfSize);
            Vector4 smallBlockPosition4 = new Vector4(blockPosition.x + halfSize, blockPosition.y, blockPosition.z + halfSize, halfSize);
            SplitTreeInstances(treeBounds, ref blockDatas, inBlockTrees, smallBlockPosition1, combineBlockCount, maxBlockRadius, searchInfo1);
            SplitTreeInstances(treeBounds, ref blockDatas, inBlockTrees, smallBlockPosition2, combineBlockCount, maxBlockRadius, searchInfo2);
            SplitTreeInstances(treeBounds, ref blockDatas, inBlockTrees, smallBlockPosition3, combineBlockCount, maxBlockRadius, searchInfo3);
            SplitTreeInstances(treeBounds, ref blockDatas, inBlockTrees, smallBlockPosition4, combineBlockCount, maxBlockRadius, searchInfo4);
        }
        else
        {
            GrassTool.BlockData blockData = new GrassTool.BlockData();
            Bounds totalBounds = new Bounds();
            List<Matrix4x4> matrix4X4s = new List<Matrix4x4>();
            List<OBB> obbBounds = new List<OBB>();
            for (int i = 0; i < inBlockTrees.Count; i++)
            {
                Transform treeInstance = inBlockTrees[i];
                Vector3 pos = treeInstance.position;
                Vector3 scale = treeInstance.localScale;
                matrix4X4s.Add(treeInstance.localToWorldMatrix);
                obbBounds.Add(new OBB(new Vector2(treeInstance.position.x, treeInstance.position.z), new Vector2(treeBounds.size.x, treeBounds.size.z) * scale.x, treeInstance.rotation.eulerAngles.y));
                float width = (new Vector2(treeBounds.size.x, treeBounds.size.z) * scale.x).magnitude;
                float height = treeBounds.size.y * scale.y;
                Vector3 size = new Vector3(width, height, width);
                Bounds newBounds = new Bounds(pos + treeBounds.center, size);
                if (i == 0)
                {
                    totalBounds = newBounds;
                }
                totalBounds.Encapsulate(newBounds);
            }
            blockData.totalBounds_Editor = totalBounds;
            blockData.packedObbBound = new OBB(new Vector2(totalBounds.center.x, totalBounds.center.z), new Vector2(totalBounds.size.x, totalBounds.size.z), 0);
            // blockData.packedSphere = new Vector4(blockPosition.x + halfSize, blockPosition.y, blockPosition.z + halfSize, currentRad);
            blockData.packedSphere = new Vector4(totalBounds.center.x, totalBounds.center.y, totalBounds.center.z, totalBounds.extents.magnitude);
            blockData.treeTransform = matrix4X4s.ToArray();
            blockData.drawSize = matrix4X4s.Count;
            blockData.obbBounds = obbBounds.ToArray();
            if (totalBounds.Intersects(new Bounds(new Vector3(5, 0, -25), new Vector3(250, 200, 330))))
            {
                searchInfo.blockIndexs.Add(blockDatas.Count);
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

    void CutSearchInfo(GrassTool.SearchInfo searchInfo, List<GrassTool.BlockData> blockDatas)
    {
        CombineBlockSearchInfo(searchInfo);
        CombineOneSearchInfo(searchInfo);
        CalSearchInfoSize(searchInfo, blockDatas);
    }

    bool CombineBlockSearchInfo(GrassTool.SearchInfo searchInfo)
    {
        bool haveChange = false;
        for (int i = searchInfo.nextSearchInfos.Count - 1; i >= 0; i--)
        {
            GrassTool.SearchInfo nextSearchInfo = searchInfo.nextSearchInfos[i];
            if (nextSearchInfo.nextSearchInfos.Count == 0)
            {
                if (nextSearchInfo.blockIndexs.Count == 0)
                {
                    searchInfo.nextSearchInfos.Remove(nextSearchInfo);
                    haveChange = true;
                }
                else if (searchInfo.blockIndexs.Count + searchInfo.nextSearchInfos.Count < searchInfoMinSize && nextSearchInfo.blockIndexs.Count < searchInfoMinSize)
                {
                    searchInfo.blockIndexs.AddRange(nextSearchInfo.blockIndexs);
                    searchInfo.nextSearchInfos.Remove(nextSearchInfo);
                    haveChange = true;
                }
            }
            else
            {
                while (CombineBlockSearchInfo(nextSearchInfo)) { }
            }
        }
        return haveChange;
    }

    void CombineOneSearchInfo(GrassTool.SearchInfo searchInfo)
    {
        if (searchInfo.nextSearchInfos.Count == 1 && searchInfo.blockIndexs.Count == 0)
        {
            // searchInfo.sphere = searchInfo.nextSearchInfos[0].sphere;
            searchInfo.blockIndexs = searchInfo.nextSearchInfos[0].blockIndexs;
            searchInfo.nextSearchInfos = searchInfo.nextSearchInfos[0].nextSearchInfos;
            CombineOneSearchInfo(searchInfo);
        }
        else
        {
            for (int i = 0; i < searchInfo.nextSearchInfos.Count; i++)
            {
                GrassTool.SearchInfo nextSearchInfo = searchInfo.nextSearchInfos[i];
                CombineOneSearchInfo(nextSearchInfo);
            }
        }
    }

    void CalSearchInfoSize(GrassTool.SearchInfo searchInfo, List<GrassTool.BlockData> blockDatas)
    {
        for (int i = 0; i < searchInfo.nextSearchInfos.Count; i++)
        {
            CalSearchInfoSize(searchInfo.nextSearchInfos[i], blockDatas);
        }
        Bounds totalBounds = new Bounds();
        if (searchInfo.blockIndexs.Count != 0)
        {
            totalBounds = blockDatas[searchInfo.blockIndexs[0]].totalBounds_Editor;
        }
        else if (searchInfo.nextSearchInfos.Count != 0)
        {
            totalBounds = searchInfo.nextSearchInfos[0].totalBounds_Editor;
        }
        for (int i = 0; i < searchInfo.nextSearchInfos.Count; i++)
        {
            totalBounds.Encapsulate(searchInfo.nextSearchInfos[i].totalBounds_Editor);
        }
        for (int i = 0; i < searchInfo.blockIndexs.Count; i++)
        {
            totalBounds.Encapsulate(blockDatas[searchInfo.blockIndexs[i]].totalBounds_Editor);
        }
        searchInfo.totalBounds_Editor = totalBounds;
        searchInfo.obbBound = new OBB(new Vector2(totalBounds.center.x, totalBounds.center.z), new Vector2(totalBounds.size.x, totalBounds.size.z), 0);// new Vector4(totalBounds.center.x, totalBounds.center.y, totalBounds.center.z, totalBounds.extents.magnitude);
    }

    void SearchInfoBlockCountCheck(GrassTool.SearchInfo searchInfo, ref int count)
    {
        count += searchInfo.blockIndexs.Count;
        for (int i = 0; i < searchInfo.nextSearchInfos.Count; i++)
        {
            SearchInfoBlockCountCheck(searchInfo.nextSearchInfos[i], ref count);
        }
    }

    void SearchInfoDepthCheck(GrassTool.SearchInfo searchInfo, int temp, ref int depth)
    {
        temp++;
        depth = Mathf.Max(depth, temp);
        for (int i = 0; i < searchInfo.nextSearchInfos.Count; i++)
        {
            SearchInfoDepthCheck(searchInfo.nextSearchInfos[i], temp, ref depth);
        }
    }

    void SearchInfoToArray(GrassTool.SearchInfo searchInfo, List<GrassTool.SearchInfo> searchInfos)
    {
        // List<GrassTool.SearchInfo> searchInfos = new List<GrassTool.SearchInfo>();
        // searchInfos.Add(searchInfo);
        for(int i = 0; i < searchInfo.nextSearchInfos.Count; i++)
        {
            searchInfo.nextSearchInfoIndexs.Add(searchInfos.Count);
            searchInfos.Add(searchInfo.nextSearchInfos[i]);
            SearchInfoToArray(searchInfo.nextSearchInfos[i], searchInfos);
        }
    }
}
