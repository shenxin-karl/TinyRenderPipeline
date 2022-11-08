using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Rendering;

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
    private Cubemap _skyboxMap;
    private SH3     _diffuseSH3;
    private Cubemap _specularMap;
    private bool    _isInit = false;

    private ComputeShader _generateSHShader;
    
    public SH3     DiffuseSH3  => _diffuseSH3;
    public Cubemap SpecularMap => _specularMap;
    public bool    IsInit      => _isInit;

    public static readonly int kThreadX = 8;
    public static readonly int kThreadY = 8;
    
    public GenerateIBL(Cubemap cubemap)
    {
        if (cubemap == null)
            throw new System.NullReferenceException("GenerateIBL constructor cubemap is null"); 
        _skyboxMap = cubemap;
        _generateSHShader = Resources.Load<ComputeShader>("Shaders/IrradianceMapCS");
    }

    public void Generate(ScriptableRenderContext context) {
        GenerateDiffuseSH(context);
    }

    private void GenerateDiffuseSH(ScriptableRenderContext context) {
        int kernelIndex = _generateSHShader.FindKernel("CSMain");

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
        _generateSHShader.SetTexture(kernelIndex, "gEnvMap", _skyboxMap);
        _generateSHShader.SetBuffer(kernelIndex, "gOutput", output);
        _generateSHShader.SetVector("gResolution", skyboxResolution);
        _generateSHShader.SetVector("gFaceNumBlock", new Vector2(blockX, blockY));

        for (int i = 0; i < 6; ++i) {
            _generateSHShader.SetFloat("gCubeMapIndex", i);
            _generateSHShader.Dispatch( kernelIndex, blockX, blockY, 1);
        }

        Vector3[] data = new Vector3[output.count * SH3.kVector3Count];
        output.GetData(data);

        _diffuseSH3 = new SH3();
        for (int i = 0; i < SH3.kVector3Count; ++i)
            _diffuseSH3[i] = new Vector3(0f, 0f, 0f);
        
        for (int i = 0; i < data.Length; ++i)
            _diffuseSH3[i % SH3.kVector3Count] += data[i];
    }
};
