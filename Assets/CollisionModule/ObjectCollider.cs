using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCollider : MonoBehaviour
{
    List<GrassHashCell> cells = new List<GrassHashCell>();

    void Update()
    {
        cells.Clear();
        GrassHashManager.Instance.GetCellsAroundPos(transform.position, 20, ref cells);
    }
}
