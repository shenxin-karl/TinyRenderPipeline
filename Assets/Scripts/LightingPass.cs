using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer.Internal.Converters;
using UnityEngine;
using UnityEngine.Rendering;

public class LightingPass {
    private ComputeShader _shader;
    private int _kernelIndex;
    
    private Material _material;
    private static readonly int GBuffer2 = Shader.PropertyToID("gBuffer2");
    private static readonly int GBuffer1 = Shader.PropertyToID("gBuffer1");
    private static readonly int GBuffer0 = Shader.PropertyToID("gBuffer0");
    private static readonly int GDepthMap = Shader.PropertyToID("gDepthMap");

    private static readonly int GMatInvViewProj = Shader.PropertyToID("gMatInvViewProj");
    private static readonly int GViewPortRay = Shader.PropertyToID("gViewPortRay");
    public LightingPass() {
        _material = new Material(Resources.Load<Shader>("Shaders/LightingPassPS"));
    }

    public void Execute(CameraRenderer cameraRenderer, ScriptableRenderContext context) {
        ExecuteGraphicsShader(cameraRenderer, context);
    }
    
    public void ExecuteGraphicsShader(CameraRenderer cameraRenderer, ScriptableRenderContext context) {
        _material.SetTexture(GBuffer0, cameraRenderer.gBufferMaps[0]);
        _material.SetTexture(GBuffer1, cameraRenderer.gBufferMaps[1]);
        _material.SetTexture(GBuffer2, cameraRenderer.gBufferMaps[2]);
        _material.SetTexture(GDepthMap, cameraRenderer.depthMap);
        _material.SetMatrix(GMatInvViewProj, cameraRenderer.matInvViewProj);
        
        var aspect = cameraRenderer.camera.aspect;
        var far = cameraRenderer.camera.farClipPlane;
        var right = cameraRenderer.camera.transform.right;
        var up = cameraRenderer.camera.transform.up;
        var forward = cameraRenderer.camera.transform.forward;
        var halfFovTan = Mathf.Tan(cameraRenderer.camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
 
        //计算相机在远裁剪面处的xyz三方向向量
        var rightVec = right * far * halfFovTan * aspect;
        var upVec = up * far * halfFovTan;
        var forwardVec = forward * far;
 
        //构建四个角的方向向量
        var topLeft = (forwardVec - rightVec + upVec);
        var topRight = (forwardVec + rightVec + upVec);
        var bottomLeft = (forwardVec - rightVec - upVec);
        var bottomRight = (forwardVec + rightVec - upVec);

        var viewPortRay = Matrix4x4.identity;
        viewPortRay.SetRow(0, topLeft);
        viewPortRay.SetRow(1, topRight);
        viewPortRay.SetRow(2, bottomLeft);
        viewPortRay.SetRow(3, bottomRight);
        
        _material.SetMatrix(GViewPortRay, viewPortRay);
        
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "LightingPass";
        cmd.Blit(BuiltinRenderTextureType.None, cameraRenderer.screenMap, _material);
        cameraRenderer.ExecuteAndClearCmd(cmd);
        context.Submit();
    }
}
