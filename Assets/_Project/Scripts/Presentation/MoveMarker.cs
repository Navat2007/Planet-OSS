using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>Кратковременный маркер точки приказа: диск, сжимается и исчезает. Цвет/размер/время — в настройках.</summary>
    public sealed class MoveMarker : MonoBehaviour
    {
        private float _life = 0.5f;
        private float _startSize = 1.4f;
        private float _t;

        public static void Spawn(Vector3 world)
        {
            var s = GameplaySettings.Instance;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "MoveMarker";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            go.transform.position = world + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(s.MarkerStartSize, 0.02f, s.MarkerStartSize);

            var r = go.GetComponent<Renderer>();
            var c = s.MarkerColor;
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c);
            if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var marker = go.AddComponent<MoveMarker>();
            marker._life = s.MarkerLife;
            marker._startSize = s.MarkerStartSize;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float k = _t / _life;
            if (k >= 1f) { Destroy(gameObject); return; }

            float size = Mathf.Lerp(_startSize, 0.3f, k);
            transform.localScale = new Vector3(size, 0.02f, size);
        }
    }
}
