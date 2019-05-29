
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

[Serializable]
public struct Grass_LOD
{
    public float distance;
    public Mesh bladeMesh;
}

[Serializable]
public class GrassConfig
{
    [Header("Grass Scale")]
    [Range(0, 5)]
    public float minScale = 0;
    [Range(0, 5)]
    public float maxScale = 1;

    [Range(0.01f, 10)]
    public float distributionDensity = 4;

    [Space]
    [Header("Grass Configuration")]
    public bool receiveShadow = false;
    public ShadowCastingMode shadowCasting = ShadowCastingMode.Off;
    public bool useMipMap = true;
    [Range(-5, 0)]
    public float mipMapBias = -1.5f;

    [HideInInspector] public int seed;

    public Material objectRendererMaterial;

    public List<Grass_LOD> LODRanges;

    public float distanceViewRange { get { return LODRanges[LODRanges.Count - 1].distance; } }
}


/*
public class ObjetcRendererIndirect : MonoBehaviour
{
    public GrassConfig m_config;
    public ComputeBufferPool positionsBufferPool { get; private set; }

    [HideInInspector] public int instances1D { get; private set; }
    [HideInInspector] public int instances2D { get; private set; }

    [HideInInspector] public int rendererID;

    private int MAX_NODES;
    private int activeCellsCounter;
    
    private GrassHashCell[] cellsToRender;

    private ComputeBuffer[] computeBufferArgsQuad;
    private MaterialPropertyBlock[] propBlocksQuad;
    private int[] renderLODIndex;
    private ComputeBuffer LODRanges;

    uint[] args = new uint[5];

    Bounds boundMaxViewRange;


    public static int MaxNodesToDistance(float distance, float cellSize)
    {
        return (int)Mathf.Pow(1 + Mathf.Ceil(((2f * distance) / cellSize)), 2f); ;
    }
    

    public void Initialize()
    {
        MAX_NODES = MaxNodesToDistance(m_config.LODRanges.Last().distance, GrassHashManager.Instance.cellHSize);

        cellsToRender = new GrassHashCell[MAX_NODES];
        propBlocksQuad = new MaterialPropertyBlock[MAX_NODES];
        computeBufferArgsQuad = new ComputeBuffer[MAX_NODES];
        LODRanges = new ComputeBuffer(m_config.LODRanges.Count, sizeof(float));
        renderLODIndex = new int[MAX_NODES];

        instances1D = (int)(GrassHashManager.Instance.cellHSize * m_config.distributionDensity);
        instances2D = instances1D * instances1D;
        
        positionsBufferPool = new ComputeBufferPool(MAX_NODES * 5, instances2D, 4 * sizeof(float), ComputeBufferType.Append);

        boundMaxViewRange = new Bounds(Vector3.zero, Vector3.one * 2f * m_config.LODRanges.Last().distance);

        m_config.seed = UnityEngine.Random.Range(1, 100);

        InitializeAllPropBlock();

        UpdateInfosOnShaders.Instance.AddNewCallback = InitializeAllPropBlock;
    }

    private int SelectLODIndex(GrassHashCell cell)
    {
        float dist = cell.minDistToCamera;

        for (int i = 0; i < m_config.LODRanges.Count; i++)
        {
            if (dist < m_config.LODRanges[i].distance)
                return i;
        }

        return m_config.LODRanges.Count - 1;
    }


    private void FillArgs(ref ComputeBuffer compute, int lodIndex)
    {
        args[0] = (uint)m_config.LODRanges[lodIndex].bladeMesh.GetIndexCount(0);
        args[1] = (uint)0;// instances count - setted by copyCount
        args[2] = (uint)0;// m_config.LODRanges[lodIndex].bladeMesh.GetIndexStart(0);
        args[3] = (uint)0;// m_config.LODRanges[lodIndex].bladeMesh.GetBaseVertex(0);
       
        compute.SetData(args);
    }

    /// <summary>
    /// Add cell in list to render and prepare all infos necessary to render.
    /// </summary>
    public void PrepareCellToRender(GrassHashCell cell)
    {
        if (activeCellsCounter > MAX_NODES) return;

        Profiler.BeginSample("Grass - Prepare cells");

        int lodIndex = SelectLODIndex(cell);
        
        FillArgs(ref computeBufferArgsQuad[activeCellsCounter], lodIndex);
        
        propBlocksQuad[activeCellsCounter].SetVector("_collisionmapDesc", cell.collisionPage.tl_size);

        propBlocksQuad[activeCellsCounter].SetVector("_cellWorldDesc", cell.boundsWorldMinSize);
        
        propBlocksQuad[activeCellsCounter].SetInt("_CurrentLOD", lodIndex);

        renderLODIndex[activeCellsCounter] = lodIndex;

        cellsToRender[activeCellsCounter] = cell; 

        activeCellsCounter++;
        Profiler.EndSample();
    }
    

    public void RenderCellsQuad()
    {
        Profiler.BeginSample("Grass - Render cells :: ID : " + rendererID);

        boundMaxViewRange.center = Camera.main.transform.position;

        for (int i = 0; i < activeCellsCounter; i++)
        {
            ComputeBuffer renderPositions = cellsToRender[i].GetPositionsBuffer(this);

            propBlocksQuad[i].SetBuffer("_positionsBuffer", renderPositions);
            
            ComputeBuffer.CopyCount(renderPositions, computeBufferArgsQuad[i], sizeof(uint));
  
            Graphics.DrawMeshInstancedIndirect(m_config.LODRanges[renderLODIndex[i]].bladeMesh, 0, m_config.objectRendererMaterial, boundMaxViewRange,
                                           computeBufferArgsQuad[i], 0, propBlocksQuad[i], m_config.shadowCasting, m_config.receiveShadow);
        }

        Profiler.EndSample();

        activeCellsCounter = 0;
    }


    private void InitializeAllPropBlock()
    {
        for (int i = 0; i < MAX_NODES; i++)
        {
            if(propBlocksQuad[i] == null)
                propBlocksQuad[i] = new MaterialPropertyBlock();

            if(computeBufferArgsQuad[i] == null)
                computeBufferArgsQuad[i] = new ComputeBuffer(args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
            
            propBlocksQuad[i].SetTexture("_collisionAtlas", GrassHashManager.Instance.CollisionAtlas().texture);

            List<float> lodDistances = new List<float>();
            foreach (Grass_LOD l in m_config.LODRanges)
                lodDistances.Add(l.distance);
            LODRanges.SetData(lodDistances);

            propBlocksQuad[i].SetBuffer("_LODRanges", LODRanges);
            propBlocksQuad[i].SetInt("_LODCount", m_config.LODRanges.Count);
        }
    }


    private void OnApplicationQuit()
    {
        positionsBufferPool?.Release();
        LODRanges?.Release();

        for (int i = 0; i < MAX_NODES; i++)
        {
            propBlocksQuad[i] = null;
            computeBufferArgsQuad[i].Release();
            computeBufferArgsQuad[i] = null;
        }
        
        cellsToRender = null;
        propBlocksQuad = null;
        computeBufferArgsQuad = null;
        LODRanges = null;
    }
}
*/

