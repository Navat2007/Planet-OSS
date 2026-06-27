using System.Collections.Generic;
using Planet.Sim;
using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Визуальный прокси одной sim-сущности: плавно следует за позицией в симуляции,
    /// разворачивается по движению, умеет показывать кольцо выделения.
    /// Регистрируется в <see cref="Active"/> — по нему работает выделение.
    /// Чисто презентационный: на логику симуляции не влияет.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        /// <summary>Все активные визуалы юнитов (для выделения).</summary>
        public static readonly List<UnitView> Active = new List<UnitView>();

        private const float PositionSmoothing = 12f;
        private const float TurnSpeedDegPerSec = 540f;

        private SimEntity _entity;
        private float _y;
        private float _yawOffset;
        private Vector3 _lastTargetWorld;
        private HealthBar _healthBar;

        public SimEntity Entity => _entity;

        /// <summary>Ключ типа юнита (имя UnitDef) — для выделения «всех такого же типа».</summary>
        public string TypeKey;

        /// <summary>Радиус для кольца выделения, м.</summary>
        public float SelectionRadius = 0.6f;

        public void Bind(SimEntity entity, float y = 0f, float yawOffset = 0f)
        {
            _entity = entity;
            _y = y;
            _yawOffset = yawOffset;
            Vector3 p = SimConvert.ToWorld(entity.Position, y);
            transform.position = p;
            _lastTargetWorld = p;

            _healthBar = CreateHealthBar(); // у каждого юнита; видимость решает сам HealthBar
        }

        private void OnEnable() => Active.Add(this);
        private void OnDisable() => Active.Remove(this);

        /// <summary>Покрасить все рендереры тинтом (различение сторон до team-color шейдера).</summary>
        public void ApplyTint(Color tint)
        {
            var mpb = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (_healthBar != null && r.transform.IsChildOf(_healthBar.transform)) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", tint);
                mpb.SetColor("_Color", tint);
                r.SetPropertyBlock(mpb);
            }
        }

        /// <summary>Отметить юнит выделенным (полоса получает белую обводку).</summary>
        public void SetSelected(bool selected)
        {
            if (_healthBar != null) _healthBar.SetSelected(selected);
        }

        private HealthBar CreateHealthBar()
        {
            var go = new GameObject("HealthBar");
            go.transform.SetParent(transform, false);

            float modelTop = ComputeModelHeight(); // верх модели (по рендерам)
            var hb = go.AddComponent<HealthBar>();
            hb.Setup(_entity, Mathf.Max(SelectionRadius * 2.2f, 1.0f), modelTop + 0.7f);
            return hb;
        }

        /// <summary>Высота модели над основанием (для размещения полосы ХП над юнитом).</summary>
        private float ComputeModelHeight()
        {
            float baseY = transform.position.y;
            float top = baseY;
            bool any = false;
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                float maxY = r.bounds.max.y;
                if (!any) { top = maxY; any = true; }
                else if (maxY > top) top = maxY;
            }
            return any ? Mathf.Max(top - baseY, 0.5f) : 1.8f;
        }

        private void Update()
        {
            if (_entity == null) return;

            if (!_entity.Alive)
            {
                gameObject.SetActive(false); // смерть/обломки — Фаза 6
                return;
            }

            Vector3 target = SimConvert.ToWorld(_entity.Position, _y);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-PositionSmoothing * Time.deltaTime));

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
