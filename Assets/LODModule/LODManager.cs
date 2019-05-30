using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Linq;
using UnityEngine.Profiling;

public class LODManager : Singleton<LODManager>
{
    private ComputeShader compute;

    private int computeLOD16x16;
    private uint compute_gx, compute_gy, compute_gz;

    private ComputeBuffer allPositionsPagesDescBuffer;
    private ComputeBuffer allCollisionsPagesDescBuffer;

    public void Awake()
    {
        compute = Resources.Load<ComputeShader>("LOD_SelectionCompute");
        computeLOD16x16 = compute.FindKernel("ComputeLODs");

        compute.GetKernelThreadGroupSizes(computeLOD16x16, out compute_gx, out compute_gy, out compute_gz);
        
        ResizePagesBuffer(1);

        base.Awake();
    }


    public void CreateLODs(Atlas fullBuffer, List<Vector4> posBufferToRenderDesc, List<Vector4> colBufferToRenderDesc, List<ComputeBuffer> outputBuffer, List<ComputeBuffer> outputBufferRotated, ComputeBuffer LODDefinitions)
    {
        Profiler.BeginSample("Grass - LOD");

        if (posBufferToRenderDesc == null || posBufferToRenderDesc.Count == 0 ||
            outputBuffer == null || LODDefinitions == null) return;

        if (allPositionsPagesDescBuffer.count < posBufferToRenderDesc.Count)
            ResizePagesBuffer(posBufferToRenderDesc.Count);

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

        compute.SetTexture(computeLOD16x16, "_positionsBufferAtlas", fullBuffer.texture);
        compute.SetTexture(computeLOD16x16, "_collisionsBufferAtlas", ObjectCollisionManager.Instance.m_atlasCollisionAtlas.texture);
        
        allPositionsPagesDescBuffer.SetData(posBufferToRenderDesc);
        allCollisionsPagesDescBuffer.SetData(colBufferToRenderDesc);

        compute.SetBuffer(computeLOD16x16, "_allPositionsPagesDesc", allPositionsPagesDescBuffer);
        compute.SetBuffer(computeLOD16x16, "_allCollisionsPagesDesc", allCollisionsPagesDescBuffer);

        compute.SetInt("_validPagesDescCounter", allPositionsPagesDescBuffer.count);

        int size = fullBuffer.PageSize;

        compute.Dispatch(computeLOD16x16,   Mathf.CeilToInt(size / (float)compute_gx), 
                                            Mathf.CeilToInt(size / (float)compute_gy),
                                            Mathf.CeilToInt(posBufferToRenderDesc.Count / (float)compute_gz));
        Profiler.EndSample();
    }


    private void ResizePagesBuffer(int counter)
    {
        allPositionsPagesDescBuffer?.Release();
        allPositionsPagesDescBuffer = null;

        allCollisionsPagesDescBuffer?.Release();
        allCollisionsPagesDescBuffer = null;

        allPositionsPagesDescBuffer = new ComputeBuffer(counter, sizeof(float) * 4);
        allCollisionsPagesDescBuffer = new ComputeBuffer(counter, sizeof(float) * 4);
    }


    private void Update()
    {
#if UNITY_EDITOR
        compute.GetKernelThreadGroupSizes(computeLOD16x16, out compute_gx, out compute_gy, out compute_gz);
#endif
    }
}
