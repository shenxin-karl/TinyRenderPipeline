using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class CameraRenderer {
    private ScriptableRenderContext _context;

    private List<IPass> _passes;
    private LightingPass _lightingPass;
    private PostProcessingPass _postProcessingPass;
    private TemporalAAPass _temporalAAPass;

    public TinyRenderPipeline pipeline;
    public Camera camera;
    public int width = -1;
    public int height = -1;

    private static readonly int kGBufferCount = 4;
    
    public RenderTexture depthMap;
    public RenderTexture[] gBufferMaps = new RenderTexture[kGBufferCount];
    public RenderTexture screenMap;
    public RenderTargetIdentifier[] gBufferID = new RenderTargetIdentifier[kGBufferCount];
    public RenderTargetIdentifier depthMapID;
    public RenderTargetIdentifier screenMapID;

    public Matrix4x4 matInvViewProj;
    public Matrix4x4 currFrameWorldToViewport;
    public Matrix4x4 prevFrameWorldToViewport;

    public CameraRenderer(TinyRenderPipeline pipeline) {
        this.pipeline = pipeline;
        _passes = new List<IPass>();
        _lightingPass = new LightingPass(this);
        _passes.Add(_lightingPass);

        _postProcessingPass = new PostProcessingPass(this);
        _passes.Add(_postProcessingPass);

        _temporalAAPass = new TemporalAAPass(this);
        _passes.Add(_temporalAAPass);
    }

    ~CameraRenderer() {
        Clear();
    }

    private void Clear() {
        RenderTexture.ReleaseTemporary(depthMap);
        if (screenMap != null)
            screenMap.Release();
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
        gBufferMaps[0] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gBufferMaps[1] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        gBufferMaps[2] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gBufferMaps[3] = RenderTexture.GetTemporary(width, height, 0, GraphicsFormat.A2B10G10R10_UNormPack32);
        
        screenMap = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
        screenMap.enableRandomWrite = true;
        screenMap.Create();
        
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
        TemporalAAPass();
        PostProcessingPass();
        
        bool isEditor = Handles.ShouldRenderGizmos();
        if (isEditor && camera.cameraType == CameraType.SceneView) {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
        
        Blit();
    }

    public void ExecuteAndClearCmd(CommandBuffer cmd) {
        _context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }
    
    private void SetupRender(ScriptableRenderContext context, Camera c) {
        _context = context;
        camera = c;
        if (width != camera.pixelWidth || height != camera.pixelHeight)
            Resize(context, camera.pixelWidth, camera.pixelHeight);
        
        Matrix4x4 matProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        var worldToCameraMatrix = camera.worldToCameraMatrix;
        Matrix4x4 vpMatrix = matProj * worldToCameraMatrix;
        matInvViewProj = vpMatrix.inverse;

        Matrix4x4 matViewProj = matProj * worldToCameraMatrix;

        Vector3 half = new Vector3(0.5f, 0.5f, 0f);
        Matrix4x4 toTextureSpace = Matrix4x4.Translate(half) * Matrix4x4.Scale(half);
        Matrix4x4 toViewport = Matrix4x4.Scale(new Vector3(width, height, 1f));
        Matrix4x4 clipToViewport = toViewport * toTextureSpace;
        
        prevFrameWorldToViewport = currFrameWorldToViewport;
        currFrameWorldToViewport = clipToViewport * matProj * worldToCameraMatrix;
        Shader.SetGlobalMatrix("gMatViewProj", matViewProj);
        
        if (camera.cameraType == CameraType.SceneView) {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    private void GeometryPass() {
        CommandBuffer cmd = new CommandBuffer();
        cmd.BeginSample("GeometryPass");
        _context.SetupCameraProperties(camera);
        
        Vector2 jitter = _temporalAAPass.Jitter;
        cmd.SetRenderTarget(gBufferID, depthMapID);
        
        if (camera.cameraType == CameraType.SceneView)
            cmd.SetViewport(new Rect(jitter.x, jitter.y, width, height));
        else 
            cmd.SetViewport(new Rect(0, 0, width, height));
        
        cmd.ClearRenderTarget(true, true, Color.clear);
        ExecuteAndClearCmd(cmd);

        camera.TryGetCullingParameters(out ScriptableCullingParameters p);
        CullingResults cullingResults = _context.Cull(ref p);

        ShaderTagId shaderTagId = new ShaderTagId("GeometryPass");
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        _context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        cmd.EndSample("GeometryPass");
        ExecuteAndClearCmd(cmd);

        _context.Submit();
    }

    private void LightingPass() {
        _lightingPass.ExecutePass(_context);
    }

    private void TemporalAAPass() {
        _temporalAAPass.ExecutePass(_context);
    }
    
    private void PostProcessingPass() {
        _postProcessingPass.ExecutePass(_context);
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
