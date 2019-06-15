using System;
using UnityEngine;
using UnityEngine.Profiling;

public class GrassHashCell
{
    public float lastFrameRequestedCell;
    public float lastFrameCollisionDetected;
    public float lastTimeCollisionDetected;

    /// <summary>
    /// Store the positions pre-computed. Each buffer store the position for one kind of ObjectRenderer.
    /// </summary>
    private Atlas.AtlasPageDescriptor[] _positionsBuffer;
    public float[] lastFrameRequestedPositionsBuffer { get; private set; }

    /// <summary>
    /// Store the amount of valid positions stored in positionsBuffer.
    /// </summary>
    private ComputeBuffer _positionsBufferCounter;

    public Bounds boundsWorld { get; private set; }
    public Vector4 boundsWorldMinSize { get; private set; }

    public int seed { get; private set; }

    public float minDistToCamera;
    
    public bool _hasPositionBuffer = false;
    public bool hasPositionBuffer { get { return _hasPositionBuffer; } }

    public float m_collisionResultantDuration = 0;
    public float slowerRecoverSpeed
    {
        get
        {
            return m_collisionResultantDuration;
        }
        set
        {
            m_collisionResultantDuration = Mathf.Max(value, m_collisionResultantDuration);
        }
    }


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

        _positionsBuffer = new Atlas.AtlasPageDescriptor[ObjectRendererManager.Instance.objectRenderers.Length];
        lastFrameRequestedPositionsBuffer = new float[ObjectRendererManager.Instance.objectRenderers.Length];

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
    public Atlas.AtlasPageDescriptor GetPositionsBuffer(ObjetcRendererIndirect renderer)
    {
        if (_positionsBuffer[renderer.rendererID] == null)
        {
            if (renderer.positionsBuffer.FreePageCount == 0)
                GrassHashManager.Instance.ReleasePositionsBufferForced();

            _positionsBuffer[renderer.rendererID] = renderer.positionsBuffer.GetPage();

            PreComputePositions.Instance.ComputePositions(_positionsBuffer[renderer.rendererID], this, renderer.m_config);

            _hasPositionBuffer = true;
        }

        lastFrameRequestedPositionsBuffer[renderer.rendererID] = Time.frameCount;
        
        PreComputePositions.Instance.ComputePositions(_positionsBuffer[renderer.rendererID], this, renderer.m_config);

        return _positionsBuffer[renderer.rendererID];
    }


    public bool hasCollisionPage { get { return m_collisionPage != null; } }

    private Atlas.AtlasPageDescriptor m_collisionPage;
    public Atlas.AtlasPageDescriptor collisionPage
    {
        get
        {
            if (m_collisionPage == null)
            {
                m_collisionPage = GrassHashManager.Instance.GetEmptyCollisionAtlasPage();
                GrassHashManager.Instance.allCreatedCellsWithCollision.Add(this);
            }

            lastFrameCollisionDetected = Time.frameCount;

            return m_collisionPage;
        }
    }




    public bool HasValidCollisionInfoOnPage
    {
        get
        {
            return true;
            return (Time.time - lastTimeCollisionDetected) < (1f / slowerRecoverSpeed);
        }
    }
    

    public void ReleaseCollisionPage()
    {
        if(!HasValidCollisionInfoOnPage)
        {
            m_collisionPage?.Release();
            m_collisionPage = null;
            return;
        }
    }




    /// <summary>
    /// Release all created buffers with the pre-computed positions used by the objectRenderers.
    /// </summary>
    public void ReleasePositionsBuffer(int rendererID)
    {
        _positionsBuffer[rendererID]?.Release();
        _positionsBuffer[rendererID] = null;

        _hasPositionBuffer = false;

        for (int i = 0; i < _positionsBuffer.Length; i++)
        {
            if (_positionsBuffer[i] != null)
            {
                _hasPositionBuffer = true;
                break;
            }
        }
    }


    public void ReleaseAllPositionsBuffer()
    {
        for (int i = 0; i < ObjectRendererManager.Instance.objectRenderers.Length; i++)
            ReleasePositionsBuffer(i);
    }

    ~GrassHashCell()
    {
        //TODO
    }
}

