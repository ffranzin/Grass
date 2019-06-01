using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GrassHashManager : Singleton<GrassHashManager>
{
    public ComputeShader distributePositionsCompute;
    
    /// <summary>
    /// Store a reference of created cells. The access of each cell if through of world position (without camera offset).  
    /// </summary>
    private GrassHashCell[,] hash;
    /// <summary>
    /// Store all created cells. Basically is used to avoid going through all hash to do something in a existed cell. These cells is stored in a hash too. 
    /// </summary>
    public List<GrassHashCell> allCreatedCells = new List<GrassHashCell>();
    public List<GrassHashCell> allCreatedCellsWithCollision = new List<GrassHashCell>();

    public float cellHSize { get; private set; }
    public float cellVSize { get; private set; }

    private int cellHCounter;
    private int cellVCounter;

    float terrainSize = 1024;
    float terrainCellSize = 16;
     
    private void Awake()
    {
        Vector2 tSize = Vector2.one * terrainSize;
        Vector2 cSize = Vector2.one * terrainCellSize;

        InitializeHash(tSize, cSize);
    }

    public void InitializeHash(Vector2 terrainSize, Vector2 cellSize)
    {
        cellHSize = cellSize.x;
        cellVSize = cellSize.y;
        
        cellHCounter = Mathf.CeilToInt((float)(terrainSize.x / cellSize.x));
        cellVCounter = Mathf.CeilToInt((float)(terrainSize.y / cellSize.y));

        hash = new GrassHashCell[cellHCounter + 1, cellVCounter + 1];

        hashDebug = new Texture2D(cellHCounter, cellVCounter, TextureFormat.RGBA32, false);
        hashDebug.filterMode = FilterMode.Point;
        hashDebug.wrapMode = TextureWrapMode.Clamp;
        hashDebug.Apply();

        GameObject.Find("HashDebug").GetComponent<RawImage>().texture = hashDebug;
        
        Debug.Log("Object hash initialized [" + cellHCounter + ", " + cellVCounter + "].");
    }


    /// <summary>
    /// Find a cell in hash at position (without camera offset).
    /// </summary>
    public GrassHashCell GetHashCell(Vector3 pos)
    {
        int l = (int)(pos.x / cellHSize);
        int c = (int)(pos.z / cellVSize);

        if (l < 0 || c < 0 || l > cellHCounter || c > cellVCounter)
        {
            Debug.LogError("Cell outside of terrain");
            return null;
        }

        hash[l, c] = hash[l, c] ?? new GrassHashCell(l, c);

        hash[l, c].lastCellRequestedTime = Time.time;
        return hash[l, c];
    }


    public GrassHashCell GetHashCell(int l, int c)
    {
        if (l < 0 || c < 0 || l > cellHCounter || c > cellVCounter)
        {
            Debug.LogError("Cell outside of terrain");
            return null;
        }

        hash[l, c] = hash[l, c] ?? new GrassHashCell(l, c);

        hash[l, c].lastCellRequestedTime = Time.time;
        return hash[l, c];
    }



    /// <summary>
    /// Find all cells around a position (without camera offset) and radius. The output list is not cleaned at this method.
    /// </summary>
    public void GetCellsAroundPos(Vector3 positionNoOffset, float radius, ref List<GrassHashCell> selectedCells)
    {
        int l = (int)(positionNoOffset.x / cellHSize);//floorToInt
        int c = (int)(positionNoOffset.z / cellVSize);

        int offSetX = 1 + (int)(radius / cellHSize);//ceilToInt
        int offSetY = 1 + (int)(radius / cellVSize);

        //No cells outside of the terrain
        int iniX = Mathf.Clamp(l - offSetX, 0, cellHCounter);
        int endX = Mathf.Clamp(l + offSetX, 0, cellHCounter);

        int iniY = Mathf.Clamp(c - offSetY, 0, cellVCounter);
        int endY = Mathf.Clamp(c + offSetY, 0, cellVCounter);

        for (int i = iniX; i <= endX; i++)
        {
            for (int j = iniY; j <= endY; j++)
            {
                hash[i, j] = hash[i, j] ?? new GrassHashCell(i, j);
                hash[i, j].lastCellRequestedTime = Time.time;
                
                float dist = Mathf.Sqrt(hash[i, j].boundsWorld.SqrDistance(positionNoOffset));

                if (dist < radius)
                {
                    selectedCells.Add(hash[i, j]);
                    hash[i, j].minDistToCamera = dist;
                }
            }
        }
    }


    public Atlas.AtlasPageDescriptor GetEmptyCollisionAtlasPage()
    {
        return ObjectCollisionManager.Instance.GetEmptyAtlasPage();
    }

    public Atlas CollisionAtlas()
    {
        return ObjectCollisionManager.Instance.m_atlasCollisionAtlas;
    }


    /// <summary>
    /// Find unused cells and release all used memory. This process accur slowly to avoid any processing peak.  
    /// </summary>
    //public void ReleaseActiveCells()
    //{
    //    timeLastReleaseSearch = Time.time;

    //    Vector3 camNoOffset = OriginOffsetManager.RemoveOffset(OriginOffsetManager.currentCamera.transform.position);

    //    int cellsReleased = 0;
    //    int cellsCompressed = 0;

    //    Profiler.BeginSample("Object - Release Memory");

    //    float t = Time.time;

    //    int k = Mathf.Min(allCreatedCells.Count, lastCellToReleaseVisited + maxCellsVisidedToReleaseMemoryPerFrame);

    //    for (int i = lastCellToReleaseVisited; i < k && i < allCreatedCells.Count; i++)
    //    {
    //        lastCellToReleaseVisited++;

    //        ObjectCollisionHashCell cell = allCreatedCells[i];

    //        if (cell == null) continue;

    //        Bounds b = cell.boundsWorld;

    //        if (distToReleaseCell > Mathf.Sqrt(b.SqrDistance(camNoOffset))) continue;

    //        if (!cell.hadPermanentCollisionInsideCell && (t - cell.lastCellRequestedTime) > timeToReleaseCell)
    //        {
    //            int l = Mathf.FloorToInt((float)(cell.boundsWorld.center.x / cellHSize));
    //            int c = Mathf.FloorToInt((float)(cell.boundsWorld.center.z / cellVSize));

    //            cell.ReleaseCell();
    //            cell = null;

    //            hash[l, c] = null;

    //            allCreatedCells.RemoveAt(i);
    //            continue;
    //        }

    //        if ((Time.time - cell.lastCellRequestedTime) > timeToReleasePositionsBuffer)
    //            cell.ReleasePositionsBuffer();

    //        if ((Time.time - cell.lastCellRequestedTime) > timeToReleaseAtlas)
    //            cell.ReleaseAuxAtlasesPage();

    //        if (cell.hadCollisionInsideCell && !cell.hasColliderInsideCell)
    //        {
    //            if (maxPagesReleasedPerFrame > cellsReleased && (Time.time - cell.lastTextureUsedTime) > timeToCompressByteArray)
    //            {
    //                if (cell.ReleaseCollisionAtlasPage())
    //                    cellsReleased++;
    //            }
    //            if (maxCompressionPerFrame > cellsCompressed && (Time.time - cell.lastTextureUsedTime) > timeToCompressByteArray)
    //            {
    //                if (cell.CompressByteArray())
    //                    cellsCompressed++;
    //            }
    //        }
    //    }


    //    if (lastCellToReleaseVisited >= allCreatedCells.Count)
    //        lastCellToReleaseVisited = 0;

    //    Profiler.EndSample();
    //}


    private void Update()
    {
#if UNITY_EDITOR
        // UpdateInfos();
#endif
       

        //ReleaseActiveCells();
    }
    

    private void OnDestroy()
    {
        for (int i = 0; i < cellHCounter; i++)
        {
            for (int j = 0; j < cellVCounter; j++)
            {
                hash[i, j] = null;
            }
        }

        hash = null;
        base.OnDestroy();
    }


    public static Texture2D hashDebug;
    

    public void SetNewCell(int x, int y)
    {
        hashDebug.SetPixel(x, y, Color.red);
        hashDebug.Apply();
    }


    #region DEBUG_INFOS
    
    public void OnDrawGizmos()
    {
        foreach(GrassHashCell c in allCreatedCells)
        {
            Gizmos.DrawLine(Vector3.zero, new Vector3(0, 0, terrainSize));
            Gizmos.DrawLine(Vector3.zero, new Vector3(terrainSize, 0, 0));
            Gizmos.DrawLine(new Vector3(terrainSize, 0, terrainSize), new Vector3(0, 0, terrainSize));
            Gizmos.DrawLine(new Vector3(terrainSize, 0, terrainSize), new Vector3(terrainSize, 0, 0));
        }
    }


    //public void UpdateInfos()
    //{
    //    if (Time.frameCount % 60 != 0)
    //        return;

    //    info_cellStoredInByteArray = 0;
    //    info_cellStoredInByteArrayMemoryReal = 0;
    //    info_cellStoredInByteArrayCompressed = 0;
    //    info_cellWithCollision = 0;

    //    for (int i = 0; i < cellHCounter; i++)
    //    {
    //        for (int j = 0; j < cellVCounter; j++)
    //        {
    //            if (hash[i, j] == null) continue;

    //            info_cellWithCollision += hash[i, j].hadCollisionInsideCell ? 1 : 0;

    //            if (hash[i, j].byteArray != null)
    //            {
    //                info_cellStoredInByteArray++;
    //                info_cellStoredInByteArrayMemoryReal += hash[i, j].byteArray.Length;
    //                if (hash[i, j].isByteArrayCompressed)
    //                    info_cellStoredInByteArrayCompressed++;
    //            }
    //        }
    //    }

    //    info_cellStoredInByteArrayMemory = info_cellStoredInByteArray * (4 * sizeof(float) * CollisionmapSizePadded * CollisionmapSizePadded) / (1024f * 1024f);
    //    info_cellStoredInByteArrayMemoryReal = info_cellStoredInByteArrayMemoryReal / (1024f * 1024f);

    //}

    #endregion
}
