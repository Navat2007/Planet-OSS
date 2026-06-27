using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>Кратковременный маркер точки приказа: зелёный диск, сжимается и исчезает.</summary>
    public sealed class MoveMarker : MonoBehaviour
    {
        private const float Life = 0.5f;
        private const float StartSize = 1.4f;
        private float _t;

        public static void Spawn(Vector3 world)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "MoveMarker";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            go.transform.position = world + Vector3.up * 0.05f;
            go.transform.localScale = new Vector3(StartSize, 0.02f, StartSize);

            var r = go.GetComponent<Renderer>();
            var c = new Color(0.3f, 1f, 0.4f, 1f);
            if (r.material.HasProperty("_BaseColor")) r.material.SetColor("_BaseColor", c);
            if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            go.AddComponent<MoveMarker>();
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float k = _t / Life;
            if (k >= 1f) { Destroy(gameObject); return; }

            float s = Mathf.Lerp(StartSize, 0.3f, k);
            transform.localScale = new Vector3(s, 0.02f, s);
        }
    }
}
