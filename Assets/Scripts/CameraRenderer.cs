using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer  
{
    private Camera _camera;
    private ScriptableRenderContext _context;
    private CommandBuffer _commandBuffer = new CommandBuffer();
    private CullingResults _cullingResults;
    private readonly string sBufferName = "RenderCamera";
    
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        Init(context, camera);
        if (!Cull())
            return;
        
        Setup();
        
        DrawGeometryPass();
        DrawVisibleGeometry();
        
        Submit();
    }

    void DrawVisibleGeometry() 
    {
        _context.DrawSkybox(_camera);
    }


    void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    void Init(ScriptableRenderContext context, Camera camera)
    {
        _context = context;
        _camera = camera;
    }
    
    void Setup()
    {
        _commandBuffer.name = sBufferName;
        _context.SetupCameraProperties(_camera);
        _commandBuffer.ClearRenderTarget(true, true, Color.clear);
        _commandBuffer.BeginSample(sBufferName);
        ExecuteBuffer();
    }
    
    void Submit() 
    {
        _commandBuffer.EndSample(sBufferName);
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
        ShaderTagId shaderTagId = new ShaderTagId("GeometryPass");   
        SortingSettings sortingSettings = new SortingSettings(_camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
    }
}
