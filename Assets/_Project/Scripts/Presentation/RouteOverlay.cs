using System.Collections.Generic;
using Planet.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Planet.Presentation
{
    /// <summary>
    /// Маршрут выделенных юнитов: пунктирная линия по точкам
    /// (текущая позиция → цель → waypoints) и полупрозрачный флажок на каждой точке.
    /// Показывается, пока юнит выделен и у него есть маршрут. Чисто презентационный.
    /// </summary>
    public sealed class RouteOverlay : MonoBehaviour
    {
        private const float LineY = 0.08f; // приподнять линию над землёй

        private GameplaySettings _s;
        private SelectionController _selection;
        private Camera _cam;

        private readonly List<LineRenderer> _lines = new List<LineRenderer>();
        private readonly List<Transform> _flags = new List<Transform>();
        private Vector3[] _posBuffer = new Vector3[8];

        private static Material _lineTemplate;
        private static Material _flagMat;
        private static Mesh _pennantMesh;

        public void Init(SelectionController selection)
        {
            _selection = selection;
            _s = GameplaySettings.Instance;
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (_selection == null) { Deactivate(0, 0); return; }
            if (_cam == null) _cam = Camera.main;

            float camYaw = _cam != null ? _cam.transform.eulerAngles.y : 0f;

            // Опорный юнит группы (минимальный Id — стабильный выбор) и центр текущих позиций.
            // Маршрут рисуем по РЕАЛЬНЫМ точкам кликов (RoutePoints), а не по слотам формации —
            // поэтому флаг стоит ровно там, где кликнули, и не «уезжает» в сторону.
            float now = Time.time;
            SimEntity rep = null;
            UnitView repView = null;
            Vector3 startSum = Vector3.zero;
            int startCount = 0;

            foreach (var v in _selection.Selected)
            {
                if (v == null || v.Entity == null || !v.Entity.Alive) continue;
                var pts = v.RoutePoints;
                if (pts.Count == 0) continue;

                // Срезать пройденные точки по прогрессу sim — но только после grace-окна,
                // иначе свежий приказ (команда ещё не исполнилась) обнулит маршрут.
                if (now - v.RouteFreshTime > _s.RouteFreshGrace)
                {
                    int simLegs = (v.Entity.HasTarget ? 1 : 0) + v.Entity.Waypoints.Count;
                    while (pts.Count > simLegs && pts.Count > 0) pts.RemoveAt(0);
                }
                if (pts.Count == 0) continue;

                startSum += SimConvert.ToWorld(v.Entity.Position);
                startCount++;
                if (rep == null || v.Entity.Id < rep.Id) { rep = v.Entity; repView = v; }
            }

            int lineIdx = 0, flagIdx = 0;
            if (repView != null)
            {
                var pts = repView.RoutePoints;
                Vector3 groupCenter = startSum / startCount;
                int needed = 1 + pts.Count;
                if (_posBuffer.Length < needed) _posBuffer = new Vector3[Mathf.NextPowerOfTwo(needed)];

                int n = 0;
                _posBuffer[n++] = groupCenter + Vector3.up * LineY;
                for (int i = 0; i < pts.Count; i++) AddRoutePoint(pts[i], ref n, ref flagIdx, camYaw);

                if (n >= 2)
                {
                    LineRenderer lr = GetLine(lineIdx++);
                    lr.positionCount = n;
                    lr.SetPositions(_posBuffer);
                    float len = PathLength(n);
                    lr.material.mainTextureScale = new Vector2(Mathf.Max(1f, len / _s.RouteDashLength), 1f);
                }
            }

            Deactivate(lineIdx, flagIdx);
        }

        /// <summary>Добавить точку маршрута в линию и поставить там флажок.</summary>
        private void AddRoutePoint(Vector3 world, ref int n, ref int flagIdx, float camYaw)
        {
            _posBuffer[n++] = new Vector3(world.x, LineY, world.z);
            Transform f = GetFlag(flagIdx++);
            f.position = new Vector3(world.x, 0f, world.z);
            f.rotation = Quaternion.Euler(0f, camYaw, 0f);
        }

        private float PathLength(int count)
        {
            float len = 0f;
            for (int i = 1; i < count; i++) len += Vector3.Distance(_posBuffer[i - 1], _posBuffer[i]);
            return len;
        }

        private void Deactivate(int fromLine, int fromFlag)
        {
            for (int i = fromLine; i < _lines.Count; i++)
                if (_lines[i].positionCount != 0) _lines[i].positionCount = 0;
            for (int i = fromFlag; i < _flags.Count; i++)
                if (_flags[i].gameObject.activeSelf) _flags[i].gameObject.SetActive(false);
        }

        private LineRenderer GetLine(int index)
        {
            while (_lines.Count <= index) _lines.Add(CreateLine());
            return _lines[index];
        }

        private LineRenderer CreateLine()
        {
            var go = new GameObject("Route");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.View;
            lr.textureMode = LineTextureMode.Stretch;
            lr.numCornerVertices = 2;
            lr.widthMultiplier = _s.RouteLineWidth;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.material = new Material(LineTemplate); // свой инстанс — тайлинг у каждой линии свой
            lr.positionCount = 0;
            return lr;
        }

        private Transform GetFlag(int index)
        {
            while (_flags.Count <= index) _flags.Add(CreateFlag());
            Transform f = _flags[index];
            if (!f.gameObject.activeSelf) f.gameObject.SetActive(true);
            return f;
        }

        private Transform CreateFlag()
        {
            var root = new GameObject("RouteFlag");
            root.transform.SetParent(transform, false);

            var pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var poleCol = pole.GetComponent<Collider>();
            if (poleCol != null) Destroy(poleCol);
            pole.transform.SetParent(root.transform, false);
            pole.transform.localScale = new Vector3(0.05f, 1.3f, 0.05f);
            pole.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            var poleR = pole.GetComponent<Renderer>();
            poleR.sharedMaterial = FlagMat;
            poleR.shadowCastingMode = ShadowCastingMode.Off;

            var pennant = new GameObject("Pennant");
            pennant.transform.SetParent(root.transform, false);
            var mf = pennant.AddComponent<MeshFilter>();
            mf.sharedMesh = PennantMesh;
            var mr = pennant.AddComponent<MeshRenderer>();
            mr.sharedMaterial = FlagMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;

            return root.transform;
        }

        // --- Общие ресурсы ---

        private static Material LineTemplate
        {
            get
            {
                if (_lineTemplate != null) return _lineTemplate;
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                var rc = GameplaySettings.Instance.RouteColor;
                _lineTemplate = MakeTransparent(new Material(sh),
                    new Color(rc.r, rc.g, rc.b, GameplaySettings.Instance.RouteLineAlpha));

                Texture2D dash = MakeDashTexture();
                if (_lineTemplate.HasProperty("_BaseMap")) _lineTemplate.SetTexture("_BaseMap", dash);
                _lineTemplate.mainTexture = dash;
                return _lineTemplate;
            }
        }

        private static Material FlagMat
        {
            get
            {
                if (_flagMat != null) return _flagMat;
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                var rc = GameplaySettings.Instance.RouteColor;
                _flagMat = MakeTransparent(new Material(sh),
                    new Color(rc.r, rc.g, rc.b, GameplaySettings.Instance.RouteFlagAlpha));
                return _flagMat;
            }
        }

        private static Material MakeTransparent(Material m, Color c)
        {
            m.SetFloat("_Surface", 1f); // Transparent
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        /// <summary>Текстура штриха: половина непрозрачная, половина прозрачная, повтор по длине.</summary>
        private static Texture2D MakeDashTexture()
        {
            const int w = 16;
            var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };
            var px = new Color32[w];
            for (int i = 0; i < w; i++)
            {
                byte a = (byte)(i < 10 ? 255 : 0); // штрих ~62%, пробел ~38%
                px[i] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Двусторонний треугольный вымпел у верхушки древка.</summary>
        private static Mesh PennantMesh
        {
            get
            {
                if (_pennantMesh != null) return _pennantMesh;
                var mesh = new Mesh { name = "RoutePennant" };
                var verts = new[]
                {
                    new Vector3(0f, 1.30f, 0f),
                    new Vector3(0f, 0.95f, 0f),
                    new Vector3(0.45f, 1.12f, 0f),
                };
                mesh.vertices = verts;
                mesh.triangles = new[] { 0, 2, 1, 0, 1, 2 }; // обе стороны
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                _pennantMesh = mesh;
                return _pennantMesh;
            }
        }
    }
}
