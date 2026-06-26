using Planet.Sim;
using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Визуальный прокси одной sim-сущности. Каждый кадр плавно подтягивает свой transform
    /// к позиции сущности в симуляции и разворачивается по направлению движения.
    /// Чисто презентационный: на логику симуляции не влияет.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        private const float PositionSmoothing = 12f;
        private const float TurnSpeedDegPerSec = 540f;

        private SimEntity _entity;
        private float _y;
        private float _yawOffset;
        private Vector3 _lastTargetWorld;

        public SimEntity Entity => _entity;

        public void Bind(SimEntity entity, float y = 0f, float yawOffset = 0f)
        {
            _entity = entity;
            _y = y;
            _yawOffset = yawOffset;
            Vector3 p = SimConvert.ToWorld(entity.Position, y);
            transform.position = p;
            _lastTargetWorld = p;
        }

        /// <summary>Покрасить все рендереры тинтом (различение сторон до полноценного team-color шейдера).</summary>
        public void ApplyTint(Color tint)
        {
            var mpb = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", tint); // URP Lit
                mpb.SetColor("_Color", tint);     // Built-in/legacy — лишнее свойство MPB игнорирует
                r.SetPropertyBlock(mpb);
            }
        }

        private void Update()
        {
            if (_entity == null) return;

            if (!_entity.Alive)
            {
                gameObject.SetActive(false); // смерть/обломки развернём в Фазе 6
                return;
            }

            Vector3 target = SimConvert.ToWorld(_entity.Position, _y);

            // Плавная интерполяция позиции (только для глаза).
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-PositionSmoothing * Time.deltaTime));

            // Разворот по направлению движения.
            Vector3 move = target - _lastTargetWorld;
            move.y = 0f;
            if (move.sqrMagnitude > 1e-6f)
            {
                float yaw = Mathf.Atan2(move.x, move.z) * Mathf.Rad2Deg + _yawOffset;
                Quaternion want = Quaternion.Euler(0f, yaw, 0f);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, want, TurnSpeedDegPerSec * Time.deltaTime);
            }
            _lastTargetWorld = target;
        }
    }
}