public class ObjetcRendererIndirect : MonoBehaviour
{
    public GrassConfig m_config;
    //public ComputeBufferPool positionsBufferPool { get; private set; }

    [HideInInspector] public int instances1D { get; private set; }
    [HideInInspector] public int instances2D { get; private set; }

    [HideInInspector] public int rendererID;

    private int MAX_NODES;

    private List<Atlas.AtlasPageDescriptor> buffersToRender;
    private List<GrassHashCell> cellsToRender;
    private ComputeBuffer[] computeBufferArgs;
    private MaterialPropertyBlock[] propBlocks;

    private ComputeBuffer[] computeBufferArgsQuad;
    private MaterialPropertyBlock[] propBlocksQuad;

    private int activeCellsCount;

    private ComputeBuffer[] outputLODBuffers;
    private ComputeBuffer[] outputLODBuffersRotated;

    private ComputeBuffer LOD_Definitions;

    public Atlas positionsBuffer;

    uint[] args = new uint[5];

    public static int MaxNodesToDistance(float distance, float cellSize)
    {
        return (int)Mathf.Pow(1 + Mathf.Ceil(((2f * distance) / cellSize)), 2f); ;
    }

    public void Initialize()
    {
        MAX_NODES = MaxNodesToDistance(m_config.distanceViewRange, GrassHashManager.Instance.cellHSize);

        buffersToRender = new List<Atlas.AtlasPageDescriptor>();
        cellsToRender = new List<GrassHashCell>();
        propBlocks = new MaterialPropertyBlock[m_config.LODRanges.Count];
        computeBufferArgs = new ComputeBuffer[m_config.LODRanges.Count];
        outputLODBuffers = new ComputeBuffer[m_config.LODRanges.Count];
        outputLODBuffersRotated = new ComputeBuffer[m_config.LODRanges.Count];

        float[] lodDistances = new float[m_config.LODRanges.Count];
        {
            for (int i = 0; i < m_config.LODRanges.Count; i++)
            {
                propBlocks[i] = new MaterialPropertyBlock();

                args[0] = (uint)m_config.LODRanges[i].bladeMesh.GetIndexCount(0);
                args[1] = (uint)0;
                args[2] = (uint)m_config.LODRanges[i].bladeMesh.GetIndexStart(0);
                args[3] = (uint)m_config.LODRanges[i].bladeMesh.GetBaseVertex(0);

                computeBufferArgs[i] = new ComputeBuffer(args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);
                computeBufferArgs[i].SetData(args);

                propBlocks[i].SetFloat("_maxViewRange", m_config.LODRanges[m_config.LODRanges.Count - 1].distance);

                lodDistances[i] = m_config.LODRanges[i].distance;
            }
        }

        propBlocksQuad = new MaterialPropertyBlock[MAX_NODES];
        computeBufferArgsQuad = new ComputeBuffer[MAX_NODES];

        for (int i = 0; i < MAX_NODES; i++)
        {
            propBlocksQuad[i] = new MaterialPropertyBlock();

            computeBufferArgsQuad[i] = new ComputeBuffer(args.Length, sizeof(uint), ComputeBufferType.IndirectArguments);

            propBlocksQuad[i].SetFloat("_maxViewRange", m_config.LODRanges[m_config.LODRanges.Count - 1].distance);
        }

        instances1D = (int)(GrassHashManager.Instance.cellHSize * m_config.distributionDensity);
        instances2D = instances1D * instances1D;
        //positionsBufferPool = new ComputeBufferPool(MAX_NODES * 5, instances2D, 4 * sizeof(float), ComputeBufferType.Append);

        float counter1 = (m_config.distributionDensity * m_config.distributionDensity) * (Mathf.PI * Mathf.Pow(m_config.LODRanges[0].distance, 2f));
        float counter2 = (m_config.distributionDensity * m_config.distributionDensity) * (Mathf.PI * Mathf.Pow(m_config.LODRanges[1].distance, 2f)) - counter1;
        float counter3 = (m_config.distributionDensity * m_config.distributionDensity) * (Mathf.PI * Mathf.Pow(m_config.LODRanges[2].distance, 2f)) - (counter1 + counter2);

        outputLODBuffers[0] = new ComputeBuffer((int)counter1, sizeof(float) * 4, ComputeBufferType.Append);
        outputLODBuffers[1] = new ComputeBuffer((int)counter2, sizeof(float) * 4, ComputeBufferType.Append);
        outputLODBuffers[2] = new ComputeBuffer((int)counter3, sizeof(float) * 4, ComputeBufferType.Append);

        outputLODBuffersRotated[0] = new ComputeBuffer((int)counter1, sizeof(float) * 8, ComputeBufferType.Append);
        outputLODBuffersRotated[1] = new ComputeBuffer((int)counter2, sizeof(float) * 8, ComputeBufferType.Append);
        outputLODBuffersRotated[2] = new ComputeBuffer((int)counter3, sizeof(float) * 8, ComputeBufferType.Append);


#if UNITY_EDITOR
        int memory = (int)(((counter1 + counter2 + counter3) * 4 * sizeof(float)) / (8f * 1024f * 1024f));
        Debug.Log("LOD buffers : " + memory.ToString("0.00") + "MB");
#endif  

        LOD_Definitions = new ComputeBuffer(m_config.LODRanges.Count, sizeof(float));
        LOD_Definitions.SetData(lodDistances);

        m_config.seed = UnityEngine.Random.Range(1, 100);

         positionsBuffer = new Atlas(RenderTextureFormat.ARGBFloat, FilterMode.Point, 8192, instances1D, false);
    }


