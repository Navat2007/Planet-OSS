using Planet.Sim;
using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Визуальный прокси одной sim-сущности. Каждый кадр плавно подтягивает свой transform
    /// к позиции сущности в симуляции. Чисто презентационный: на логику не влияет.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        private SimEntity _entity;
        private float _y;

        public SimEntity Entity => _entity;

        public void Bind(SimEntity entity, float y = 0f)
        {
            _entity = entity;
            _y = y;
            transform.position = SimConvert.ToWorld(entity.Position, y);
        }

        private void Update()
        {
            if (_entity == null) return;

            if (!_entity.Alive)
            {
                // Смерть/уничтожение визуала разворачивается в Фазе 6; пока просто скрываем.
                gameObject.SetActive(false);
                return;
            }

            Vector3 target = SimConvert.ToWorld(_entity.Position, _y);
            // Визуальная интерполяция (только для глаза, на симуляцию не влияет).
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
    }
}
