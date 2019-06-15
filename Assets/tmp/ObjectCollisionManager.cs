
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class ObjectCollisionManager : Singleton<ObjectCollisionManager>
{
    public static bool DISABLE_GRASS_COLLISION = false;

    public Vector4 nullCollisionPage = -Vector4.one;

    public static List<GrassHashCell> info_cellsSelected = new List<GrassHashCell>();
    public static int info_groupsDispatchedCount;

    public Atlas m_atlasCollisionAtlas;
    private int m_atlasCollisionAtlasPageSize = 64;

    private static ComputeShader compute;
    private static int computeCollisionKernel;
    private static int initializeAtlasPagekernel;
    private static int vectorRecoverStateKernel;
    
    private static Vector2 aux_atlasPixeloffsetMin = new Vector2Int();
    private static Vector2 aux_atlasPixeloffsetMax = new Vector2Int();

    private static Vector3 aux_objBoundMin = new Vector3();
    private static Vector3 aux_objBoundMax = new Vector3();
    private static Vector2 aux_uvMin = new Vector2();
    private static Vector2 aux_uvMax = new Vector2();

    private static ComputeBuffer hadCollisionOnPage;
    private static int[] hadCollisionOnPageOutput;

    private static int dispatchGroupSize;


    ComputeBuffer allPagesToRecover;

    List<KeyValuePair<Vector2Int, List<TerrainColliderInteraction.CollisionInfo>>> allCollidersInfos = new List<KeyValuePair<Vector2Int, List<TerrainColliderInteraction.CollisionInfo>>>();

    int collisionInfoStructureSize;
    
    /// <summary>
    /// Used to avoid the use of collision page, more specifically by the recover form method. 
    /// It is necessary for cells, without colliders inside, release his page. These releases occur after X times after the last request. 
    /// </summary>
    public static float maxTimeToGrassRecoverForm = 5f;


    private void Awake()
    {
        m_atlasCollisionAtlas = new Atlas(RenderTextureFormat.ARGBHalf, FilterMode.Point, 4096, m_atlasCollisionAtlasPageSize, false);

        compute = (ComputeShader)Resources.Load("ObjectCollisionDetect");
        computeCollisionKernel = compute.FindKernel("ComputeObjectCollision");
        initializeAtlasPagekernel = compute.FindKernel("InitializeCollisionPage");
        vectorRecoverStateKernel = compute.FindKernel("RecoverCollisionPageToInitialState");

        if (computeCollisionKernel < 0 || initializeAtlasPagekernel < 0 || vectorRecoverStateKernel < 0)
        {
            Debug.LogError("Missing kernel.");
            return;
        }

        hadCollisionOnPage = new ComputeBuffer(1, sizeof(int));
        hadCollisionOnPageOutput = new int[1];

        dispatchGroupSize = Mathf.CeilToInt(m_atlasCollisionAtlasPageSize / 8f);

        GameObject.Find("CollisionAtlas").GetComponent<RawImage>().texture = m_atlasCollisionAtlas.texture;
            
        UpdateInfosOnShaders.Instance.AddNewCallback = UpdateStaticData;

        collisionInfoStructureSize = Marshal.SizeOf(typeof(TerrainColliderInteraction.CollisionInfo));
    }
    

    /// <summary>
    /// Set all data on page with (x,y,z,w) = (0,1,0,0).
    /// Called for cells that collide the first time with some object. 
    /// </summary>
    public Atlas.AtlasPageDescriptor GetEmptyAtlasPage()
    {
        Atlas.AtlasPageDescriptor page = m_atlasCollisionAtlas?.GetPage();

        if (page == null) return null;
        
        compute.SetVector("_collisionmapDesc", page.tl_size);
        compute.SetTexture(initializeAtlasPagekernel, "_collisionmapAtlas", page.atlas.texture);

        compute.Dispatch(initializeAtlasPagekernel, dispatchGroupSize, dispatchGroupSize, 1);

        return page;
    }

    
    /// <summary>
    /// Restore the vector for his initial direction. Only vector that has an angle (in relation vector up) smaller than 'K' .
    /// Used to animate the grassRecover.
    /// </summary>
    private void RecoverVectorForInitialStateOnPage(List<GrassHashCell> cellsToRecover)
    {
        if (cellsToRecover == null || cellsToRecover.Count == 0) return;

        List<Vector4> allPages = cellsToRecover.Select(c => c.collisionPage.tl_size).ToList();

        if(allPages == null || allPages.Count == 0) return;

        if (allPagesToRecover == null || allPagesToRecover.count < allPages.Count)
            ResizeComputeBuffer(ref allPagesToRecover, allPages.Count, sizeof(float) * 4);

        allPagesToRecover.SetData(allPages);
        
        compute.SetBuffer(vectorRecoverStateKernel, "_collisionPages", allPagesToRecover);

        compute.SetFloat("_deltaTime", Time.deltaTime);

        compute.Dispatch(vectorRecoverStateKernel, dispatchGroupSize, dispatchGroupSize, allPages.Count);
    }


    /// <summary>
    /// Call the recover vector form for all cells that collided with something and is probably rendering or colliding with something. (see the 'lastCollisionTime' update).  
    /// </summary>
    private void RecoverVectorToInitialState()
    {
        if (GrassHashManager.Instance.allCreatedCellsWithCollision == null || GrassHashManager.Instance.allCreatedCellsWithCollision.Count == 0) return;

        Profiler.BeginSample("Grass - Recover collision form");
        
        RecoverVectorForInitialStateOnPage(GrassHashManager.Instance.allCreatedCellsWithCollision.FindAll(c => c.hasCollisionPage && c.HasValidCollisionInfoOnPage && (Time.time - c.lastFrameCollisionDetected) < maxTimeToGrassRecoverForm));

        Profiler.EndSample();
    }

  
    private void DispatchAllCollisions()
    {
        if (allCollidersInfos == null || allCollidersInfos.Count == 0) return;

        compute.SetTexture(computeCollisionKernel, "_collisionmapAtlas", m_atlasCollisionAtlas.texture);
        
        for (int i = 0; i < allCollidersInfos.Count; i++)
        {
            if (allCollidersInfos[i].Value.Count == 0) continue;

            ComputeBuffer infosBuffer = new ComputeBuffer(allCollidersInfos[i].Value.Count, collisionInfoStructureSize);
            infosBuffer.SetData(allCollidersInfos[i].Value);

            compute.SetBuffer(computeCollisionKernel, "_collisionPages1", infosBuffer);
            
            compute.Dispatch(computeCollisionKernel, allCollidersInfos[i].Key.x, allCollidersInfos[i].Key.y, allCollidersInfos[i].Value.Count);
        }
    }


    /// <summary>
    /// Detect collision on GPU for active and moving colliders.
    /// The collision is calculated around a radius of collider mesh. For this we calculate some 
    /// offset inside the page and the amount os pixels that need be verified if is or not colliding with the object  
    /// (This is used to reduce the amount of groups dispatched on GPU and increase the performance).
    /// </summary>
    private void PrepareToCompute(TerrainColliderInteraction col, GrassHashCell cell)
    {
        Bounds bound = cell.boundsWorld;
        Vector3 min = bound.min;
        Vector3 size = bound.size;
        
        TerrainColliderInteraction.CollisionInfo collisionInfo = col.GetCollisionInfo();

        Vector3 radius = col.shape.meshRadius + new Vector3(TerrainColliderInteractionShape.meshRadiusOffset, 0, TerrainColliderInteractionShape.meshRadiusOffset);

        Vector3 pos = col.transform.position;

        aux_objBoundMin = pos - radius;
        aux_objBoundMax = pos + radius;

        aux_uvMin.x = Mathf.Clamp01((aux_objBoundMin.x - min.x) / size.x);
        aux_uvMin.y = Mathf.Clamp01((aux_objBoundMin.z - min.z) / size.z);

        aux_uvMax.x = Mathf.Clamp01((aux_objBoundMax.x - min.x) / size.x);
        aux_uvMax.y = Mathf.Clamp01((aux_objBoundMax.z - min.z) / size.z);

        aux_atlasPixeloffsetMin.x = (int)(aux_uvMin.x * m_atlasCollisionAtlasPageSize); //mathf.floorToInt
        aux_atlasPixeloffsetMin.y = (int)(aux_uvMin.y * m_atlasCollisionAtlasPageSize);

        aux_atlasPixeloffsetMax.x = 1 + (int)(aux_uvMax.x * m_atlasCollisionAtlasPageSize); //mathf.ceilToInt
        aux_atlasPixeloffsetMax.y = 1 + (int)(aux_uvMax.y * m_atlasCollisionAtlasPageSize);

        collisionInfo.boundMinMax = new Vector4(aux_atlasPixeloffsetMin.x, aux_atlasPixeloffsetMin.y, aux_atlasPixeloffsetMax.x, aux_atlasPixeloffsetMax.y);
        
        cell.lastTimeCollisionDetected = Time.time;

        int gx = 1 + (int)((aux_atlasPixeloffsetMax.x - aux_atlasPixeloffsetMin.x) / 8f);//mathf.ceilToInt
        int gy = 1 + (int)((aux_atlasPixeloffsetMax.y - aux_atlasPixeloffsetMin.y) / 8f);
        
        for (int i = 0; i < allCollidersInfos.Count; i++)
        {
            if(allCollidersInfos[i].Key.x == gx && allCollidersInfos[i].Key.y == gy)
            {
                allCollidersInfos[i].Value.Add(collisionInfo);
                return;
            }
        }
        
        allCollidersInfos.Add(new KeyValuePair<Vector2Int, List<TerrainColliderInteraction.CollisionInfo>>(new Vector2Int(gx, gy), new List<TerrainColliderInteraction.CollisionInfo>()));
        allCollidersInfos.Last().Value.Add(collisionInfo);
    }


    void Update()
    {
        if (Time.frameCount % 240 == 0)
        {
            ResetAllComputeBuffers();
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.C))
            DISABLE_GRASS_COLLISION = !DISABLE_GRASS_COLLISION;

        if (DISABLE_GRASS_COLLISION) return;

        UpdateStaticData();
        
       // RecoverVectorToInitialState();
       
        if (TerrainColliderInteraction.allColliders == null || TerrainColliderInteraction.allColliders.Count == 0)
        {
            return;
        }

        Profiler.BeginSample("Grass - Collision Detection " + TerrainColliderInteraction.allColliders.Count);
        
        allCollidersInfos.ForEach(c => c.Value?.Clear());

        if (Time.frameCount % 300 == 0)
            allCollidersInfos.Clear();

        foreach (TerrainColliderInteraction c in TerrainColliderInteraction.allColliders)
        {
            if (c == null || c.forceXZ.magnitude < 0.001f || !c.activeCollision)
            {
                continue;
            }

            if (c.consideOnlyInsideCell)
            {
                PrepareToCompute(c, c.cell);
            }
            else
            {
                List<GrassHashCell> cells = c.cells;

                foreach (GrassHashCell cell in cells)
                {
                    PrepareToCompute(c, cell);
                }
            }
        }

        DispatchAllCollisions();

        Profiler.EndSample();
    }





    private void ResetAllComputeBuffers()
    {
        ResizeComputeBuffer(ref allPagesToRecover, 1, sizeof(float) * 4);
    }
    

    private void ResizeComputeBuffer(ref ComputeBuffer comp, int counter, int stride)
    {
        if (comp != null && comp.stride == stride && comp.count == counter) return;

        comp?.Release();
        comp = null;

        comp = new ComputeBuffer(counter, stride);
    }


    private void UpdateStaticData()
    {
        compute.SetTexture(computeCollisionKernel, "_collisionmapAtlas", m_atlasCollisionAtlas.texture);
        compute.SetTexture(initializeAtlasPagekernel, "_collisionmapAtlas", m_atlasCollisionAtlas.texture);
        compute.SetTexture(vectorRecoverStateKernel, "_collisionmapAtlas", m_atlasCollisionAtlas.texture);
    }

    private void OnApplicationQuit()
    {
        hadCollisionOnPage?.Release();
    }


    /// <summary>
    /// Detect collision on GPU for active and moving colliders.
    /// The collision is calculated around a radius of collider mesh. For this we calculate some 
    /// offset inside the page and the amount os pixels that need be verified if is or not colliding with the object  
    /// (This is used to reduce the amount of groups dispatched on GPU and increase the performance).
    /// </summary>
    //private void ComputeCollision1(TerrainColliderInteraction col, GrassHashCell cell)
    //{
    //    Profiler.BeginSample("Grass - Collision Detection dispatch");
        
    //    Bounds bound = cell.boundsWorld;
    //    Vector3 min = bound.min;
    //    Vector3 size = bound.size;

    //    ///////
    //    compute.SetVector("_collisionmapDesc", cell.collisionPage.tl_size);
    //    ///////

    //    compute.SetVector("_cellWorldDesc", cell.boundsWorldMinSize);

    //    Vector3 pos = col.transform.position;
    //    compute.SetVector("_colliderPosition", pos);

    //    compute.SetVector("_forceDir", col.forceXZ);

    //    //col.shape.SetShapeComputeShader(compute);

    //    compute.SetFloat("_collisionRecoverSpeed", col.collisionRecoverSpeed);

    //    Vector3 radius = col.shape.meshRadius + new Vector3(TerrainColliderInteractionShape.meshRadiusOffset, 0, TerrainColliderInteractionShape.meshRadiusOffset);

    //    aux_objBoundMin = pos - radius;
    //    aux_objBoundMax = pos + radius;

    //    aux_uvMin.x = Mathf.Clamp01((aux_objBoundMin.x - min.x) / size.x);
    //    aux_uvMin.y = Mathf.Clamp01((aux_objBoundMin.z - min.z) / size.z);

    //    aux_uvMax.x = Mathf.Clamp01((aux_objBoundMax.x - min.x) / size.x);
    //    aux_uvMax.y = Mathf.Clamp01((aux_objBoundMax.z - min.z) / size.z);

    //    aux_atlasPixeloffsetMin.x = (int)(aux_uvMin.x * m_atlasCollisionAtlasPageSize); //mathf.floorToInt
    //    aux_atlasPixeloffsetMin.y = (int)(aux_uvMin.y * m_atlasCollisionAtlasPageSize);

    //    aux_atlasPixeloffsetMax.x = 1 + (int)(aux_uvMax.x * m_atlasCollisionAtlasPageSize); //mathf.ceilToInt
    //    aux_atlasPixeloffsetMax.y = 1 + (int)(aux_uvMax.y * m_atlasCollisionAtlasPageSize);

    //    compute.SetVector("_boundMin", aux_atlasPixeloffsetMin);
    //    compute.SetVector("_boundMax", aux_atlasPixeloffsetMax);

    //    int gx = 1 + (int)((aux_atlasPixeloffsetMax.x - aux_atlasPixeloffsetMin.x) / 8f);//mathf.ceilToInt
    //    int gy = 1 + (int)((aux_atlasPixeloffsetMax.y - aux_atlasPixeloffsetMin.y) / 8f);


    //    compute.Dispatch(computeCollisionKernel, gx, gy, 1);

    //    cell.lastTimeCollisionDetected = Time.time;

    //    Profiler.EndSample();
    //}

