﻿using UnityEngine;
using UnityEngine.Profiling;

public class PreComputePositions : Singleton<PreComputePositions>
{
    private ComputeShader compute;

    private int kernelIndex;
    
    void Awake()
    {
        compute = Resources.Load<ComputeShader>("PreComputePositions");
        kernelIndex = compute.FindKernel("ComputePositions");

        base.Awake();
    }
    

    public void ComputePositions(Atlas.AtlasPageDescriptor bufferDesc, GrassHashCell cell, GrassConfig config)
    {
        Profiler.BeginSample("Grass - Distribution");
        Bounds b = cell.boundsWorld;

        float gridDim1D = b.size.x * config.distributionDensity;
        float gridDim2D = gridDim1D * gridDim1D;
        float gridCellSize = b.size.x / gridDim1D;

        compute.SetVector("_cellDesc", new Vector4(b.min.x, b.min.z, b.size.x, b.size.z));
        compute.SetVector("_gridDim", new Vector4(gridDim1D, gridCellSize, 0, 0));
        
        compute.SetTexture(kernelIndex, "_positionsBufferAtlas", bufferDesc.atlas.texture);
        compute.SetVector("_positionsBufferDesc", bufferDesc.tl_size);
        
        compute.SetInt("_distribuitionSeed", cell.seed * config.seed);

        compute.SetFloat("frequency", config.frequency);
        compute.SetFloat("gain", config.gain);
        compute.SetFloat("lacunarity", config.lacunarity);
        compute.SetFloat("amplitude", config.amplitude);
        compute.SetInt("octaves", config.octaves);
        
        uint gtx, gty, gtz;

        compute.GetKernelThreadGroupSizes(kernelIndex, out  gtx, out  gty, out  gtz);

        int gx = Mathf.CeilToInt(gridDim1D / gtx);
        int gy = Mathf.CeilToInt(gridDim1D / gty);

        compute.Dispatch(kernelIndex, gx, gy, 1);
        Profiler.EndSample();
    }
}
