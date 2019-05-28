

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

public class QuadTreeNode
{
    public QuadTreeNode()
    {
        bound = new Bounds();
        children = new QuadTreeNode[4];
    }

    public Bounds bound;

    public QuadTreeNode[] children;

    public QuadTreeNode parent;
    public int level;
    public Atlas.AtlasPageDescriptor atlasPage = null;

    public bool hasChild = false;

    public bool isVisible
    {
        get
        {
            Vector3 cameraPos = new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z);

            float distance = Mathf.Sqrt(bound.SqrDistance(cameraPos));

            return GeometryUtility.TestPlanesAABB(TerrainManager.frustumPlanes, bound) && distance < 100;
        }
    }


    public void Reset()
    {
        children[0] = children[1] = children[2] = children[3] = null;

        hasChild = false;

        atlasPage?.Release();
        atlasPage = null;
    }
    
}



public class QuadTreeManager : MonoBehaviour
{
    public QuadTreeNode m_quadTree;
    
    private int QUADTREE_MAX_LEVEL;

    void Start()
    {
        NodePool.Instance.Initialize(1000);

        {
            m_quadTree = NodePool.Instance.NodeRequest();
            m_quadTree.bound.min = TerrainManager.TERRAIN_ORIGIN;
            m_quadTree.bound.max = TerrainManager.TERRAIN_END;
            m_quadTree.parent = null;
            m_quadTree.level = 0;

            Utils.ShowBoundLines(m_quadTree.bound, Color.green, Mathf.Infinity);
        }
        
        int terrainSize = (int)Mathf.Abs(TerrainManager.TERRAIN_ORIGIN.x - TerrainManager.TERRAIN_END.x);

        QUADTREE_MAX_LEVEL = terrainSize / TerrainManager.lowerestBlockSizeOnQuadTree;
        QUADTREE_MAX_LEVEL = (int)Mathf.Log(QUADTREE_MAX_LEVEL, 2f);
    }
    

    // <summary>
    // Rescale child's bound based in position. 
    // |0 = TL|   |1 = TR|   |2 = BL|  |3 = BR| 
    // </summary>
    private void UpdateBoundChild(Bounds bParent, ref Bounds bNode, int boundPosition)
    {
        switch (boundPosition)
        {
            case 0:
                bNode.min = new Vector3(bParent.min.x, 0, ((bParent.min.z + bParent.max.z) / 2f));
                bNode.max = new Vector3(((bParent.min.x + bParent.max.x) / 2f), 0, bParent.max.z);
                break;
            case 1:
                bNode.min = new Vector3(((bParent.min.x + bParent.max.x) / 2f), 0, ((bParent.min.z + bParent.max.z) / 2f));
                bNode.max = new Vector3(bParent.max.x, 0, bParent.max.z);
                break;
            case 2:
                bNode.min = new Vector3(bParent.min.x, 0, bParent.min.z);
                bNode.max = new Vector3(((bParent.min.x + bParent.max.x) / 2f), 0, ((bParent.min.z + bParent.max.z) / 2f));
                break;
            case 3:
                bNode.min = new Vector3(((bParent.min.x + bParent.max.x) / 2f), 0, bParent.min.z);
                bNode.max = new Vector3(bParent.max.x, 0, ((bParent.min.z + bParent.max.z) / 2f));
                break;
            default:
                break;
        }
    }



    /// <summary>
    /// Subdivide an node and set all parameters of new childs.
    /// Any node are created, only requested on NodePool.
    /// </summary>
    /// <param name="node"></param>
    private void SubdivideQuadTreeNode(QuadTreeNode node)
    {
        if (node.level > QUADTREE_MAX_LEVEL || NodePool.Instance.freeNodes < 4) return;

        Debug.Log("1.  " + NodePool.Instance.freeNodes);

        for (int i = 0; i < 4; i++)
        {
            node.children[i] = NodePool.Instance.NodeRequest();
            node.children[i].parent = node;
            node.children[i].level = node.level + 1;

            UpdateBoundChild(node.bound, ref node.children[i].bound, i);
        }

        node.hasChild = true;
        Debug.Log("2.  "+NodePool.Instance.freeNodes);
    }



    /// <summary>
    /// Release all nodes that arent visible.
    /// These nodes are re-added in nodePool
    /// </summary>
    /// <param name="node"></param>
    public void ReleaseNodes(QuadTreeNode node, bool clearAll = false)
    {
        if (node == null || !node.hasChild) return;

        for (int i = 0; i < 4; i++)
            ReleaseNodes(node.children[i], clearAll);

        if (clearAll || !node.isVisible)
            NodePool.Instance.NodeRelease(node);
    }

    
    /// <summary>
    /// Verify if has new visible nodes.
    /// If necessary subdivide and request distribution of vegetation.
    /// </summary>
    /// <param name="node"></param>
    public void SubdivideQuadTree(QuadTreeNode node)
    {
        if (node == null) return;

        if (node.isVisible && !node.hasChild)
            SubdivideQuadTreeNode(node);
        
        for (int i = 0; i < node.children.Length; i++)
            SubdivideQuadTree(node.children[i]);
    }

    
    private void Update()
    {
        if (m_quadTree == null) return;
        
        Profiler.BeginSample("QUADTREE");
        SubdivideQuadTree(m_quadTree);
        Profiler.EndSample();

        if (Time.frameCount % 180 == 0)
            ReleaseNodes(m_quadTree);

        if(Input.GetKeyDown(KeyCode.P))
            ReleaseNodes(m_quadTree, true);
    }


    private void ShowBounds(QuadTreeNode node)
    {
        if (node == null) return;

        Utils.ShowBoundLines(node.bound, Color.black);

        for(int i = 0; i < 4; i++)
            ShowBounds(node.children[i]);
    }


    private void OnDrawGizmos()
    {
        ShowBounds(m_quadTree);
    }

}
