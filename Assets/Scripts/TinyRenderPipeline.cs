using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class TinyRenderPipeline : RenderPipeline  {
    private CameraRenderer _cameraRenderer = new CameraRenderer();

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)  {
        foreach (Camera c in cameras) 
            _cameraRenderer.Render(context, c);
    }
}
