using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class DeltaMaterials
{
    private static readonly Dictionary<string, Material> s_Cache = new Dictionary<string, Material>();

    public static void ClearCache()
    {
        s_Cache.Clear();
    }

    private static Shader GetUrpLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[DeltaMaterials] Shader 'Universal Render Pipeline/Lit' not found!");
        }
        return shader;
    }

    /// <summary>
    /// Brushed stainless steel material with procedural brushed normal map and smoothness variance.
    /// Albedo: 0.55, 0.56, 0.58 · Metallic: 1.0 · Smoothness: 0.60 · Normal bump scale: 0.25
    /// </summary>
    public static Material BrushedStainless()
    {
        const string key = "BrushedStainless";
        if (s_Cache.TryGetValue(key, out Material mat) && mat != null)
        {
            return mat;
        }

        Shader shader = GetUrpLitShader();
        mat = new Material(shader)
        {
            name = "M_Delta_BrushedStainless"
        };

        mat.SetColor("_BaseColor", new Color(0.55f, 0.56f, 0.58f, 1f));
        mat.SetFloat("_Metallic", 1.0f);
        mat.SetFloat("_Smoothness", 0.60f);

        Texture2D brushedNormal = GenerateBrushedNormalMap(256, 256);
        Texture2D smoothnessMask = GenerateSmoothnessVariationMap(256, 256, 0.60f, 0.05f);

        if (brushedNormal != null)
        {
            mat.SetTexture("_BumpMap", brushedNormal);
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_BumpScale", 0.25f);
        }

        if (smoothnessMask != null)
        {
            mat.SetTexture("_MetallicGlossMap", smoothnessMask);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        s_Cache[key] = mat;
        return mat;
    }

    /// <summary>
    /// Mid-dark industrial concrete material.
    /// Albedo: 0.22, 0.22, 0.23 · Smoothness: 0.15 · Metallic: 0.0
    /// </summary>
    public static Material Concrete()
    {
        const string key = "Concrete";
        if (s_Cache.TryGetValue(key, out Material mat) && mat != null)
        {
            return mat;
        }

        Shader shader = GetUrpLitShader();
        mat = new Material(shader)
        {
            name = "M_Delta_Concrete"
        };

        mat.SetColor("_BaseColor", new Color(0.22f, 0.22f, 0.23f, 1f));
        mat.SetFloat("_Metallic", 0.0f);
        mat.SetFloat("_Smoothness", 0.15f);

        s_Cache[key] = mat;
        return mat;
    }

    /// <summary>
    /// Clear PET transparent plastic material for bottles.
    /// Surface: Transparent · Queue: 3000 · Base: 0.85, 0.95, 0.94, 0.25 · Smoothness: 0.90 · Metallic: 0.0
    /// </summary>
    public static Material ClearPET()
    {
        const string key = "ClearPET";
        if (s_Cache.TryGetValue(key, out Material mat) && mat != null)
        {
            return mat;
        }

        Shader shader = GetUrpLitShader();
        mat = new Material(shader)
        {
            name = "M_Delta_ClearPET"
        };

        mat.SetFloat("_Surface", 1.0f); // Transparent
        mat.SetFloat("_Blend", 0.0f);   // Alpha
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetShaderPassEnabled("ShadowCaster", false);

        mat.SetColor("_BaseColor", new Color(0.85f, 0.95f, 0.94f, 0.25f));
        mat.SetFloat("_Smoothness", 0.90f);
        mat.SetFloat("_Metallic", 0.0f);

        s_Cache[key] = mat;
        return mat;
    }

    /// <summary>
    /// Opaque liquid volume material (e.g. pearly shampoo).
    /// Smoothness: 0.5 · Metallic: 0.0
    /// </summary>
    public static Material Liquid(Color tint)
    {
        string key = $"Liquid_{ColorUtility.ToHtmlStringRGBA(tint)}";
        if (s_Cache.TryGetValue(key, out Material mat) && mat != null)
        {
            return mat;
        }

        Shader shader = GetUrpLitShader();
        mat = new Material(shader)
        {
            name = $"M_Delta_Liquid_{ColorUtility.ToHtmlStringRGB(tint)}"
        };

        mat.SetColor("_BaseColor", tint);
        mat.SetFloat("_Metallic", 0.0f);
        mat.SetFloat("_Smoothness", 0.50f);

        s_Cache[key] = mat;
        return mat;
    }

    /// <summary>
    /// Default mid machine grey painted steel: Albedo 0.38, 0.39, 0.41 · Metallic 0.15 · Smoothness 0.30
    /// </summary>
    public static Material PaintedSteel()
    {
        return PaintedSteel(new Color(0.38f, 0.39f, 0.41f, 1f));
    }

    /// <summary>
    /// Painted machine-frame steel with custom color.
    /// Metallic: 0.15 · Smoothness: 0.30
    /// </summary>
    public static Material PaintedSteel(Color c)
    {
        string key = $"PaintedSteel_{ColorUtility.ToHtmlStringRGBA(c)}";
        if (s_Cache.TryGetValue(key, out Material mat) && mat != null)
        {
            return mat;
        }

        Shader shader = GetUrpLitShader();
        mat = new Material(shader)
        {
            name = $"M_Delta_PaintedSteel_{ColorUtility.ToHtmlStringRGB(c)}"
        };

        mat.SetColor("_BaseColor", c);
        mat.SetFloat("_Metallic", 0.15f);
        mat.SetFloat("_Smoothness", 0.30f);

        s_Cache[key] = mat;
        return mat;
    }

    /// <summary>
    /// Conveyor belt dark rubber surface.
    /// Albedo: 0.06, 0.06, 0.07 · Metallic: 0.0 · Smoothness: 0.12
    /// </summary>
    public static Material DarkRubber()
    {
        const string key = "DarkRubber";
        if (s_Cache.TryGetValue(key, out Material mat) && mat != null)
        {
            return mat;
        }

        Shader shader = GetUrpLitShader();
        mat = new Material(shader)
        {
            name = "M_Delta_DarkRubber"
        };

        mat.SetColor("_BaseColor", new Color(0.06f, 0.06f, 0.07f, 1f));
        mat.SetFloat("_Metallic", 0.0f);
        mat.SetFloat("_Smoothness", 0.12f);

        s_Cache[key] = mat;
        return mat;
    }

    private static Texture2D GenerateBrushedNormalMap(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Procedural_Brushed_Normal",
            wrapMode = TextureWrapMode.Repeat
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.04f, y * 0.5f);
                float nx = (Mathf.PerlinNoise((x + 1) * 0.04f, y * 0.5f) - n) * 0.5f;
                float ny = (Mathf.PerlinNoise(x * 0.04f, (y + 1) * 0.5f) - n) * 1.5f;

                Vector3 norm = new Vector3(-nx * 0.2f, -ny * 0.2f, 1.0f).normalized;
                Color c = new Color(norm.x * 0.5f + 0.5f, norm.y * 0.5f + 0.5f, norm.z * 0.5f + 0.5f, 1f);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D GenerateSmoothnessVariationMap(int width, int height, float baseSmoothness, float variance)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Procedural_MetallicSmoothness",
            wrapMode = TextureWrapMode.Repeat
        };

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                float s = Mathf.Clamp01(baseSmoothness + (n - 0.5f) * variance);
                // URP Standard Lit MetallicGlossMap: R = Metallic, G = Occlusion, B = Detail Mask, A = Smoothness
                Color c = new Color(1.0f, 1.0f, 0.0f, s);
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }
}
