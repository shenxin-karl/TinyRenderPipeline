using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer.Internal.Converters;
using UnityEngine;
using UnityEngine.Rendering;

public class LightingPass {
    public readonly int kGroupThreadX = 8;
    public readonly int kGroupThreadY = 8;
    private bool _useComputeShader = false;
    private ComputeShader _shader;
    private int _kernelIndex;
    
    private Material _material;
    private static readonly int GBuffer2 = Shader.PropertyToID("gBuffer2");
    private static readonly int GBuffer1 = Shader.PropertyToID("gBuffer1");
    private static readonly int GBuffer0 = Shader.PropertyToID("gBuffer0");
    private static readonly int GDepthMap = Shader.PropertyToID("gDepthMap");
    private static readonly int GInvViewProj = Shader.PropertyToID("gInvViewProj");

    private Vector3[] _frustumCorners;
    private Vector4[] _vectorArray;
    private static readonly int GFrustumCorners = Shader.PropertyToID("gFrustumCorners");

    public LightingPass() {
        _shader = Resources.Load<ComputeShader>("Shaders/LightingPassCS");
        _kernelIndex = _shader.FindKernel("CS");
        _material = new Material(Resources.Load<Shader>("Shaders/LightingPassPS"));
        _frustumCorners = new Vector3[4];
        _vectorArray = new Vector4[4];
    }

    public void Execute(CameraRenderer cameraRenderer, ScriptableRenderContext context) {
        if (_useComputeShader)
            ExecuteComputeShader(cameraRenderer, context);
        else
            ExecuteGraphicsShader(cameraRenderer, context);
    }
    
    public void ExecuteGraphicsShader(CameraRenderer cameraRenderer, ScriptableRenderContext context) {
        _material.SetTexture(GBuffer0, cameraRenderer.gBufferMaps[0]);
        _material.SetTexture(GBuffer1, cameraRenderer.gBufferMaps[1]);
        _material.SetTexture(GBuffer2, cameraRenderer.gBufferMaps[2]);
        _material.SetTexture(GDepthMap, cameraRenderer.depthMap);
        _material.SetMatrix(GInvViewProj, cameraRenderer.matInvViewProj);
        
        cameraRenderer.camera.CalculateFrustumCorners(
            new Rect(0, 0, 1, 1),
            cameraRenderer.camera.farClipPlane,
            cameraRenderer.camera.stereoActiveEye,
            _frustumCorners
        );
        
        var fov = cameraRenderer.camera.fieldOfView;
        var near = cameraRenderer.camera.nearClipPlane;
        var far = cameraRenderer.camera.farClipPlane;
        var aspect = cameraRenderer.camera.aspect;

        var halfHeight = far * Mathf.Tan(fov / 2 * Mathf.Deg2Rad);
        var toRight = cameraRenderer.camera.transform.right * halfHeight * aspect;
        var toTop = cameraRenderer.camera.transform.up * halfHeight;
        var toForward = cameraRenderer.camera.transform.forward * far;

        _vectorArray[0] = toForward + toTop - toRight;
        _vectorArray[1] = toForward + toTop + toRight;
        _vectorArray[2] = toForward - toTop - toRight;
        _vectorArray[3] = toForward - toTop + toRight;
        _material.SetVectorArray(GFrustumCorners, _vectorArray);
        
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "LightingPass";
        cmd.Blit(BuiltinRenderTextureType.None, cameraRenderer.screenMap, _material);
        cameraRenderer.ExecuteAndClearCmd(cmd);
        context.Submit();
    }
    
    public void ExecuteComputeShader(CameraRenderer cameraRenderer, ScriptableRenderContext context) {
        _shader.SetTexture(_kernelIndex, "gBuffer0", cameraRenderer.gBufferMaps[0]);
        _shader.SetTexture(_kernelIndex, "gBuffer1", cameraRenderer.gBufferMaps[1]);
        _shader.SetTexture(_kernelIndex, "gBuffer2", cameraRenderer.gBufferMaps[2]);
        _shader.SetTexture(_kernelIndex, "gDepthMap", cameraRenderer.depthMap);
        _shader.SetTexture(_kernelIndex, "gOutputMap", cameraRenderer.screenMap);
        int dx = MathUtility.DivideByMultiple(cameraRenderer.width,kGroupThreadX);
        int dy = MathUtility.DivideByMultiple(cameraRenderer.height, kGroupThreadY);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "LightingPass";
        cmd.DispatchCompute(_shader, _kernelIndex, dx, dy, 1);
        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }
    
}
