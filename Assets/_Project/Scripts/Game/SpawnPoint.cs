using UnityEngine;

namespace Planet.Game
{
    /// <summary>
    /// Маркер точки спавна на карте. Расставляется вручную в сцене.
    /// OwnerId: 0 — игрок, 1 — противник.
    /// </summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [Tooltip("0 — игрок, 1 — противник")]
        public int OwnerId = 0;

        private void OnDrawGizmos()
        {
            Color c = OwnerId == 0 ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.4f, 0.3f);
            Gizmos.color = c;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3f);
        }
    }
}
