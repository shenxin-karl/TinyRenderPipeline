using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class TinyRenderPipeline : RenderPipeline {
    private Dictionary<string, CameraRenderer> _cameraRenderers;
    private GenerateIBL _generateIbl;
    public TinyRenderPipelineAsset pipelineSettings;
    
    public TinyRenderPipeline(TinyRenderPipelineAsset asset) {
        pipelineSettings = asset;
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
