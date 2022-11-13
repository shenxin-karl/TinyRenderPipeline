using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Net.NetworkInformation;
using System.Numerics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

public class SH3 {
    public static readonly int kSH3Byte = sizeof(float) * 3 * 9;
    public static readonly int kVector3Count = 9;
    public Vector3[] data = new Vector3[9];
    public Vector3 _0   { get { return data[0]; } set { data[0] = value; } }
    public Vector3 _1_1 { get { return data[1]; } set { data[1] = value; } }
    public Vector3 _1   { get { return data[2]; } set { data[2] = value; } }
    public Vector3 _11  { get { return data[3]; } set { data[3] = value; } }
    public Vector3 _2_2 { get { return data[4]; } set { data[4] = value; } }
    public Vector3 _2_1 { get { return data[5]; } set { data[5] = value; } }
    public Vector3 _2   { get { return data[6]; } set { data[6] = value; } }
    public Vector3 _21  { get { return data[7]; } set { data[7] = value; } }
    public Vector3 _22  { get { return data[8]; } set { data[8] = value; } }
    public Vector3 this[int index] {
        get {
            if (index < 0 || index > 8)
                throw new IndexOutOfRangeException("Invalid SH3 index");
            return data[index];
        }
        set {
            if (index < 0 || index > 8)
                throw new IndexOutOfRangeException("Invalid SH3 index");
            data[index] = value;
        }
    }

    public Vector4[] toVector4Array() {
        Vector4[] array = new Vector4[kVector3Count];
        for (int i = 0; i < kVector3Count; ++i)
            array[i] = data[i];
        return array;
    }
};

public class GenerateIBL  {
    private Texture   _panoramaMap;
    private Texture   _environmentMap;
    private SH3       _diffuseSH3;
    private Vector4[] _diffuseSHVectors;
    private Cubemap   _specularMap;
    private bool      _isInit = false;
    private RenderTexture _cubeMapRenderTexture;
    
    
    public SH3     DiffuseSH3  => _diffuseSH3;
    public Cubemap SpecularMap => _specularMap;
    public bool    IsInit      => _isInit;
    public Vector4[] DiffuseSHVectors => _diffuseSHVectors;
    
    public RenderTexture IrradianceMap;
    
    public static readonly int kThreadX = 8;
    public static readonly int kThreadY = 8;
    
    public GenerateIBL(Texture skybox) {
        if (skybox == null)
            throw new System.NullReferenceException("GenerateIBL constructor cubemap is null");

        if (skybox.dimension == TextureDimension.Cube)
            _environmentMap = (Cubemap)skybox;
        else if (skybox.dimension == TextureDimension.Tex2D)
            _panoramaMap = skybox;
        else
            throw new System.ArgumentException("invalid skybox dimension");

        IrradianceMap = new RenderTexture(32, 32, 0, GraphicsFormat.R16G16B16A16_SFloat);
        IrradianceMap.enableRandomWrite = true;
        IrradianceMap.dimension = TextureDimension.Cube;
        IrradianceMap.Create();
    }

    ~GenerateIBL() {
        IrradianceMap.Release();
        if (_cubeMapRenderTexture != null)
            _cubeMapRenderTexture.Release();
    }
    
    public void Generate(ScriptableRenderContext context) {
        // GenerateEnvironmentMap(context);
        GenerateDiffuseSH(context);
    }

    public void GenerateEnvironmentMap(ScriptableRenderContext context) {
        // if (_environmentMap != null)
        //     return;
        
        if (_cubeMapRenderTexture != null)
            _cubeMapRenderTexture.Release();

        _cubeMapRenderTexture = new RenderTexture(
            512, 
            512, 
            0, 
            GraphicsFormat.R16G16B16A16_SFloat
        ) {
            dimension = TextureDimension.Tex2DArray | TextureDimension.Cube,
            volumeDepth = 6,
            enableRandomWrite = true,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Trilinear
        };
        _cubeMapRenderTexture.Create();
        
        ComputeShader shader = Resources.Load<ComputeShader>("Shaders/PanoramaToCube");
        int kernelIndex = shader.FindKernel("CSMain");
        shader.SetTexture(kernelIndex, "gPanoramaMap" ,_panoramaMap);
        shader.SetTexture(kernelIndex, "gResult", _cubeMapRenderTexture);
        shader.GetKernelThreadGroupSizes(kernelIndex, out uint groupX, out uint groupY, out uint groupZ);

        int dx = MathUtility.DivideByMultiple(512, (int)groupX);
        int dy = MathUtility.DivideByMultiple(512, (int)groupY);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "GenerateEnvironmentMap";
        cmd.DispatchCompute(shader, kernelIndex, dx, dy, 6);
        context.ExecuteCommandBuffer(cmd);
        context.Submit();
        cmd.Release();

        // _cubeMapRenderTexture;
        _environmentMap = _cubeMapRenderTexture;
    }
    
