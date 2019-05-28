using System.Collections.Generic;
using UnityEngine;


/*
This class is a simple compute buffers manager. In the beginning the manager initialize some instances [poolSize] of the compute buffer
with the [type], size [bufferCount] and data size [bufferStride]. Classes that use this pool can only get a buffer, that was previoslly initialized.

If a pool is empty, new buffers is initialized.

The buffers can be released and reinserted on the pool. If, during the reinsertion, the pool is already full the buffer is destroyed and isnt reinserted.
*/


public class ComputeBufferPool
{
    private static float info_allMemoryUsageByAllPools;

    private int bufferCount;
    private int bufferStride;
    private int poolSize;
    private ComputeBufferType type;

    private List<ComputeBuffer> buffers = new List<ComputeBuffer>();

    public bool IsEmpty { get { return buffers.Count == poolSize; } }

    public ComputeBufferPool(int poolSize, int bufferCount, int bufferStride, ComputeBufferType type = ComputeBufferType.Default)
    {
        this.poolSize = poolSize;
        this.bufferCount = bufferCount;
        this.bufferStride = bufferStride;
        this.type = type;
        
        buffers = new List<ComputeBuffer>();

        CreateNewBuffers(poolSize);


#if UNITY_EDITOR
        float memory = (bufferCount * bufferStride) * poolSize;

        memory /= (8f * 1024f * 1024f);

        info_allMemoryUsageByAllPools += memory;

        Debug.Log("Node Pool Memory : " + memory.ToString("0.00") + "MB" +
                   " -- All Memory : " + info_allMemoryUsageByAllPools.ToString("0.00") + "MB");
#endif
    }

    public ComputeBuffer GetBuffer()
    {
        if (buffers.Count == 0)
                return new ComputeBuffer(bufferCount, bufferStride, type);

        ComputeBuffer buf = buffers[0];

        buffers.RemoveAt(0);

        return buf;
    }

    public void ReleaseBuffer(ComputeBuffer b)
    {
        if (b == null) return;
        
        if (buffers.Count >= poolSize)
        {
            b.Release();
            b = null;
            return;
        }
        
        buffers.Add(b);
    }

    public void CreateNewBuffers(int count)
    {
        for (int i = 0; i < count; i++)
            buffers.Add(new ComputeBuffer(bufferCount, bufferStride, type));
    }

    public void Release()
    {
        for (int i = 0; i < buffers.Count; i++)
        {
            buffers[i]?.Dispose();
        }

        buffers.Clear();
        buffers = null;
    }

    //~ComputeBufferPool()    
    //{
    //    return;
    //    for (int i = 0; i < buffers.Count; i++)
    //    {
    //        if (buffers[i] != null)
    //        {
    //            buffers[i].Dispose();
    //            buffers[i] = null;
    //        }
    //    }
    //    buffers.Clear();
    //    buffers = null;
    //}
}
