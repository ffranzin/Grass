
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;

public class ObjectRendererManager : Singleton<ObjectRendererManager>
{
    public string objectRendererBundlePath = "";

    public ObjetcRendererIndirect[] objectRenderers;

    private List<GrassHashCell> selectedCells = new List<GrassHashCell>();

    public static bool IS_GRASS_ACTIVE = true;

    private static float maxViewRange = -1;

    private static Plane[] frustumPlanes;

    private void Start()
    {
        objectRenderers = FindObjectsOfType<ObjetcRendererIndirect>();
        if (objectRenderers == null || objectRenderers.Length == 0)
        {
            IS_GRASS_ACTIVE = false;
            Debug.LogError("Couldn't render grass : Missing Component.");
            return;
        }

        for (int i = 0; i < objectRenderers.Length; i++)
        {
            objectRenderers[i].Initialize();
            objectRenderers[i].rendererID = i;
            maxViewRange = Mathf.Max(maxViewRange, objectRenderers[i].m_config.distanceViewRange);
        }

        frustumPlanes = new Plane[6];
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.G))
            IS_GRASS_ACTIVE = !IS_GRASS_ACTIVE;

        if (!IS_GRASS_ACTIVE) return;

        SelectCellsToRender();
        
        for (int i = 0; i < objectRenderers.Length; i++)
            objectRenderers[i].RenderCells();
        
    }



    /// <summary>
    /// Select all cells around the camera position based on biggest distance from all objectRenderer.
    /// After the selection, for each cell are verified the distances from the camera AND if is inside the 'viewDistance' of objectRenderer.
    /// If true, it cells is added in a list on objectRenderer to be render.
    /// </summary>
    private void SelectCellsToRender()
    {
        selectedCells.Clear();

        Profiler.BeginSample("Grass - Cells selection");
        GrassHashManager.Instance.GetCellsAroundPos(Camera.main.transform.position, maxViewRange, ref selectedCells);
        Profiler.EndSample();

        GeometryUtility.CalculateFrustumPlanes(Camera.main, frustumPlanes);

        foreach (GrassHashCell cell in selectedCells)
        {
            if (!GeometryUtility.TestPlanesAABB(frustumPlanes, cell.boundsWorld))
                continue;

            foreach (ObjetcRendererIndirect gr in objectRenderers)
            {
                if (cell.minDistToCamera < gr.m_config.distanceViewRange)
                    gr.PrepareCellToRender(cell);
            }
        }
    }
}