    private void GenerateDiffuseSH(ScriptableRenderContext context) {
        ComputeShader generateSHShader = Resources.Load<ComputeShader>("Shaders/IrradianceMapCS");
        int kernelIndex = generateSHShader.FindKernel("CSMain");

        const int kDiffuseMapResolution = 32;
        Vector4 skyboxResolution = new Vector4(
            kDiffuseMapResolution, 
            kDiffuseMapResolution, 
            1f / kDiffuseMapResolution, 
            1f / kDiffuseMapResolution
        );
        
        int blockX = MathUtility.DivideByMultiple(kDiffuseMapResolution, kThreadX);
        int blockY = MathUtility.DivideByMultiple(kDiffuseMapResolution, kThreadY);

        ComputeBuffer output = new ComputeBuffer(blockX * blockY * 6, SH3.kSH3Byte);
        generateSHShader.SetTexture(kernelIndex, "gEnvMap", _environmentMap);
        generateSHShader.SetBuffer(kernelIndex, "gOutput", output);
        generateSHShader.SetVector("gResolution", skyboxResolution);
        generateSHShader.SetVector("gFaceNumBlock", new Vector2(blockX, blockY));

        for (int i = 0; i < 6; ++i) {
            generateSHShader.SetFloat("gCubeMapIndex", i);
            generateSHShader.Dispatch( kernelIndex, blockX, blockY, 1);
        }

        Vector3[] data = new Vector3[output.count * SH3.kVector3Count];
        output.GetData(data);
        output.Release();

        _diffuseSH3 = new SH3();
        for (int i = 0; i < data.Length; ++i)
            _diffuseSH3[i % SH3.kVector3Count] += data[i];

        _diffuseSHVectors = new Vector4[SH3.kVector3Count];
        for (int i = 0; i < SH3.kVector3Count; ++i)
            _diffuseSHVectors[i] = _diffuseSH3[i];
    }

    public void DebugDirection() {
        ComputeShader generateSHShader = Resources.Load<ComputeShader>("Shaders/IrradianceMapCS");
        int kernelIndex = generateSHShader.FindKernel("CSMain");

        const int kDiffuseMapResolution = 32;
        Vector4 skyboxResolution = new Vector4(
            kDiffuseMapResolution, 
            kDiffuseMapResolution, 
            1f / kDiffuseMapResolution, 
            1f / kDiffuseMapResolution
        );
        
        int blockX = MathUtility.DivideByMultiple(kDiffuseMapResolution, kThreadX);
        int blockY = MathUtility.DivideByMultiple(kDiffuseMapResolution, kThreadY);

        ComputeBuffer outputDirection = new ComputeBuffer(32 * 32 * 6, sizeof(float) * 3);
        generateSHShader.SetBuffer(kernelIndex, "gOutputDirection", outputDirection);
        generateSHShader.SetVector("gResolution", skyboxResolution);
        generateSHShader.SetVector("gFaceNumBlock", new Vector2(blockX, blockY));

        for (int i = 0; i < 6; ++i) {
            generateSHShader.SetFloat("gCubeMapIndex", i);
            generateSHShader.Dispatch(kernelIndex, blockX, blockY, 1);
        }

        Vector3[] data = new Vector3[32 * 32 * 6];
        outputDirection.GetData(data);
        outputDirection.Release();
        
        _diffuseSH3 = new SH3();
        Vector3 half = new Vector3(0.5f, 0.5f, 0.5f);
        for (int i = 0; i < data.Length; ++i) {
            Vector3 N = data[i];
            float x = N.x;
            float y = N.y;
            float z = N.z;
            Vector3 irradiance = N * 0.5f + half;
            for (int j = 0; j < 9; ++j) {
                _diffuseSH3[0] += irradiance * 0.2820948f;							            // y0p0
                _diffuseSH3[1] += irradiance * 0.4886025f * y;						            // y1n1
                _diffuseSH3[2] += irradiance * 0.4886025f * z;						            // y1p0
                _diffuseSH3[3] += irradiance * 0.4886025f * x;						            // y1p1
                _diffuseSH3[4] += irradiance * 1.0925480f * x * y;					            // y2n2
                _diffuseSH3[5] += irradiance * 1.0925480f * y * z;					            // y2n1
                _diffuseSH3[6] += irradiance * 0.3153916f * ((3f * z * z) - 1f);	            // y2p0
                _diffuseSH3[7] += irradiance * 1.0925480f * x * z;					            // y2p1
                _diffuseSH3[8] += irradiance * 0.5462742f * (x * x - y * y);		            // y2p2
            }
        }
        for (int i = 0; i < 9; ++i) {
            _diffuseSH3[i] /= data.Length;
        }
    }
};
