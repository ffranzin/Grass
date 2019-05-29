using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Linq;
using UnityEngine.Profiling;

public class LODManager : Singleton<LODManager>
{
    ComputeShader compute;

    int computeLOD16x16;
    uint compute_gx, compute_gy, compute_gz;

    ComputeBuffer allPagesDescBuffer;

    public void Awake()
    {
        compute = Resources.Load<ComputeShader>("LOD_SelectionCompute");
        computeLOD16x16 = compute.FindKernel("ComputeLODs");
        
        ResizePagesBuffer(1);

        base.Awake();

        compute.GetKernelThreadGroupSizes(computeLOD16x16, out compute_gx, out compute_gy, out compute_gz);
    }


    public void CreateLODs(List<Atlas.AtlasPageDescriptor> allBufferToRender, List<ComputeBuffer> outputBuffer, List<ComputeBuffer> outputBufferRotated, ComputeBuffer LODDefinitions)
    {
        Profiler.BeginSample("Grass - LOD");

        if (allBufferToRender == null || allBufferToRender.Count == 0 ||
            outputBuffer == null || LODDefinitions == null) return;

        if (allPagesDescBuffer.count < allBufferToRender.Count)
            ResizePagesBuffer(allBufferToRender.Count);

        outputBuffer.ToList().ForEach(c => c.SetCounterValue(0));
        outputBufferRotated.ToList().ForEach(c => c.SetCounterValue(0));
        
        compute.SetVector("_cameraPosition", Camera.main.transform.position);

        compute.SetBuffer(computeLOD16x16, "_outputPositionsBuffer_LOD0", outputBuffer[0]);
        compute.SetBuffer(computeLOD16x16, "_outputPositionsBuffer_LOD1", outputBuffer[1]);
        compute.SetBuffer(computeLOD16x16, "_outputPositionsBuffer_LOD2", outputBuffer[2]);

        compute.SetBuffer(computeLOD16x16, "_outputPositionsRotatedBuffer_LOD0", outputBufferRotated[0]);
        compute.SetBuffer(computeLOD16x16, "_outputPositionsRotatedBuffer_LOD1", outputBufferRotated[1]);
        compute.SetBuffer(computeLOD16x16, "_outputPositionsRotatedBuffer_LOD2", outputBufferRotated[2]);

        compute.SetBuffer(computeLOD16x16, "_LODRanges", LODDefinitions);

        compute.SetTexture(computeLOD16x16, "_positionsBufferAtlas", allBufferToRender[0].atlas.texture);
        
        allPagesDescBuffer.SetData(allBufferToRender.Select(t => t.tl_size).ToArray());

        compute.SetBuffer(computeLOD16x16, "_allPagesDesc", allPagesDescBuffer);

        compute.SetInt("_allPagesDescCounter", allPagesDescBuffer.count);

        compute.Dispatch(computeLOD16x16,   Mathf.CeilToInt(allBufferToRender[0].size / (float)compute_gx), 
                                            Mathf.CeilToInt(allBufferToRender[0].size / (float)compute_gy),
                                            Mathf.CeilToInt(allBufferToRender.Count   / (float)4));
        Profiler.EndSample();
    }


    private void ResizePagesBuffer(int counter)
    {
        allPagesDescBuffer?.Release();
        allPagesDescBuffer = null;

        allPagesDescBuffer = new ComputeBuffer(counter, sizeof(float) * 4);
    }


    private void Update()
    {
#if UNITY_EDITOR
        compute.GetKernelThreadGroupSizes(computeLOD16x16, out compute_gx, out compute_gy, out compute_gz);
#endif
    }
}
