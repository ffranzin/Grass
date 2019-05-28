
using System.Collections.Generic;
using UnityEngine;

public class Utils : MonoBehaviour {
    
    public static Vector3 RandomPosInsideBound(Bounds b, float boundBorder = 0)
    {
        return new Vector3( Random.Range(b.min.x + boundBorder, b.max.x - boundBorder), 0,
                            Random.Range(b.min.z + boundBorder, b.max.z - boundBorder));
    }


    /// <summary>
    /// Generate 'nPositions' inside bound 'b', considering 'minDist'. 
    /// </summary>
    public static List<Vector3> GenerateRandomPosInsideBound(Bounds b, int nPositions, float minDistBetweenPos, float distToBorder)
    {
        int attemps = 2 * nPositions;
        List<Vector3> positions = new List<Vector3>();

        while (positions.Count < nPositions && attemps > 0)
        {
            Vector3 pos = RandomPosInsideBound(b, distToBorder);

            if (!positions.Exists(p => (p - pos).magnitude < minDistBetweenPos))
                positions.Add(pos);

            attemps--;
        }

        return positions;
    }


    public static void ShowBoundLines(Bounds b, Color c, float duration = 0 )
    {
        float h = TerrainManager.TERRAIN_HEIGHT_MULTIPLIER;
        
        Debug.DrawLine(new Vector3(b.min.x, h, b.max.z), new Vector3(b.max.x, h, b.max.z), c, duration); // horizontal top
        Debug.DrawLine(new Vector3(b.max.x, h, b.min.z), new Vector3(b.min.x, h, b.min.z), c, duration); // horizontal bottom

        Debug.DrawLine(new Vector3(b.min.x, h, b.max.z), new Vector3(b.min.x, h, b.min.z), c, duration); // vertical left
        Debug.DrawLine(new Vector3(b.max.x, h, b.min.z), new Vector3(b.max.x, h, b.max.z), c, duration); // vertical right
    }

    
    public static float Remap(float org_val, float org_min, float org_max, float new_min, float new_max)
    {
        return new_min + Mathf.Clamp01(((org_val - org_min) / (org_max - org_min)) * (new_max - new_min));
    }

    public static float DistancePointToLine(Vector3 l0, Vector3 l1, Vector3 p)
    {
        l0.y = l1.y = p.y = 0;

        if ((l1 - l0).magnitude < (p - l1).magnitude || (l1 - l0).magnitude < (p - l0).magnitude)
            return Mathf.Infinity;

        return Vector3.Cross((l1 - l0).normalized, p - l0).magnitude;
    }
}
