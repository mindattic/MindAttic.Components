using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MindAttic.Components.Unity.Tests
{
    /// <summary>A fresh MindAttic canvas, the dark theme and an EventSystem per test; everything torn down after.</summary>
    public abstract class UiTestBase
    {
        protected Canvas canvas;

        [SetUp]
        public void BaseSetUp()
        {
            MaTheme.ReduceMotion = false;
            MaTheme.UseDark();
            MaInput.PointerModality = false;
            MaEventSystem.Ensure();
            canvas = MaCanvas.Create("Test Canvas");
        }

        [TearDown]
        public void BaseTearDown()
        {
            MaModal.CloseAll();
            MaToast.DismissAll();
            if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            MaCanvas.DestroyOverlay();
            foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(es.gameObject);
            MaTheme.UseDark();
            MaTheme.ReduceMotion = false;
        }

        protected static GameObject Selected => MaEventSystem.Selected;

        protected static void AssertColor(Color expected, Color actual, string what = null)
        {
            const float tol = 0.01f;
            bool ok = Mathf.Abs(expected.r - actual.r) < tol && Mathf.Abs(expected.g - actual.g) < tol &&
                      Mathf.Abs(expected.b - actual.b) < tol && Mathf.Abs(expected.a - actual.a) < tol;
            Assert.That(ok, Is.True, (what ?? "colour") + ": expected " + expected + " but was " + actual);
        }

        protected static bool IsWithin(GameObject go, Component root) => go != null && root != null && go.transform.IsChildOf(root.transform);
    }
}