    /// <summary>
    /// Add cell in list to render and prepare all infos necessary to render.
    /// </summary>
    public void PrepareCellToRender(GrassHashCell cell)
    {
        buffersToRender.Add(cell.GetPositionsBuffer(this));
        cellsToRender.Add(cell);
    }


    int SelectLOD(GrassHashCell cell, Vector3 position)
    {
        float dist = Mathf.Sqrt(cell.boundsWorld.SqrDistance(position));

        for (int i = 0; i < m_config.LODRanges.Count; i++)
        {
            if (dist < m_config.LODRanges[i].distance)
                return i;
        }

        return 999999;
    }


    void FillArgs(ref ComputeBuffer compute, int lodIndex)
    {
        args[0] = (uint)m_config.LODRanges[lodIndex].bladeMesh.GetIndexCount(0);
        args[1] = (uint)0;
        args[2] = (uint)m_config.LODRanges[lodIndex].bladeMesh.GetIndexStart(0);
        args[3] = (uint)m_config.LODRanges[lodIndex].bladeMesh.GetBaseVertex(0);

        compute.SetData(args);
    }


    public void RenderCellsQuad()
    {
        Bounds b = new Bounds(Camera.main.transform.position, Vector3.one * 100);

        Vector3 camPos = Camera.main.transform.position;
        camPos.y = 0;

        m_config.objectRendererMaterial.SetVector("_cameraWorldPos", camPos);
        m_config.objectRendererMaterial.SetBuffer("_LODRanges", LOD_Definitions);
        m_config.objectRendererMaterial.SetTexture("_collisionAtlas", cellsToRender[0].collisionPage.atlas.texture);

        //for (int i = 0; i < cellsToRender.Count; i++)
        //{
        //    int lodIndex = SelectLOD(cellsToRender[i], camPos);

        //    if (lodIndex > m_config.LODRanges.Count) continue;

        //    ComputeBuffer positions = cellsToRender[i].GetPositionsBuffer(this);

        //    propBlocksQuad[i].SetBuffer("_positionsBuffer", positions);

        //    Vector2 tl = cellsToRender[i].collisionPage.tl;
        //    int s = cellsToRender[i].collisionPage.size;

        //    propBlocksQuad[i].SetVector("_collisionmapDesc", new Vector4(tl.x, tl.y, s, 0));

        //    propBlocksQuad[i].SetVector("_cellWorldDesc", cellsToRender[i].boundsWorldMinSize);

        //    FillArgs(ref computeBufferArgsQuad[i], lodIndex);

        //    propBlocksQuad[i].SetInt("_CurrentLOD", lodIndex);

        //    ComputeBuffer.CopyCount(positions, computeBufferArgsQuad[i], sizeof(uint));

        //    Graphics.DrawMeshInstancedIndirect(m_config.LODRanges[lodIndex].bladeMesh, 0, m_config.objectRendererMaterial, b,
        //                                   computeBufferArgsQuad[i], 0, propBlocksQuad[i], m_config.shadowCasting, m_config.receiveShadow);
        //}

        buffersToRender.Clear();
        cellsToRender.Clear();
    }


