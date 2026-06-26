using UnityEngine;

namespace Planet.Presentation
{
    /// <summary>
    /// Драйвер анимации юнита (Presentation). Меряет реальную скорость своего transform
    /// (его двигает <see cref="UnitView"/>) и переключает Animator между idle и run.
    /// Root motion выключен — движение задаёт симуляция, анимация только «крутит ноги».
    /// </summary>
    public sealed class UnitAnimator : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private string _movingParam = "Moving";
        [Tooltip("Порог скорости (м/с), выше которого играет run.")]
        [SerializeField] private float _moveThreshold = 0.1f;

        private int _movingHash;
        private Vector3 _lastPos;

        private void Awake()
        {
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_animator != null) _animator.applyRootMotion = false;
            _movingHash = Animator.StringToHash(_movingParam);
            _lastPos = transform.position;
        }

        private void Update()
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;

            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float speed = (transform.position - _lastPos).magnitude / dt;
            _lastPos = transform.position;

            _animator.SetBool(_movingHash, speed > _moveThreshold);
        }
    }
}
