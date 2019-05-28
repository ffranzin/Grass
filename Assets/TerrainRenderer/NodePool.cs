using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

public class NodePool : Singleton<NodePool>
{
    public int nodePoolCapacity { get; private set; }

    private List<QuadTreeNode> qt_NodePool;

    public bool hasFreeNodes
    {
        get
        {
            return qt_NodePool.Count > 0;
        }
    }

    public bool isFull
    {
        get
        {
            return qt_NodePool.Count == 0;
        }
    }

    public int freeNodes
    {
        get
        {
            return qt_NodePool.Count;
        }
    }


    public void Initialize(uint size)
    {
        if (nodePoolCapacity > 0)
        {
            Debug.LogError("NodePool already initialized, you need to use Resize method.");
            return;
        }

        Resize((int)size);
    }


    public void Resize(int counter)
    {
        qt_NodePool = qt_NodePool ?? new List<QuadTreeNode>();

        if (counter > 0)
        {
            for (int i = 0; i < counter; i++)
                qt_NodePool.Add(new QuadTreeNode());
        }
        else
        {
            qt_NodePool.RemoveRange(0, Mathf.Min(-1 * counter, freeNodes));
        }

        nodePoolCapacity += counter;
    }


    /// <summary>
    /// Return pre-allocated node.
    /// </summary>
    public QuadTreeNode NodeRequest()
    {
        if (!hasFreeNodes) return null;

        QuadTreeNode aux = qt_NodePool[0];

        qt_NodePool.RemoveAt(0);

        return aux;
    }

    /// <summary>
    /// Reset all parameters of node.
    /// Release atlas node.
    /// </summary>
    public void NodeRelease(QuadTreeNode node)
    {
        if (node == null) return;

        for (int i = 0; i < 4; i++)
        {
            node.Reset();
            qt_NodePool.Add(node.children[i]);
        }

        node.hasChild = false;
    }
}
