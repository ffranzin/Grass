using System;
using UnityEngine;
using UnityEngine.Profiling;

public class GrassHashCell
{
    public float lastCellRequestedTime;
    public float lastCollisionTime;

    /// <summary>
    /// Store the positions pre-computed. Each buffer store the position for one kind of ObjectRenderer.
    /// </summary>
    private ComputeBuffer[] _positionsBuffer;
    /// <summary>
    /// Store the amount of valid positions stored in positionsBuffer.
    /// </summary>
    private ComputeBuffer _positionsBufferCounter;

    public Bounds boundsWorld { get; private set; }

    public Vector4 boundsWorldMinSize;

    public int seed;

    public float minDistToCamera;

    public bool hadCollisionInsideCell, hadPermanentCollisionInsideCell;

    public GrassHashCell(int hashPosI, int hashPosJ)
    {
        Vector3 cellCenter = new Vector3(GrassHashManager.Instance.cellHSize * (hashPosI + 0.5f), 0,
                                        GrassHashManager.Instance.cellVSize * (hashPosJ + 0.5f));
        Vector3 cellSize = new Vector3(GrassHashManager.Instance.cellHSize, 1,
                                        GrassHashManager.Instance.cellVSize);
        boundsWorld = new Bounds(cellCenter, cellSize);
        
        GrassHashManager.Instance.allCreatedCells.Add(this);
        GrassHashManager.Instance.SetNewCell(hashPosI, hashPosJ);

        InstanciateCollider();

        _positionsBuffer = new ComputeBuffer[ObjectRendererManager.Instance.objectRenderers.Length];

        boundsWorldMinSize = new Vector4(boundsWorld.min.x, boundsWorld.min.z, boundsWorld.size.x, boundsWorld.size.z);

        seed = UnityEngine.Random.Range(1, 100);
    }
    
    private void InstanciateCollider()
    {
        GameObject collider = GameObject.CreatePrimitive(PrimitiveType.Plane);
        collider.transform.position = boundsWorld.center;
        collider.transform.localScale = boundsWorld.size / 10f;
        collider.GetComponent<MeshRenderer>().material = Resources.Load<Material>("GroundMaterial");
        Vector3 c = UnityEngine.Random.insideUnitSphere;
        
        //collider.GetComponent<MeshRenderer>().material.SetColor("_Color", new Vector4(c.x, c.y, c.z, 1));
        TerrainCollider terrainCollider = collider.AddComponent<TerrainCollider>();
        TerrainData terraindata = new TerrainData();

        terraindata.size = new Vector3(boundsWorld.size.x, 0, boundsWorld.size.z);
        terraindata.heightmapResolution = (int)boundsWorld.size.x;

        terrainCollider.terrainData = terraindata;
    }


    /// <summary>
    /// Each cell contains a list of buffers with the pre-computed position for each kind of objectrenderer.
    /// The renderer get the buffer through this method, that control the buffer generation when it is necessary.
    /// </summary>
    public ComputeBuffer GetPositionsBuffer(ObjetcRendererIndirect renderer)
    {
        if (_positionsBuffer[renderer.rendererID] == null)
        {
            _positionsBuffer[renderer.rendererID] = renderer.positionsBufferPool.GetBuffer();

            PreComputePositions.Instance.ComputePositions(_positionsBuffer[renderer.rendererID], this, renderer.m_config);
        }

        //PreComputePositions.Instance.ComputePositions(_positionsBuffer[renderer.rendererID], this, renderer.m_config);

        return _positionsBuffer[renderer.rendererID];
    }


    private Atlas.AtlasPageDescriptor m_collisionPage;
    public Atlas.AtlasPageDescriptor collisionPage
    {
        get
        {
            if (m_collisionPage == null)
                m_collisionPage = GrassHashManager.Instance.GetEmptyCollisionAtlasPage();
            return m_collisionPage;
        }
    }



    /// <summary>
    /// Release all created buffers with the pre-computed positions used by the objectRenderers.
    /// </summary>
    public void ReleasePositionsBuffer()
    {
        //_positionsBuffer?.Dispose();
        //_positionsBufferCounter?.Dispose();
        //_positionsBuffer = null;
        //_positionsBufferCounter = null;
    }

    
    ~GrassHashCell()
    {
       //TODO
    }
}

