using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Глобальные настройки boost-курсора (СКМ-ускорение камеры). Единый источник правды.
    /// Лежит в Resources и применяется на старте (см. <see cref="BoostCursorBootstrap"/>),
    /// поэтому boost-курсор работает во всех игровых сценах без ручной настройки.
    /// </summary>
    [CreateAssetMenu(menuName = "Planet/Boost Cursor Settings", fileName = "BoostCursorSettings")]
    public sealed class BoostCursorSettings : ScriptableObject
    {
        [Tooltip("Стрелка, указывающая ВВЕРХ (CursorBoost2). Импорт — Texture Type = Cursor, Read/Write включён.")]
        public Texture2D Texture;

        [Tooltip("Размер значка на экране, пиксели.")]
        public Vector2 Size = new Vector2(48f, 48f);
    }
}
