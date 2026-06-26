using NUnit.Framework;
using Planet.Presentation;
using UnityEngine;

namespace Planet.Tests.PlayMode
{
    /// <summary>
    /// Тесты логики RTS-камеры (Фаза 2). Дёргаем публичные методы Pan/Zoom/Rotate напрямую,
    /// без эмуляции устройств ввода. Значения границ/зума совпадают с дефолтами RtsCamera.
    /// </summary>
    public sealed class RtsCameraTests
    {
        private const float MapExtent = 50f;   // _mapMax по умолчанию
        private const float MinDistance = 12f; // _minDistance по умолчанию
        private const float MaxDistance = 55f; // _maxDistance по умолчанию
        private const float Eps = 0.01f;

        private RtsCamera _cam;

        [SetUp]
        public void Setup()
        {
            var go = new GameObject("RtsCameraTest");
            _cam = go.AddComponent<RtsCamera>();
            _cam.Initialize(Vector3.zero, new Vector2(-MapExtent, -MapExtent), new Vector2(MapExtent, MapExtent));
        }

        [TearDown]
        public void Teardown()
        {
            if (_cam != null) Object.DestroyImmediate(_cam.gameObject);
        }

        [Test]
        public void Pan_Right_MovesPivotAlongPositiveX()
        {
            _cam.Pan(new Vector2(1f, 0f), 0.5f);
            Assert.Greater(_cam.Pivot.x, 0f);
            Assert.AreEqual(0f, _cam.Pivot.z, Eps);
        }

        [Test]
        public void Pan_ClampsToMapBounds()
        {
            for (int i = 0; i < 100; i++) _cam.Pan(new Vector2(1f, 0f), 1f);
            Assert.AreEqual(MapExtent, _cam.Pivot.x, Eps, "Pivot не должен выходить за границу карты.");
        }

        [Test]
        public void Zoom_In_ClampsTargetAtMinDistance()
        {
            for (int i = 0; i < 100; i++) _cam.Zoom(1f);
            Assert.AreEqual(MinDistance, _cam.TargetDistance, Eps);
        }

        [Test]
        public void Zoom_Out_ClampsTargetAtMaxDistance()
        {
            for (int i = 0; i < 100; i++) _cam.Zoom(-1f);
            Assert.AreEqual(MaxDistance, _cam.TargetDistance, Eps);
        }

        [Test]
        public void Zoom_Smoothing_ConvergesToTarget()
        {
            for (int i = 0; i < 100; i++) _cam.Zoom(1f);            // целимся в min
            for (int i = 0; i < 300; i++) _cam.UpdateZoomSmoothing(0.1f);
            Assert.AreEqual(MinDistance, _cam.Distance, Eps,
                "Текущее расстояние должно плавно дойти до целевого.");
        }

        [Test]
        public void Rotate_ChangesYaw()
        {
            float before = _cam.Yaw;
            _cam.Rotate(1f, 1f);
            Assert.AreNotEqual(before, _cam.Yaw);
        }

        [Test]
        public void PanByScreenOffset_MovesPivotTowardOffset()
        {
            _cam.PanByScreenOffset(new Vector2(100f, 0f), 1f);
            Assert.Greater(_cam.Pivot.x, 0f, "Смещение курсора вправо двигает камеру по +X.");
            Assert.AreEqual(0f, _cam.Pivot.z, Eps);
        }

        [Test]
        public void PanByScreenOffset_ClampsToMapBounds()
        {
            for (int i = 0; i < 2000; i++) _cam.PanByScreenOffset(new Vector2(100f, 0f), 1f);
            Assert.AreEqual(MapExtent, _cam.Pivot.x, Eps, "СКМ-панорама не должна выводить за границу карты.");
        }

        [Test]
        public void Initialize_PlacesRigAboveAndBehindPivot()
        {
            // Камера должна стоять выше уровня земли и позади точки фокуса (отрицательный Z при yaw=0).
            Assert.Greater(_cam.transform.position.y, 0f, "Камера должна быть над землёй.");
            Assert.Less(_cam.transform.position.z, 0f, "При yaw=0 камера должна быть позади точки фокуса.");
        }
    }
}
