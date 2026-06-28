using Planet.Sim;
using UnityEngine;
using UnityEngine.Rendering;

namespace Planet.Presentation
{
    /// <summary>
    /// Полоса здоровья над юнитом (билборд к камере).
    ///  - показывается, если юнит ранен (ХП не полное) ИЛИ выделен;
    ///  - у выделенного юнита — белая обводка.
    /// Слои (от дальнего к ближнему): обводка (белая) → подложка (тёмная) → заполнение (зелёное).
    /// </summary>
    public sealed class HealthBar : MonoBehaviour
    {
        private const float BackgroundHeight = 0.16f;
        private const float BarHeight = BackgroundHeight; // заливка во всю высоту подложки
        private const float OutlinePaddingX = 0.06f;
        private const float OutlinePaddingY = 0.035f;
        private const float FillInsetX = 0f; // без боковых отступов — заливка во всю ширину

        private SimEntity _entity;
        private float _width;
        private float _heightOffset;
        private bool _selected;
        private Camera _cam;

        private Renderer _outline;
        private Renderer _bg;
        private Renderer _fillRenderer;
        private Transform _fill;

        private static Material _outlineMat;
        private static Material _bgMat;
        private static Material _fillMat;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock _fillProps;

        public void Setup(SimEntity entity, float width, float heightOffset)
        {
            _entity = entity;
            _width = width;
            _heightOffset = heightOffset;
            _cam = Camera.main;
            _fillProps = new MaterialPropertyBlock();

            _outline = CreateBar(OutlineMat,
                new Vector3(_width + OutlinePaddingX, BackgroundHeight + OutlinePaddingY, 0.006f),
                new Vector3(0f, 0f, 0.016f));
            _bg = CreateBar(BgMat,
                new Vector3(_width, BackgroundHeight, 0.008f),
                Vector3.zero);
            _fillRenderer = CreateBar(FillMat,
                new Vector3(Mathf.Max(_width - FillInsetX, 0.05f), BarHeight, 0.01f),
                new Vector3(0f, 0f, -0.012f));
            _fill = _fillRenderer.transform;

            SetRenderers(false);
        }

        public void SetSelected(bool selected) => _selected = selected;

        private Renderer CreateBar(Material mat, Vector3 scale, Vector3 localPos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            go.transform.SetParent(transform, false);
            go.transform.localScale = scale;
            go.transform.localPosition = localPos;

            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        private void LateUpdate()
        {
            if (_entity == null) return;

            float frac = _entity.MaxHp > 0 ? Mathf.Clamp01((float)_entity.Hp / _entity.MaxHp) : 1f;
            bool damaged = frac < 0.999f;
            bool visible = damaged || _selected;

            _bg.enabled = visible;
            _fillRenderer.enabled = visible;
            _outline.enabled = _selected; // белая обводка — только у выделенного

            if (!visible) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            transform.position = transform.parent.position + Vector3.up * _heightOffset;
            transform.rotation = _cam.transform.rotation; // билборд

            float fillWidth = Mathf.Max(_width - FillInsetX, 0.05f);
            Vector3 s = _fill.localScale;
            s.x = fillWidth * frac;
            _fill.localScale = s;
            _fill.localPosition = new Vector3(-fillWidth * (1f - frac) * 0.5f, 0f, _fill.localPosition.z);

            Color hpColor = HealthColor(frac);
            _fillRenderer.GetPropertyBlock(_fillProps);
            _fillProps.SetColor(BaseColorId, hpColor);
            _fillProps.SetColor(ColorId, hpColor);
            _fillRenderer.SetPropertyBlock(_fillProps);
        }

        private void SetRenderers(bool v)
        {
            _outline.enabled = false;
            _bg.enabled = v;
            _fillRenderer.enabled = v;
        }

        private static Material OutlineMat => _outlineMat != null ? _outlineMat : (_outlineMat = MakeUnlit(GameSettings.HealthBar.Outline));
        private static Material BgMat => _bgMat != null ? _bgMat : (_bgMat = MakeUnlit(GameSettings.HealthBar.Background));
        private static Material FillMat => _fillMat != null ? _fillMat : (_fillMat = MakeUnlit(GameSettings.HealthBar.ColorFull));

        private static Color HealthColor(float frac)
        {
            var s = GameSettings.HealthBar;
            if (frac <= s.LowThreshold) return s.ColorLow;
            if (frac <= s.MidThreshold) return s.ColorMid;
            return s.ColorFull;
        }

        private static Material MakeUnlit(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            var m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }
    }
}
