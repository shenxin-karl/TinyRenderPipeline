using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer {
    private ScriptableRenderContext _context;
    private TinyRenderPipeline _pipeline;
    private LightingPass _lightingPass;

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

    public CameraRenderer() {
        _lightingPass = new LightingPass();
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
    
    private void Resize(int width, int height) {
        width = Math.Max(width, 1);
        height = Math.Max(height, 1);

        Clear();
        depthMap = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        screenMap = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        gBufferMaps[0] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gBufferMaps[1] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gBufferMaps[2] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        for (int i = 0; i < kGBufferCount; ++i) {
            gBufferMaps[i].name = $"gBuffer{i}";  
            gBufferID[i] = gBufferMaps[i];
        }

        depthMap.name = "DepthMap";
        screenMap.name = "ScreenMap";
        
        depthMapID = depthMap;
        screenMapID = screenMap;

        screenMap.enableRandomWrite = true;
    }

    public void Render(TinyRenderPipeline pipeline, ScriptableRenderContext context, Camera camera) {
        Init(pipeline, context, camera);
        GeometryPass();
        LightingPass();
        DrawSkyBox();
        Blit();
    }

    public void ExecuteAndClearCmd(CommandBuffer cmd) {
        _context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }
    
    private void Init(TinyRenderPipeline pipeline, ScriptableRenderContext context, Camera camera) {
        _pipeline = pipeline;
        _context = context;
        this.camera = camera;

        Matrix4x4 matView = camera.worldToCameraMatrix;
        Matrix4x4 matProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 matViewProj = matProj * matView;
        matInvViewProj = matViewProj.inverse;
        
        if (width != camera.pixelWidth || height != camera.pixelHeight) {
            Resize(camera.pixelWidth, camera.pixelHeight);
            width = camera.pixelWidth;
            height = camera.pixelHeight;
        }
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
        _lightingPass.Execute(this, _context);
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
