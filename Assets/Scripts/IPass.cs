using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class IPass {
    protected CameraRenderer _cameraRenderer;

    protected IPass(CameraRenderer cameraRenderer) {
        _cameraRenderer = cameraRenderer;
    }
    
    public virtual void Init(ScriptableRenderContext context) {
    }
    public virtual void Resize(ScriptableRenderContext context, int width, int height) {
    }
}
