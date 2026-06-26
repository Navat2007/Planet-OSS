using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Глобальные настройки курсора игры. Единый источник правды.
    /// Лежит в Resources и применяется на старте (см. <see cref="CursorBootstrap"/>),
    /// поэтому кастомный курсор активен во всех сценах, включая меню.
    /// </summary>
    [CreateAssetMenu(menuName = "Planet/Cursor Settings", fileName = "CursorSettings")]
    public sealed class CursorSettings : ScriptableObject
    {
        [Tooltip("Текстура курсора. Импортировать как Texture Type = Cursor.")]
        public Texture2D Texture;

        [Tooltip("Точка клика в пикселях от левого-верхнего угла текстуры (кончик стрелки).")]
        public Vector2 Hotspot = Vector2.zero;

        [Tooltip("Auto — аппаратный курсор; ForceSoftware — программный (нужен Read/Write у текстуры).")]
        public CursorMode Mode = CursorMode.Auto;

        /// <summary>Применить курсор. При пустой текстуре не трогаем системный курсор.</summary>
        public void Apply()
        {
            if (Texture == null) return;
            Cursor.SetCursor(Texture, Hotspot, Mode);
        }
    }
}
