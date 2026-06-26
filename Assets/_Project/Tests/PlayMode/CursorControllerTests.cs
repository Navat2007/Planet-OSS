using NUnit.Framework;
using Planet.Presentation;
using UnityEngine;

namespace Planet.Tests.PlayMode
{
    public sealed class CursorControllerTests
    {
        [Test]
        public void SetCursor_DoesNotThrow()
        {
            var go = new GameObject("CursorTest");
            var cc = go.AddComponent<CursorController>();

            // null-текстура = системный курсор; не должно бросать исключений.
            Assert.DoesNotThrow(() => cc.SetCursor(null, Vector2.zero));
            Assert.DoesNotThrow(() => cc.ResetToSystem());

            Object.DestroyImmediate(go);
        }
    }
}
