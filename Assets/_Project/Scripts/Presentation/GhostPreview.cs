using System.Collections.Generic;
using Planet.Sim;
using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Призрак-превью приказа: при facing-перетягивании показывает позиции строя
    /// и направление (стрелка-черта по facing) для каждого выделенного юнита.
    /// (Упрощённый вид; полноценные модели-призраки — позже по референсу.)
    /// </summary>
    public sealed class GhostPreview : MonoBehaviour
    {
        private readonly List<Transform> _arrows = new List<Transform>();
        private static Material _mat;

        public void Show(IReadOnlyList<SimEntity> units, Vector3 centerWorld, Vector3 facingWorld)
        {
            int n = units.Count;
            if (n == 0) { Hide(); return; }

            int maxRadius = 0;
            for (int i = 0; i < n; i++)
                if (units[i].Radius > maxRadius) maxRadius = units[i].Radius;

            var slots = new SimVector2[n];
            SimFormation.Fill(n, maxRadius, SimConvert.ToSim(centerWorld), slots);

            float yaw = facingWorld.sqrMagnitude > 1e-4f
                ? Mathf.Atan2(facingWorld.x, facingWorld.z) * Mathf.Rad2Deg
                : 0f;

            EnsureCount(n);
            for (int i = 0; i < n; i++)
            {
                Transform t = _arrows[i];
                t.gameObject.SetActive(true);
                t.position = SimConvert.ToWorld(slots[i]) + Vector3.up * 0.06f;
                t.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            for (int i = n; i < _arrows.Count; i++) _arrows[i].gameObject.SetActive(false);
        }

        public void Hide()
        {
            for (int i = 0; i < _arrows.Count; i++) _arrows[i].gameObject.SetActive(false);
        }

        private void EnsureCount(int n)
        {
            while (_arrows.Count < n) _arrows.Add(CreateArrow());
        }

        private Transform CreateArrow()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Ghost";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(0.25f, 0.04f, 1.3f); // черта вдоль +Z (facing)

            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        private static Material Mat
        {
            get
            {
                if (_mat != null) return _mat;
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                _mat = new Material(sh);
                var c = new Color(0.4f, 1f, 0.5f, 1f);
                if (_mat.HasProperty("_BaseColor")) _mat.SetColor("_BaseColor", c);
                if (_mat.HasProperty("_Color")) _mat.SetColor("_Color", c);
                return _mat;
            }
        }
    }
}
