using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

public class LODManager : Singleton<LODManager>
{
    ComputeShader compute;

    int computeLOD1024;
    uint computeLOD1024_gx, computeLOD1024_gy, computeLOD1024_gz;


    public void Awake()
    {
        compute = Resources.Load<ComputeShader>("LOD_SelectionCompute");
        computeLOD1024 = compute.FindKernel("ComputeLODs");

        base.Awake();
    }

    [StructLayout(LayoutKind.Sequential)]
    struct teste
    {
        //public ComputeBuffer[] inputBuffer;
        public int[] inputBuffer1;
    };



    teste buffers;

    public void CreateLODs1(List<ComputeBuffer> allBufferToRender, ComputeBuffer[] outputBuffer, ComputeBuffer LODDefinitions)
    {
        int kernel = compute.FindKernel("ComputeLODs1");

        buffers = new teste();

        //buffers.inputBuffer = new ComputeBuffer[16];
        buffers.inputBuffer1 = new int[16];

        outputBuffer[0].SetCounterValue(0);
        outputBuffer[1].SetCounterValue(0);
        outputBuffer[2].SetCounterValue(0);
        
        compute.SetBuffer(kernel, "outputPositionsBuffer_LOD0", outputBuffer[0]);
        compute.SetBuffer(kernel, "outputPositionsBuffer_LOD1", outputBuffer[1]);
        compute.SetBuffer(kernel, "outputPositionsBuffer_LOD2", outputBuffer[2]);

        compute.SetBuffer(kernel, "LODRanges", LODDefinitions);

        int counter = 0;
        
        for(int i = 0; i < allBufferToRender.Count; i++)
        {
            if (counter == 16)
            {
                counter = 0;

                ComputeBuffer teste1 = new ComputeBuffer(1, Marshal.SizeOf(typeof(teste)));
                teste1.SetData(new teste[1] { buffers });

                //compute.SetBuffer(kernel, "LOD_ToCompute", teste);

                //compute.Dispatch(kernel, Mathf.CeilToInt(allBufferToRender[i].count / 128), 1, 1);
                return;
            }
            buffers.inputBuffer1[counter] = 1;
            counter ++;
        }
        
    }





    public void CreateLODs(List<ComputeBuffer> allBufferToRender, ComputeBuffer[] outputBuffer, ComputeBuffer[] outputRotatedBuffer, ComputeBuffer LODDefinitions)
    {
       // CreateLODs1(allBufferToRender, outputBuffer, LODDefinitions);
       // return;

        Profiler.BeginSample("Grass - LOD");
        outputBuffer[0].SetCounterValue(0);
        outputBuffer[1].SetCounterValue(0);
        outputBuffer[2].SetCounterValue(0);
        
        compute.SetVector("_cameraPosition", Camera.main.transform.position);

        compute.SetBuffer(computeLOD1024, "outputPositionsBuffer_LOD0", outputBuffer[0]);
        compute.SetBuffer(computeLOD1024, "outputPositionsBuffer_LOD1", outputBuffer[1]);
        compute.SetBuffer(computeLOD1024, "outputPositionsBuffer_LOD2", outputBuffer[2]);
        
        compute.SetBuffer(computeLOD1024, "LODRanges", LODDefinitions);

        for (int i = 0; i < allBufferToRender.Count; i++)
        {
            compute.SetBuffer(computeLOD1024, "inputBuffer", allBufferToRender[i]);
            
            compute.Dispatch(computeLOD1024, Mathf.CeilToInt(allBufferToRender[i].count / 128), 1, 1);
        }

        Profiler.EndSample();
    }
    
    private void Update()
    {
#if UNITY_EDITOR
        compute.GetKernelThreadGroupSizes(computeLOD1024, out computeLOD1024_gx, out computeLOD1024_gy, out computeLOD1024_gz);

        computeLOD1024_gx = computeLOD1024_gx == 0 ? 1 : computeLOD1024_gx;
        computeLOD1024_gy = computeLOD1024_gy == 0 ? 1 : computeLOD1024_gy;
        computeLOD1024_gz = computeLOD1024_gz == 0 ? 1 : computeLOD1024_gz;
#endif
    }

}
