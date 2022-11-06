using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class TinyRenderPipeline : RenderPipeline
{
    private Dictionary<string, CameraRenderer> _cameraRenderers;

    public TinyRenderPipeline() {
        _cameraRenderers = new Dictionary<string, CameraRenderer>();
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        foreach (Camera c in cameras) {
            _cameraRenderers.TryGetValue(c.name, out var cameraRenderer);
            if (cameraRenderer == null) {
                cameraRenderer = new CameraRenderer();
                _cameraRenderers.Add(c.name, cameraRenderer);
            }
            cameraRenderer.Render(this, context, c);
        }
    }
}
