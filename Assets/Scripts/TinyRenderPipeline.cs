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
    public TinyRenderPipelineAsset pipelineSettings;

    public TinyRenderPipeline(TinyRenderPipelineAsset asset) {
        pipelineSettings = asset;
        _cameraRenderers = new Dictionary<string, CameraRenderer>();
        Shader.SetGlobalTexture("gBrdfLutMap", pipelineSettings.brdfLutMap);
        Shader.SetGlobalTexture("gIrradianceMap", pipelineSettings.irradianceMap);
        Shader.SetGlobalTexture("gPrefilterMap", pipelineSettings.prefilterMap);
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        foreach (Camera c in cameras) {
            _cameraRenderers.TryGetValue(c.name, out var cameraRenderer);
            if (cameraRenderer == null) {
                cameraRenderer = new CameraRenderer(this);
                cameraRenderer.Init(context);
                _cameraRenderers.Add(c.name, cameraRenderer);
            }
            cameraRenderer.Render(context, c);
        }
    }
}
