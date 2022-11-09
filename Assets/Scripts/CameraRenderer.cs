using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer {
    private ScriptableRenderContext _context;

    private List<IPass> _passes;
    private LightingPass _lightingPass;

    public TinyRenderPipeline pipeline;
    public Camera camera;
    public int width = -1;
    public int height = -1;

    private static readonly int kGBufferCount = 3;
    
    public RenderTexture depthMap;
    public RenderTexture[] gBufferMaps = new RenderTexture[kGBufferCount];
    public RenderTexture screenMap;
    public RenderTargetIdentifier[] gBufferID = new RenderTargetIdentifier[kGBufferCount];
    public RenderTargetIdentifier depthMapID;
    public RenderTargetIdentifier screenMapID;

    public Matrix4x4 matInvViewProj;

    public CameraRenderer(TinyRenderPipeline pipeline) {
        this.pipeline = pipeline;
        _passes = new List<IPass>();
        _lightingPass = new LightingPass(this);
        _passes.Add(_lightingPass);
    }

    ~CameraRenderer() {
        Clear();
    }

    private void Clear() {
        RenderTexture.ReleaseTemporary(depthMap);
        RenderTexture.ReleaseTemporary(screenMap);
        RenderTexture.ReleaseTemporary(gBufferMaps[0]);
        RenderTexture.ReleaseTemporary(gBufferMaps[1]);
        RenderTexture.ReleaseTemporary(gBufferMaps[2]);
    }

    public void Init(ScriptableRenderContext context) {
        foreach (var pass in _passes)
            pass.Init(context);
    }
    
    private void Resize(ScriptableRenderContext context, int w, int h) {
        width = w;
        height = h;
        
        Clear();
        
        depthMap = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        screenMap = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        gBufferMaps[0] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gBufferMaps[1] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        gBufferMaps[2] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        for (int i = 0; i < kGBufferCount; ++i) {
            gBufferMaps[i].name = $"gBuffer{i}";  
            gBufferID[i] = gBufferMaps[i];
        }

        depthMap.name = "DepthMap";
        screenMap.name = "ScreenMap";
        
        depthMapID = depthMap;
        screenMapID = screenMap;
        
        foreach (var pass in _passes)
            pass.Resize(context, width, height);
    }

    public void Render(ScriptableRenderContext context, Camera c) {
        SetupRender(context, c);
        GeometryPass();
        LightingPass();
        DrawSkyBox();
        Blit();
    }

    public void ExecuteAndClearCmd(CommandBuffer cmd) {
        _context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }
    
    private void SetupRender(ScriptableRenderContext context, Camera c) {
        _context = context;
        camera = c;
        
        Matrix4x4 matProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = matProj * camera.worldToCameraMatrix;
        matInvViewProj = vpMatrix.inverse;
        
        if (width != camera.pixelWidth || height != camera.pixelHeight)
            Resize(context, camera.pixelWidth, camera.pixelHeight);
    }

    private void GeometryPass() {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "GeometryPass";
        
        _context.SetupCameraProperties(camera);
        cmd.SetRenderTarget(gBufferID, depthMapID);
        cmd.SetViewport(new Rect(0f, 0f, width, height));
        cmd.ClearRenderTarget(true, true, Color.clear);
        ExecuteAndClearCmd(cmd);

        camera.TryGetCullingParameters(out ScriptableCullingParameters p);
        CullingResults cullingResults = _context.Cull(ref p);

        ShaderTagId shaderTagId = new ShaderTagId("GeometryPass");
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        _context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        ExecuteAndClearCmd(cmd);

        _context.Submit();
    }

    private void LightingPass() {
        _lightingPass.Execute(_context);
    }

    private void DrawSkyBox() {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "DrawSkyBox";
        cmd.SetRenderTarget(screenMapID, depthMapID);
        ExecuteAndClearCmd(cmd);
        _context.DrawSkybox(camera);
        ExecuteAndClearCmd(cmd);
        _context.Submit();
    }

    private void Blit() {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "Blit";
        cmd.Blit(screenMap, BuiltinRenderTextureType.CameraTarget);
        ExecuteAndClearCmd(cmd);
        _context.Submit();
    }
}
