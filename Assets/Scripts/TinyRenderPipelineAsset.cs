using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[CreateAssetMenu(menuName = "Rendering/TinyRenderPipeline")]
public class TinyRenderPipelineAsset : RenderPipelineAsset {
    public Cubemap skyboxCubeMap;
    public Texture brdfLutMap;
    
    protected override RenderPipeline CreatePipeline() {
        return new TinyRenderPipeline(this);
    }
}
