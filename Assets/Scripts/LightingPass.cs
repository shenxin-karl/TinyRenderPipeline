using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer.Internal.Converters;
using UnityEngine;
using UnityEngine.Rendering;

public class LightingPass : IPass {
    private ComputeShader _shader;
    private int _kernelIndex;
    
    private Material _material;
    private static readonly int GBuffer2 = Shader.PropertyToID("gBuffer2");
    private static readonly int GBuffer1 = Shader.PropertyToID("gBuffer1");
    private static readonly int GBuffer0 = Shader.PropertyToID("gBuffer0");
    private static readonly int GDepthMap = Shader.PropertyToID("gDepthMap");

    private static readonly int GMatInvViewProj = Shader.PropertyToID("gMatInvViewProj");
    private static readonly int GViewPortRay = Shader.PropertyToID("gViewPortRay");
    
    public LightingPass(CameraRenderer cameraRenderer) : base(cameraRenderer) {
        _material = new Material(Resources.Load<Shader>("Shaders/LightingPassPS"));
    }

    public override void Init(ScriptableRenderContext context) {
        _material.SetTexture(GBuffer0, _cameraRenderer.gBufferMaps[0]);
        _material.SetTexture(GBuffer1, _cameraRenderer.gBufferMaps[1]);
        _material.SetTexture(GBuffer2, _cameraRenderer.gBufferMaps[2]);
        _material.SetTexture(GDepthMap, _cameraRenderer.depthMap);

        if (_cameraRenderer.pipeline.generateIbl != null) {
            var diffuseSHCoefs = _cameraRenderer.pipeline.generateIbl.DiffuseSH3.toVector4Array();
            _material.SetVectorArray("gAmbientDiffuseSH", diffuseSHCoefs);
        }
    }
    
    public void Execute(ScriptableRenderContext context) {
        ExecuteGraphicsShader(_cameraRenderer, context);
    }
    
    public void ExecuteGraphicsShader(CameraRenderer cameraRenderer, ScriptableRenderContext context) {
        _material.SetMatrix(GMatInvViewProj, cameraRenderer.matInvViewProj);
        
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "LightingPass";
        cmd.Blit(BuiltinRenderTextureType.None, cameraRenderer.screenMap, _material);
        cameraRenderer.ExecuteAndClearCmd(cmd);
        context.Submit();
    }
}
