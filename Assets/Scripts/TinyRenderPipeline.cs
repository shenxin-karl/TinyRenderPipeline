using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class TinyRenderPipeline : RenderPipeline
{
    private static readonly int kGBufferCount = 3; 
    public RenderTexture _depthMap;
    public CameraRenderer cameraRenderer;
    public RenderTexture[] gBufferMaps = new RenderTexture[kGBufferCount];
    public RenderTargetIdentifier[] gBufferIDs = new RenderTargetIdentifier[kGBufferCount];

    public ComputeShader lightingPassShader;

    public TinyRenderPipeline()
    {
        cameraRenderer = new CameraRenderer();
        _depthMap = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gBufferMaps[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gBufferMaps[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gBufferMaps[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        for (int i = 0; i < kGBufferCount; ++i)
            gBufferIDs[i] = gBufferMaps[i];
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)  
    {
        foreach (Camera c in cameras) 
            cameraRenderer.Render(this, context, c);
    }
}
