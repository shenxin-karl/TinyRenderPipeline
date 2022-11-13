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
    private static readonly int GAmbientDiffuseSH = Shader.PropertyToID("gAmbientDiffuseSH");

    public LightingPass(CameraRenderer cameraRenderer) : base(cameraRenderer) {
        _material = new Material(Shader.Find("Unlit/LightingPassPS"));
    }

    public override void Init(ScriptableRenderContext context) {
        var settings = _cameraRenderer.pipeline.pipelineSettings;

        if (settings.brdfLutMap != null) {
            _material.SetTexture("gBrdfLutMap", settings.brdfLutMap);
            if (settings.irradianceMap != null) {
                _material.EnableKeyword("_ENABLE_IBL_DIFFUSE");
                _material.SetTexture("gIrradianceMap", settings.irradianceMap);
            }

            if (settings.prefilterMap != null) {
                _material.EnableKeyword("_ENABLE_IBL_SPECULAR");
                _material.SetTexture("gPrefilterMap", settings.prefilterMap);
            }
        }
        
            
    }
    
    public override void ExecutePass(ScriptableRenderContext context) {
        _material.SetTexture(GBuffer0, _cameraRenderer.gBufferMaps[0]);
        _material.SetTexture(GBuffer1, _cameraRenderer.gBufferMaps[1]);
        _material.SetTexture(GBuffer2, _cameraRenderer.gBufferMaps[2]);
        _material.SetTexture(GDepthMap, _cameraRenderer.depthMap);
        _material.SetMatrix(GMatInvViewProj, _cameraRenderer.matInvViewProj);
        
        if (_cameraRenderer.pipeline.generateIbl != null)
            _material.SetVectorArray(GAmbientDiffuseSH, _cameraRenderer.pipeline.generateIbl.DiffuseSHVectors);
        
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "LightingPass";
        cmd.Blit(BuiltinRenderTextureType.None, _cameraRenderer.screenMap, _material);
        _cameraRenderer.ExecuteAndClearCmd(cmd);
        context.Submit();
    }
}