    public void RenderCells()
    {
        LODManager.Instance.CreateLODs(buffersToRender, outputLODBuffers, LOD_Definitions);

        Vector3 camPos = Camera.main.transform.position;
        camPos.y = 0;

        Bounds b = new Bounds(Camera.main.transform.position, Vector3.one * 10000);

        for (int i = 0; i < m_config.LODRanges.Count; i++)
        {
            propBlocks[i].SetBuffer("_positionsBuffer", outputLODBuffers[i]);
            propBlocks[i].SetBuffer("_LODRanges", LOD_Definitions);

            propBlocks[i].SetInt("_CurrentLOD", i);
            propBlocks[i].SetVector("_cameraWorldPos", camPos);
            ComputeBuffer.CopyCount(outputLODBuffers[i], computeBufferArgs[i], 1 * sizeof(uint));

            Graphics.DrawMeshInstancedIndirect(m_config.LODRanges[i].bladeMesh, 0, m_config.objectRendererMaterial, b,
                                           computeBufferArgs[i], 0, propBlocks[i], m_config.shadowCasting, m_config.receiveShadow);
        }
        

        buffersToRender.Clear();
        cellsToRender.Clear();
    }


    private void OnApplicationQuit()
    {
       // positionsBufferPool.Release();

        for (int i = 0; i < MAX_NODES; i++)
        {
            computeBufferArgs[i]?.Release();
        }
    }
}



