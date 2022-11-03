using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer  
{
    private Camera _camera;
    private ScriptableRenderContext _context;
    private CommandBuffer _cmd = new CommandBuffer();
    private CullingResults _cullingResults;
    private readonly string sBufferName = "RenderCamera";
    private TinyRenderPipeline _pipeline;
    
    
    public void Render(TinyRenderPipeline pipeline, ScriptableRenderContext context, Camera camera)
    {
        Init(pipeline, context, camera);
        if (!Cull())
            return;
        
        Setup();
        
        DrawGeometryPass();
        LightingPass();
        DrawSkyBox();
        Submit();
    }

    void DrawSkyBox() 
    {
        _context.DrawSkybox(_camera);
    }


    void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_cmd);
        _cmd.Clear();
    }

    void Init(TinyRenderPipeline pipeline, ScriptableRenderContext context, Camera camera)
    {
        _pipeline = pipeline;
        _context = context;
        _camera = camera;
    }
    
    void Setup()
    {
        _cmd.name = sBufferName;
        _context.SetupCameraProperties(_camera);
        _cmd.ClearRenderTarget(true, true, Color.clear);
        _cmd.BeginSample(sBufferName);
        ExecuteBuffer();
    }
    
    void Submit() 
    {
        _cmd.EndSample(sBufferName);
        ExecuteBuffer();
        _context.Submit();
    }

    bool Cull()
    {
        if (_camera.TryGetCullingParameters(out var cullingParameters))
        {
            _cullingResults = _context.Cull(ref cullingParameters);
            return true;
        }
        return false;
    }

    void DrawGeometryPass()
    {
        _cmd.BeginSample("GeometryPass");
        {
            
        }
        RenderTargetIdentifier identifier = _pipeline._depthMap;
        _cmd.SetRenderTarget(_pipeline.gBufferIDs, _pipeline._depthMap);
        _cmd.ClearRenderTarget(true, true, Color.clear);
        ShaderTagId shaderTagId = new ShaderTagId("GeometryPass");   
        SortingSettings sortingSettings = new SortingSettings(_camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        
        _cmd.EndSample("GeometryPass");
    }

    void LightingPass()
    {
        // int kernelIndex = _lightingPassShader.FindKernel("LightingPassCS");
        // mat.SetTexture("gBuffer0", _pipeline._gBufferMaps[0]);
        // mat.SetTexture("gBuffer1", _pipeline._gBufferMaps[0]);
        // mat.SetTexture("gBuffer2", _pipeline._gBufferMaps[0]);
        // mat.SetTexture("gOutputMap", _camera.targetTexture);
        // _cmd.DispatchCompute();
        // Debug.Log("111");
    }
}
