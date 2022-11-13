using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;


[CreateAssetMenu(menuName = "Rendering/TinyRenderPipeline")]
public class TinyRenderPipelineAsset : RenderPipelineAsset {
    public Texture skyboxCubeMap;
    public Texture brdfLutMap;
    public Texture colorLutMap;
    public bool enableAcesToneMapping = true;
    public bool enableGammaCorrection = true;
    public Cubemap irradianceMap;
    public Cubemap prefilterMap;
    
    protected override RenderPipeline CreatePipeline() {
        return new TinyRenderPipeline(this);
    }
}
