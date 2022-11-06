using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public class GeometryShaderGUI : ShaderGUI {
    private Object[] _targets;
    private MaterialEditor _editor;
    private MaterialProperty[] _properties;
    
    private bool _shouldShowAlphaCutoff;
    
    private static readonly string RenderingAlphaKeyword = "_ENABLE_ALPHA_TEST";
    private static readonly string NormalMapKeyword      = "_ENABLE_NORMAL_MAP";
    private static readonly string MetallicMapKeyword    = "_ENABLE_METALLIC_TEX";
    private static readonly string AlbedoMapKeyword      = "_ENABLE_ALBEDO_MAP";
    private static readonly string RoughnessMapKeyword   = "_ENABLE_ROUGHNESS_TEX";
    // private static readonly string RenderingOpaqueKeyword = "_ENABLE_OPAQUE";
    
    enum RenderingMode {
        Opaque      = 0,
        AlphaTest   = 1,
    };

    struct RenderingSettings {
        public RenderQueue queue;
        public string renderType;
        
        public static RenderingSettings[] modes = {
            new RenderingSettings() {
                queue = RenderQueue.Geometry,
                renderType = "Opaque",
            },
            new RenderingSettings() {
                queue = RenderQueue.AlphaTest,
                renderType = "TransparentCutout"
            }
        };
    }
    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
        _targets = materialEditor.targets;
        _editor = materialEditor;
        _properties = properties;
        DoRenderingMode();
        DoMain();
    }

    private void DoRenderingMode() {
        RenderingMode mode = RenderingMode.Opaque;
        if (IsKeywordEnable(RenderingAlphaKeyword))
            mode = RenderingMode.AlphaTest;
        
        EditorGUI.BeginChangeCheck();
        mode = (RenderingMode)EditorGUILayout.EnumPopup(MakeLabel("Rendering Mode"), mode);
        _shouldShowAlphaCutoff = (mode == RenderingMode.AlphaTest);
        if (EditorGUI.EndChangeCheck()) {
            RecordAction("Rendering Mode");
            SetKeyWord(RenderingAlphaKeyword, mode == RenderingMode.AlphaTest);
            RenderingSettings settings = RenderingSettings.modes[(int)mode];
            foreach (Object o in _targets) {
                Material m = (Material)o;
                m.renderQueue = (int)settings.queue;
                m.SetOverrideTag("RenderType", settings.renderType);
                // m.SetFloat("_SrcBlend", (float)settings.srcBlend);
                // m.SetFloat("_DstBlend", (float)settings.dstBlend);
                // m.SetFloat("_ZWrite", settings.zWrite ? 1f : 0f);
            }
        }
    }

    private void DoMain() {
        GUILayout.Label("Main Maps", EditorStyles.boldLabel);
        MaterialProperty mainTex = FindProperty("_MainTex");
        MaterialProperty color = FindProperty("_Color");
        EditorGUI.BeginChangeCheck();
        {
            _editor.TexturePropertySingleLine(MakeLabel("Albedo", "Albedo(RGB)"), mainTex, color);
        }
        if (EditorGUI.EndChangeCheck())
            SetKeyWord(AlbedoMapKeyword, mainTex.textureValue);

        DoAlphaCutoff();
        DoNormal();
        DoMetallic();
        DoRoughness();
    }

    private void DoAlphaCutoff() {
        if (!_shouldShowAlphaCutoff)
            return;
        MaterialProperty alphaCutoffSlider = FindProperty("_AlphaCutoff");
        EditorGUI.indentLevel += 2;
        _editor.ShaderProperty(alphaCutoffSlider, MakeLabel("AlphaCutoff"));
        EditorGUI.indentLevel -= 2;
    }

    private void DoNormal() {
        MaterialProperty normalTex = FindProperty("_NormalTex");
        MaterialProperty bumpScale = FindProperty("_BumpScale");
        EditorGUI.BeginChangeCheck();
        {
            _editor.TexturePropertySingleLine(
                MakeLabel("Normal", "Normal(RGB)"), 
                normalTex, 
                normalTex.textureValue ? bumpScale : null
            );
        }
        if (EditorGUI.EndChangeCheck())
            SetKeyWord(NormalMapKeyword, normalTex.textureValue);
    }

    private void DoMetallic() {
        MaterialProperty metallicTex = FindProperty("_MetallicTex");
        MaterialProperty slider = FindProperty("_Metallic");
        EditorGUI.BeginChangeCheck();
        {
            _editor.TexturePropertySingleLine(
                MakeLabel("Metallic", "Metallic(R)"),
                metallicTex,
                slider
            );
        }
        if (EditorGUI.EndChangeCheck())
            SetKeyWord(MetallicMapKeyword, metallicTex.textureValue);
    }
    
    private void DoRoughness() {
        MaterialProperty roughnessTex = FindProperty("_RoughnessTex");
        MaterialProperty slider = FindProperty("_Roughness");
        EditorGUI.BeginChangeCheck();
        {
            _editor.TexturePropertySingleLine(
                MakeLabel("Roughness", "Roughness(R)"),
                roughnessTex,
                slider
            );
        }
        if (EditorGUI.EndChangeCheck())
            SetKeyWord(RoughnessMapKeyword, roughnessTex.textureValue);
    }

    private bool IsKeywordEnable(string keyword) {
        if (_targets.Length == 0)
            return false;
        Material m = _targets[0] as Material;
        return m != null && m.IsKeywordEnabled(keyword);
    }
    
    private static readonly GUIContent StaticLabel = new GUIContent();
    private static GUIContent MakeLabel(string text, string tooltip = null) {
        StaticLabel.text = text;
        StaticLabel.tooltip = tooltip;
        return StaticLabel;
    }
    
    void RecordAction(string label) {
        _editor.RegisterPropertyChangeUndo(label);
    }
    
    void SetKeyWord(string keyword, bool state) {
        foreach (var o in _targets) {
            var m = (Material)o;
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }
    }
    
    private MaterialProperty FindProperty(string name) {
        return FindProperty(name, _properties);
    }
}
