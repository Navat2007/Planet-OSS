using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Ставит кастомный курсор игры (аппаратный, через Cursor.SetCursor).
    /// Настраивается в Инспекторе и по умолчанию переживает смену сцен,
    /// поэтому достаточно одного объекта Cursor на старте.
    /// </summary>
    public sealed class CursorController : MonoBehaviour
    {
        [Header("Курсор")]
        [SerializeField] private Texture2D _cursorTexture;
        [Tooltip("Точка клика в пикселях от левого-верхнего угла текстуры (кончик стрелки).")]
        [SerializeField] private Vector2 _hotspot = Vector2.zero;
        [Tooltip("Auto — аппаратный курсор где возможно; ForceSoftware — программный (нужен Read/Write у текстуры).")]
        [SerializeField] private CursorMode _cursorMode = CursorMode.Auto;
        [SerializeField] private bool _persistAcrossScenes = true;

        private static CursorController _instance;

        private void Awake()
        {
            if (_persistAcrossScenes)
            {
                if (_instance != null && _instance != this)
                {
                    Destroy(gameObject); // дубликат при возврате в сцену — убираем
                    return;
                }
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            Apply();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>Применить текущую текстуру курсора. Пустую текстуру игнорируем,
        /// чтобы не перетереть глобальный курсор (см. CursorBootstrap).</summary>
        public void Apply()
        {
            if (_cursorTexture == null) return;
            Cursor.SetCursor(_cursorTexture, _hotspot, _cursorMode);
        }

        /// <summary>Сменить курсор в рантайме (например, на «атаку»/«нельзя» позже).</summary>
        public void SetCursor(Texture2D texture, Vector2 hotspot)
        {
            _cursorTexture = texture;
            _hotspot = hotspot;
            Apply();
        }

        /// <summary>Вернуть системный курсор.</summary>
        public void ResetToSystem()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