//public void RenderCells()
//{
//    LODManager.Instance.CreateLODs(buffersToRender, outputLODBuffers, outputLODBuffersRotated, LOD_Definitions);

//    Vector3 camPos = Camera.main.transform.position;
//    camPos.y = 0;

//    Bounds b = new Bounds(Camera.main.transform.position, Vector3.one * 10000);

//    for (int i = 0; i < m_config.LOD_Definitions.Length; i++)
//    {
//        propBlocks[i].SetBuffer("_positionsBuffer", outputLODBuffers[i]);
//        propBlocks[i].SetBuffer("_LODRanges", LOD_Definitions);

//        propBlocks[i].SetInt("_CurrentLOD", i);
//        propBlocks[i].SetVector("_cameraWorldPos", camPos);
//        ComputeBuffer.CopyCount(outputLODBuffers[i], computeBufferArgs[i], 1 * sizeof(uint));

//        Graphics.DrawMeshInstancedIndirect(m_config.LOD_Definitions[i].bladeMesh, 0, m_config.objectRendererMaterial, b,
//                                       computeBufferArgs[i], 0, propBlocks[i], m_config.shadowCasting, m_config.receiveShadow);
//    }


//    for (int i = 0; i < m_config.LOD_Definitions.Length; i++)
//    {
//        propBlocks[i].SetBuffer("_positionsBufferRotated", outputLODBuffersRotated[i]);
//        propBlocks[i].SetBuffer("_LODRanges", LOD_Definitions);

//        propBlocks[i].SetInt("_CurrentLOD", i);
//        propBlocks[i].SetVector("_cameraWorldPos", camPos);
//        ComputeBuffer.CopyCount(outputLODBuffersRotated[i], computeBufferArgs[i], 1 * sizeof(uint));

//        Graphics.DrawMeshInstancedIndirect(m_config.LOD_Definitions[i].bladeMesh, 0, m_config.objectRendererMaterial, b,
//                                       computeBufferArgs[i], 0, propBlocks[i], m_config.shadowCasting, m_config.receiveShadow);
//    }


//    buffersToRender.Clear();
//    cellsToRender.Clear();
//}


//}