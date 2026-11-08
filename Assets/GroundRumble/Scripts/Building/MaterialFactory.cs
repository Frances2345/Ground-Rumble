using UnityEngine;
using UnityEngine.Rendering;

namespace GroundRumble
{
    /// <summary>
    /// Crea materiales URP por codigo (paredes, pisos, vidrio y añicos).
    /// </summary>
    public static class MaterialFactory
    {
        private const string UrpLitShader = "Universal Render Pipeline/Lit";
        private const string StandardShader = "Standard";

        public static Material CreateLitMaterial(Color color, float smoothness = 0.1f, float metallic = 0f)
        {
            Shader shader = Shader.Find(UrpLitShader);

            if (shader == null)
            {
                shader = Shader.Find(StandardShader);
            }

            Material material = new Material(shader)
            {
                name = "Material_Generado"
            };

            SetColor(material, color);
            SetFloatIfExists(material, "_Smoothness", smoothness);
            SetFloatIfExists(material, "_Glossiness", smoothness);
            SetFloatIfExists(material, "_Metallic", metallic);
            SetTransparent(material, false);

            return material;
        }

        public static Material CreateGlassMaterial()
        {
            Material material = CreateLitMaterial(new Color(0.62f, 0.82f, 1f, 0.22f), 0.95f, 0f);
            material.name = "Vidrio";
            SetTransparent(material, true);
            return material;
        }

        public static Material CreateGlassShardMaterial()
        {
            Material material = CreateLitMaterial(new Color(0.7f, 0.9f, 1f, 0.65f), 0.9f, 0f);
            material.name = "Anico_Vidrio";
            SetTransparent(material, true);
            return material;
        }

        public static void SetTransparent(Material material, bool transparent)
        {
            if (material == null)
            {
                return;
            }

            bool isUrp = material.HasProperty("_Surface");

            if (isUrp)
            {
                material.SetFloat("_Surface", transparent ? 1f : 0f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_ZWrite", transparent ? 0f : 1f);

                if (transparent)
                {
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }
                else
                {
                    material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetInt("_SrcBlend", (int)BlendMode.One);
                    material.SetInt("_DstBlend", (int)BlendMode.Zero);
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.renderQueue = (int)RenderQueue.Geometry;
                }
            }
            else
            {
                if (transparent)
                {
                    material.SetFloat("_Mode", 3f);
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }
                else
                {
                    material.SetFloat("_Mode", 0f);
                    material.SetInt("_SrcBlend", (int)BlendMode.One);
                    material.SetInt("_DstBlend", (int)BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)RenderQueue.Geometry;
                }
            }

            material.SetFloat("_Cull", (float)CullMode.Back);
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.color = color;
        }

        private static void SetFloatIfExists(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }
    }
}
