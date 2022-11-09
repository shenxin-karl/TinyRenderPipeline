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
    private bool _isInit = false;
    public GenerateIBL generateIbl;
    public TinyRenderPipelineAsset pipelineSettings;


    public TinyRenderPipeline(TinyRenderPipelineAsset asset) {
        pipelineSettings = asset;
        _cameraRenderers = new Dictionary<string, CameraRenderer>();
        
        if (pipelineSettings.skyboxCubeMap != null)
            generateIbl = new GenerateIBL(pipelineSettings.skyboxCubeMap);
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        TryInit(context);
        
        foreach (Camera c in cameras) {
            _cameraRenderers.TryGetValue(c.name, out var cameraRenderer);
            if (cameraRenderer == null) {
                cameraRenderer = new CameraRenderer(this);
                cameraRenderer.Init(context);
                _cameraRenderers.Add(c.name, cameraRenderer);
            }
            cameraRenderer.Render(context, c);
        }
        // generateIbl.DebugDirection();
    }

    private void TryInit(ScriptableRenderContext context) {
        if (_isInit)
            return;

        _isInit = true;
        generateIbl.Generate(context);
    }
}
