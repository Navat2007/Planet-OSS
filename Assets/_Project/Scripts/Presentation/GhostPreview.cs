using System.Collections.Generic;
using Planet.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Planet.Presentation
{
    /// <summary>
    /// Призрак-превью приказа: при facing-перетягивании показывает строй из
    /// полупрозрачных клонов моделей выделенных юнитов (пехота и техника),
    /// расставленных по формации и развёрнутых по заданному направлению.
    /// Чисто презентационный: симуляцию не трогает.
    /// </summary>
    public sealed class GhostPreview : MonoBehaviour
    {
        private readonly List<GameObject> _ghosts = new List<GameObject>();
        private readonly List<UnitView> _units = new List<UnitView>();
        private static Material _mat;

        /// <summary>Создать клоны моделей под текущее выделение (вызывать один раз при старте facing).</summary>
        public void Begin(IReadOnlyList<UnitView> units)
        {
            Clear();
            for (int i = 0; i < units.Count; i++)
            {
                UnitView u = units[i];
                _units.Add(u);
                if (u == null || u.Model == null) { _ghosts.Add(null); continue; }

                GameObject ghost = Instantiate(u.Model);
                ghost.name = "Ghost";
                ghost.transform.SetParent(transform, true);
                Ghostify(ghost);
                _ghosts.Add(ghost);
            }
        }

        /// <summary>Обновить позиции и разворот строя призраков по центру и направлению.</summary>
        public void UpdatePose(Vector3 centerWorld, Vector3 facingWorld)
        {
            int n = _units.Count;
            if (n == 0) return;

            int maxRadius = 0;
            for (int i = 0; i < n; i++)
            {
                var u = _units[i];
                if (u != null && u.Entity != null && u.Entity.Radius > maxRadius) maxRadius = u.Entity.Radius;
            }

            var slots = new SimVector2[n];
            SimFormation.Fill(n, maxRadius, SimConvert.ToSim(centerWorld), slots);

            float yaw = facingWorld.sqrMagnitude > 1e-4f
                ? Mathf.Atan2(facingWorld.x, facingWorld.z) * Mathf.Rad2Deg
                : 0f;

            for (int i = 0; i < n; i++)
            {
                GameObject g = _ghosts[i];
                if (g == null) continue;
                g.transform.position = SimConvert.ToWorld(slots[i]);
                float off = _units[i] != null ? _units[i].YawOffset : 0f;
                g.transform.rotation = Quaternion.Euler(0f, yaw + off, 0f);
            }
        }

        public void Hide() => Clear();

        private void Clear()
        {
            for (int i = 0; i < _ghosts.Count; i++)
                if (_ghosts[i] != null) Destroy(_ghosts[i]);
            _ghosts.Clear();
            _units.Clear();
        }

        /// <summary>Превратить клон в полупрозрачную голограмму: убрать коллайдеры, заменить материалы.</summary>
        private static void Ghostify(GameObject ghost)
        {
            foreach (var c in ghost.GetComponentsInChildren<Collider>(true)) Destroy(c);

            foreach (var r in ghost.GetComponentsInChildren<Renderer>(true))
            {
                int len = Mathf.Max(1, r.sharedMaterials.Length);
                var mats = new Material[len];
                for (int i = 0; i < len; i++) mats[i] = GhostMat;
                r.sharedMaterials = mats;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        private static Material GhostMat
        {
            get
            {
                if (_mat != null) return _mat;
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                _mat = new Material(sh);

                // Полупрозрачный режим URP Unlit.
                _mat.SetFloat("_Surface", 1f); // Transparent
                _mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                _mat.SetInt("_ZWrite", 0);
                _mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                _mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _mat.renderQueue = (int)RenderQueue.Transparent;

                var c = GameplaySettings.Instance.GhostColor; // голограмма (цвет из настроек)
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
                if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", c);
                return _mat;
            }
        }
    }
}
