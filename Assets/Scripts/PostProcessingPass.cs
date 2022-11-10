using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcessingPass : IPass {
    private ComputeShader _shader;
    private int _kernelIndex;
    private int _featureFrag;

    public enum FeatureFrag {
        EnableAcesToneMapping = (1 << 0),
        EnableGammaCorrection = (1 << 1),
        EnableColorLut        = (1 << 2),
    };
    
    public PostProcessingPass(CameraRenderer cameraRenderer) : base(cameraRenderer) {
        _shader = Resources.Load<ComputeShader>("Shaders/PostProcessing");
        _kernelIndex = _shader.FindKernel("CSMain");
        _featureFrag = 0;

    }

    public override void Init(ScriptableRenderContext context) {
        TinyRenderPipelineAsset settings = _cameraRenderer.pipeline.pipelineSettings;
        if (settings.enableAcesToneMapping)
            _featureFrag |= (int)FeatureFrag.EnableAcesToneMapping;
        if (settings.enableGammaCorrection)
            _featureFrag |= (int)FeatureFrag.EnableGammaCorrection;
        if (settings.colorLutMap != null)
            _featureFrag |= (int)FeatureFrag.EnableColorLut;
        
        _shader.SetInt("gFeatureFlag", _featureFrag);
    }
    
    public override void ExecutePass(ScriptableRenderContext context) {
        _shader.GetKernelThreadGroupSizes(
            _kernelIndex, 
            out uint groupX, 
            out uint groupY, 
            out uint groupZ
        );
        
        TinyRenderPipelineAsset settings = _cameraRenderer.pipeline.pipelineSettings;
        if ((_featureFrag & (int)FeatureFrag.EnableColorLut) != 0 && settings.colorLutMap != null) {
            _shader.SetTexture(_kernelIndex, "gColorLutMap", settings.colorLutMap);
        } else {
            _featureFrag &= ~(int)FeatureFrag.EnableColorLut;
            _shader.SetTexture(_kernelIndex, "gColorLutMap", Texture2D.whiteTexture);
        } 
        
        int width = _cameraRenderer.width;
        int height = _cameraRenderer.height;
        int x = MathUtility.DivideByMultiple(width, (int)groupX);
        int y = MathUtility.DivideByMultiple(height, (int)groupY);
        _shader.SetTexture(_kernelIndex, "gScreenMap", _cameraRenderer.screenMap);
        _shader.Dispatch(_kernelIndex, x, y, 1);
    }
}
