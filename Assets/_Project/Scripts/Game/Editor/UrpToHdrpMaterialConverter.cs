using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Planet.Game.Editor
{
    /// <summary>
    /// Батч-конвертер материалов URP/Standard → HDRP. Готового конвертера в Unity нет,
    /// поэтому переносим вручную: URP/Lit, URP/Simple Lit, Built-in Standard → HDRP/Lit;
    /// URP/Unlit → HDRP/Unlit; *Terrain* → HDRP/TerrainLit. Переносим базовую
    /// текстуру/цвет/нормаль/металл/гладкость/эмиссию и режим прозрачности
    /// (opaque/alpha-clip/transparent — в т.ч. cutout-листву деревьев).
    ///
    /// URP Shader Graph (вода, кастомные terrain-графы) сюда НЕ входит — такой граф
    /// несовместим с HDRP и простой сменой шейдера не чинится, его надо пересобирать
    /// под HDRP-таргет или менять на HDRP-аналог.
    /// </summary>
    public static class UrpToHdrpMaterialConverter
    {
        // Внешние паки на Standard/URP-Lit (растительность, грунт). URP Shader Graph
        // (Simple Water Shader, Terrain Shadergraph) намеренно НЕ включаем — см. summary.
        private static readonly string[] ExternalPackRoots =
        {
            "Assets/Tom's Terrain Tools",
            "Assets/TL_Grass_02",
            "Assets/TL_Gravel_01",
        };

        [MenuItem("Planet/Setup/Convert Materials URP -> HDRP (_Project)")]
        public static void ConvertProjectMaterials()
        {
            ConvertRoots(new[] { "Assets/_Project" }, "_Project");
        }

        [MenuItem("Planet/Setup/Convert Materials URP -> HDRP (External Packs)")]
        public static void ConvertExternalPackMaterials()
        {
            ConvertRoots(ExternalPackRoots, "внешние паки");
        }

        private static void ConvertRoots(string[] roots, string label)
        {
            Shader hdrpLit = Shader.Find("HDRP/Lit");
            Shader hdrpUnlit = Shader.Find("HDRP/Unlit");
            Shader hdrpTerrain = Shader.Find("HDRP/TerrainLit");
            if (hdrpLit == null || hdrpUnlit == null)
            {
                Debug.LogError("[Planet] HDRP-шейдеры не найдены — HDRP не активен?");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", roots);
            int converted = 0, skipped = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null || m.shader == null) { skipped++; continue; }

                string sh = m.shader.name;

                // Уже HDRP или спец-шейдеры (скайбокс/UI/партиклы/текст) — не трогаем.
                if (sh.StartsWith("HDRP/") || sh.StartsWith("Skybox") || sh.StartsWith("UI/") ||
                    sh.StartsWith("Sprites/") || sh.StartsWith("TextMeshPro") || sh.StartsWith("Hidden/") ||
                    sh.Contains("Particles"))
                {
                    skipped++;
                    continue;
                }

                if (sh.Contains("Terrain"))
                {
                    if (hdrpTerrain == null) { skipped++; continue; } // HDRP/TerrainLit нет — оставляем как есть
                    ConvertToHdrpTerrain(m, hdrpTerrain);
                }
                else if (sh == "Universal Render Pipeline/Unlit" || sh == "Unlit/Texture" || sh == "Unlit/Color")
                    ConvertToHdrpUnlit(m, hdrpUnlit);
                else
                    ConvertToHdrpLit(m, hdrpLit); // URP/Lit, URP/2D, Standard, кастомные ShaderGraph и т.п. → HDRP/Lit
                converted++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Planet] Конвертация материалов URP→HDRP ({label}): переведено {converted}, пропущено {skipped}.");
        }

        private static void ConvertToHdrpLit(Material m, Shader hdrpLit)
        {
            // Считать значения ДО смены шейдера.
            Color baseColor = GetColor(m, Color.white, "_BaseColor", "_Color");
            Texture baseMap = GetTex(m, "_BaseMap", "_MainTex");
            Vector2 tiling = m.HasProperty("_BaseMap") ? m.GetTextureScale("_BaseMap") : Vector2.one;
            Vector2 offset = m.HasProperty("_BaseMap") ? m.GetTextureOffset("_BaseMap") : Vector2.zero;
            Texture bump = GetTex(m, "_BumpMap");
            float metallic = GetFloat(m, 0f, "_Metallic");
            float smoothness = GetFloat(m, 0.5f, "_Smoothness", "_Glossiness");
            Color emission = GetColor(m, Color.black, "_EmissionColor");
            Texture emissionMap = GetTex(m, "_EmissionMap");
            float cutoff = GetFloat(m, 0.5f, "_Cutoff", "_AlphaClipThreshold");

            // Определяем режим поверхности из исходного шейдера:
            //   Standard: _Mode 0=Opaque,1=Cutout,2=Fade,3=Transparent
            //   URP:      _Surface 0=Opaque,1=Transparent + _AlphaClip
            float stdMode = GetFloat(m, -1f, "_Mode");
            bool urpAlphaClip = GetFloat(m, 0f, "_AlphaClip") > 0.5f;
            bool cutout = stdMode == 1f || urpAlphaClip;
            bool transparent = GetFloat(m, 0f, "_Surface") > 0.5f || stdMode == 2f || stdMode == 3f;

            m.shader = hdrpLit;

            m.SetColor("_BaseColor", baseColor);
            if (baseMap != null)
            {
                m.SetTexture("_BaseColorMap", baseMap);
                m.SetTextureScale("_BaseColorMap", tiling);
                m.SetTextureOffset("_BaseColorMap", offset);
            }
            if (bump != null)
            {
                m.SetTexture("_NormalMap", bump);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);

            if (emissionMap != null || emission.maxColorComponent > 0f)
            {
                if (emissionMap != null) m.SetTexture("_EmissiveColorMap", emissionMap);
                m.SetColor("_EmissiveColor", emission);
                m.SetFloat("_UseEmissiveIntensity", 0f);
                m.EnableKeyword("_EMISSIVE_COLOR_MAP");
            }

            // Cutout (листва деревьев и т.п.) важнее проверять первым: листву нельзя
            // переводить в alpha-blend — она должна отсекаться по альфе и быть двусторонней.
            if (cutout) SetHdrpAlphaClip(m, cutoff);
            else if (transparent) SetHdrpTransparent(m);

            EditorUtility.SetDirty(m);
        }

        private static void ConvertToHdrpUnlit(Material m, Shader hdrpUnlit)
        {
            Color baseColor = GetColor(m, Color.white, "_BaseColor", "_Color");
            Texture baseMap = GetTex(m, "_BaseMap", "_MainTex");
            bool transparent = GetFloat(m, 0f, "_Surface") > 0.5f;

            m.shader = hdrpUnlit;
            if (m.HasProperty("_UnlitColor")) m.SetColor("_UnlitColor", baseColor);
            if (baseMap != null && m.HasProperty("_UnlitColorMap")) m.SetTexture("_UnlitColorMap", baseMap);

            if (transparent) SetHdrpTransparent(m);
            EditorUtility.SetDirty(m);
        }

        private static void ConvertToHdrpTerrain(Material m, Shader hdrpTerrain)
        {
            // Слои террейна (текстуры/нормали/тайлинг) HDRP/TerrainLit берёт из TerrainData
            // через TerrainLayers, а не из свойств материала — достаточно сменить шейдер.
            m.shader = hdrpTerrain;
            EditorUtility.SetDirty(m);
        }

        private static void SetHdrpAlphaClip(Material m, float cutoff)
        {
            if (m.HasProperty("_AlphaCutoffEnable")) m.SetFloat("_AlphaCutoffEnable", 1f);
            if (m.HasProperty("_AlphaCutoff")) m.SetFloat("_AlphaCutoff", cutoff);
            if (m.HasProperty("_AlphaCutoffShadow")) m.SetFloat("_AlphaCutoffShadow", cutoff);
            // Листва видна с обеих сторон — делаем материал двусторонним.
            if (m.HasProperty("_DoubleSidedEnable")) m.SetFloat("_DoubleSidedEnable", 1f);
            if (m.HasProperty("_CullMode")) m.SetFloat("_CullMode", (float)CullMode.Off);
            m.EnableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_DOUBLESIDED_ON");
            m.renderQueue = (int)RenderQueue.AlphaTest;
        }

        private static void SetHdrpTransparent(Material m)
        {
            if (m.HasProperty("_SurfaceType")) m.SetFloat("_SurfaceType", 1f);
            if (m.HasProperty("_BlendMode")) m.SetFloat("_BlendMode", 0f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Color GetColor(Material m, Color fallback, params string[] names)
        {
            foreach (var n in names) if (m.HasProperty(n)) return m.GetColor(n);
            return fallback;
        }

        private static Texture GetTex(Material m, params string[] names)
        {
            foreach (var n in names) if (m.HasProperty(n) && m.GetTexture(n) != null) return m.GetTexture(n);
            return null;
        }

        private static float GetFloat(Material m, float fallback, params string[] names)
        {
            foreach (var n in names) if (m.HasProperty(n)) return m.GetFloat(n);
            return fallback;
        }
    }
}
