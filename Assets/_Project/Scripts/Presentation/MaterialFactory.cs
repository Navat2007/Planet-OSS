using UnityEngine;
using UnityEngine.Rendering;

namespace Planet.Presentation
{
    /// <summary>
    /// Создание рантайм-материалов для оверлеев (линии, флажки, призрак, полосы ХП, маркер).
    /// Единое место с настройкой под активный пайплайн (HDRP/Unlit). Цвет/текстуру
    /// проставляем под несколько имён свойств, чтобы работало и при смене шейдера.
    /// </summary>
    public static class MaterialFactory
    {
        private static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int UnlitColorMapId = Shader.PropertyToID("_UnlitColorMap");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        private static Shader UnlitShader
        {
            get
            {
                // Выбираем под активный пайплайн — иначе HDRP-шейдер под URP (или наоборот) даст «розовый».
                bool hdrp = IsHdrpActive();
                Shader sh = hdrp ? Shader.Find("HDRP/Unlit") : Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("HDRP/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                return sh;
            }
        }

        /// <summary>Активен ли сейчас HDRP-пайплайн.</summary>
        public static bool IsHdrpActive()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            return rp != null && rp.GetType().Name.Contains("HDRenderPipeline");
        }

        /// <summary>Непрозрачный unlit-материал заданного цвета.</summary>
        public static Material UnlitOpaque(Color color)
        {
            var m = new Material(UnlitShader);
            ApplyColor(m, color);
            return m;
        }

        /// <summary>Полупрозрачный unlit-материал (alpha-блендинг).</summary>
        public static Material UnlitTransparent(Color color)
        {
            var m = new Material(UnlitShader);
            MakeTransparent(m);
            ApplyColor(m, color);
            return m;
        }

        /// <summary>Перевести материал в прозрачный alpha-режим (HDRP + запас на URP).</summary>
        public static void MakeTransparent(Material m)
        {
            // HDRP/Unlit
            if (m.HasProperty("_SurfaceType")) m.SetFloat("_SurfaceType", 1f); // Transparent
            if (m.HasProperty("_BlendMode")) m.SetFloat("_BlendMode", 0f);     // Alpha
            if (m.HasProperty("_ZTestDepthEqualForOpaque")) m.SetFloat("_ZTestDepthEqualForOpaque", (float)CompareFunction.LessEqual);
            if (m.HasProperty("_AlphaCutoffEnable")) m.SetFloat("_AlphaCutoffEnable", 0f);
            // URP/Unlit (если HDRP-шейдер не найден)
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);

            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_AlphaSrcBlend")) m.SetFloat("_AlphaSrcBlend", (float)BlendMode.One);
            if (m.HasProperty("_AlphaDstBlend")) m.SetFloat("_AlphaDstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);

            m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.EnableKeyword("_BLENDMODE_ALPHA");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>Проставить цвет под все ходовые имена свойств.</summary>
        public static void ApplyColor(Material m, Color c)
        {
            if (m.HasProperty(UnlitColorId)) m.SetColor(UnlitColorId, c);
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
        }

        /// <summary>Проставить основную текстуру под ходовые имена свойств.</summary>
        public static void ApplyTexture(Material m, Texture tex)
        {
            if (m.HasProperty(UnlitColorMapId)) m.SetTexture(UnlitColorMapId, tex);
            if (m.HasProperty(BaseMapId)) m.SetTexture(BaseMapId, tex);
            m.mainTexture = tex;
        }
    }
}
