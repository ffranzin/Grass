using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;


//Some parameter that are passed from RAM to VRAM can be passed only one time, but i dont know why the Unity losses the references from the data passed. 
//Maybe is only on edit mode.

public class UpdateInfosOnShaders : Singleton<UpdateInfosOnShaders> {

    private List<Action> allCallbacks = new List<Action>();
    
    /// <summary>
    /// Add callback to run and execute once.
    /// </summary>
    public Action AddNewCallback
    {
        set
        {
            value();
            allCallbacks.Add(value);
        }
    }
    
	void Update () {
		if(Time.frameCount % 100 != 0)
            return;

        Profiler.BeginSample("Update shadders");
        foreach(Action a in allCallbacks)
        {
            try
            {
                a();
            }
            catch { }
        }
        Profiler.EndSample();
	}
}
