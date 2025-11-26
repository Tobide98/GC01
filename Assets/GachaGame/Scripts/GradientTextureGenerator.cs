using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class GradientTextureGenerator : MonoBehaviour
{
    [Tooltip("Gradient to generate into a 1px-high texture")]
    public Gradient gradient = new Gradient();

    [Tooltip("Width of the generated gradient texture (higher = smoother). Typical: 256")]
    public int width = 256;

    [Tooltip("If assigned, the generated texture will be stored here (optional)")]
    public Texture2D outputTexture;

    [Tooltip("If assigned, the generated texture will be applied to this material's _GradientTex property")]
    public Material targetMaterial;

    [ContextMenu("Generate Gradient Texture")]
    public void Generate()
    {
        if (width < 2) width = 2;

        Texture2D tex = new Texture2D(width, 1, TextureFormat.RGBA32, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < width; x++)
        {
            float t = (float)x / (width - 1);
            Color c = gradient.Evaluate(t);
            tex.SetPixel(x, 0, c);
        }

        tex.Apply();

        // optionally assign
        outputTexture = tex;
        if (targetMaterial != null)
        {
            targetMaterial.SetTexture("_GradientTex", tex);
#if UNITY_EDITOR
            EditorUtility.SetDirty(targetMaterial);
#endif
        }

        // keep the texture in memory during editor play/scene view; we don't save to assets by default
    }

    private void OnValidate()
    {
        // regenerate in editor when something changes
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Generate();
            // mark scene dirty so change persists
            if (targetMaterial != null) EditorUtility.SetDirty(targetMaterial);
        }
#endif
    }
}