#if UNITY_EDITOR
    private void OnGUI()
    {
        return;

        //string[] msgs = new string[] {
        //    "Colliders : " + TerrainColliderInteraction.allColliders.Count,

        //    "Nodes Selected: " + info_cellsSelected.Distinct().ToList().Count,

        //    "Atlas Free Nodes: " + ObjectCollisionHashManager.Instance.m_CollisionmapAtlas.FreePageCount +"/"+
        //                           ObjectCollisionHashManager.Instance.m_CollisionmapAtlas.PageCount,

        //    "Cells with collision info: " + ObjectCollisionHashManager.Instance.info_cellWithCollision,

        //    "Cells stored in bytearray: " + ObjectCollisionHashManager.Instance.info_cellStoredInByteArray,

        //    "Cells stored in bytearray compressed: "  + ObjectCollisionHashManager.Instance.info_cellStoredInByteArrayCompressed,

        //    "All memory usage : " + ((ObjectCollisionHashManager.Instance.info_cellStoredInByteArrayMemory)).ToString("0.000") +" -- "
        //                          + ObjectCollisionHashManager.Instance.info_cellStoredInByteArrayMemoryReal.ToString("0.000"),

        //    "GPU groups dispatched :" + info_groupsDispatchedCount,
        //    "Grass instances count " + ObjetcRendererIndirect.info_instancesCount + " - " + ObjetcRendererIndirect.info_cellsCount,
        //    "Offset" + OriginOffsetManager.fOffset
        //};

        //float posX_Ini = 10;
        //float posY_Ini = 10;
        //float posY_Offset = 30;

        //float width = 200;
        //float height = 20;

        //GUI.color = Color.black;

        //if (GUI.Button(new Rect(posX_Ini, (posY_Ini += posY_Offset), width, height), "Delete Colliders"))
        //{
        //    TerrainColliderInteraction.DeleteAllColliders();
        //}

        //if (GUI.Button(new Rect(posX_Ini, (posY_Ini += posY_Offset), width, height), "Reset Atlas"))
        //{
        //    foreach (GrassHashCell c in ObjectCollisionHashManager.Instance.allCreatedCells)
        //    {
        //        InitializeAtlasPage(c.collisionAtlasDesc);
        //    }
        //}


        //if (GUI.Button(new Rect(posX_Ini, (posY_Ini += posY_Offset), width, height), (ASTMTerrain.ActiveTerrain.drawAllNodesBounds ? "Disable" : "Enable") + " draw nodes"))
        //{
        //    ASTMTerrain.ActiveTerrain.drawAllNodesBounds = !ASTMTerrain.ActiveTerrain.drawAllNodesBounds;
        //}

        //posY_Ini += 20;
        //posY_Offset = 20;
        //width = 300;
        //GUI.color = Color.blue;

        //for (int i = 0; i < msgs.Length; i++)
        //{
        //    GUI.Label(new Rect(posX_Ini, posY_Ini + posY_Offset, width, height), msgs[i]);

        //    posY_Ini += posY_Offset;
        //}
    }

#endif
}

