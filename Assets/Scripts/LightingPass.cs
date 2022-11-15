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

    public LightingPass(CameraRenderer cameraRenderer) : base(cameraRenderer) {
        _material = new Material(Shader.Find("Unlit/LightingPassPS"));
    }

    public override void Init(ScriptableRenderContext context) {
        var settings = _cameraRenderer.pipeline.pipelineSettings;

        if (settings.brdfLutMap != null) {
            _material.SetTexture("gBrdfLutMap", settings.brdfLutMap);
            if (settings.irradianceMap != null)
                _material.EnableKeyword("_ENABLE_IBL_DIFFUSE");
            if (settings.prefilterMap != null)
                _material.EnableKeyword("_ENABLE_IBL_SPECULAR");
        }
    }
    
    public override void ExecutePass(ScriptableRenderContext context) {
        _material.SetTexture(GBuffer0, _cameraRenderer.gBufferMaps[0]);
        _material.SetTexture(GBuffer1, _cameraRenderer.gBufferMaps[1]);
        _material.SetTexture(GBuffer2, _cameraRenderer.gBufferMaps[2]);
        _material.SetTexture(GDepthMap, _cameraRenderer.depthMap);
        _material.SetMatrix(GMatInvViewProj, _cameraRenderer.matInvViewProj);
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "LightingPass";
        cmd.SetViewport(new Rect(0, 0, _cameraRenderer.width, _cameraRenderer.height));
        cmd.Blit(BuiltinRenderTextureType.None, _cameraRenderer.screenMap, _material);
        _cameraRenderer.ExecuteAndClearCmd(cmd);
        context.Submit();
    }
}
