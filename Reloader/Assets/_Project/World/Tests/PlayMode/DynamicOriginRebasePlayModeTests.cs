using System.Collections;
using NUnit.Framework;
using Reloader.World.Runtime;
using Reloader.World.Runtime.Origin;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Reloader.World.Tests.PlayMode
{
    public sealed class DynamicOriginRebasePlayModeTests
    {
        private const string BootstrapSceneName = "Bootstrap";
        private const float BaselineWindowToleranceMeters = 0.25f;

        [UnityTest]
        public IEnumerator RuntimePlayer_BeyondThreshold_RebasesToStartupBaselineWithoutBreakingRelativeOffsetsOrCameraIdentity()
        {
            GameObject marker = null;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
                yield return null;

                var bootstrapRoot = Object.FindFirstObjectByType<BootstrapWorldRoot>(FindObjectsInactive.Include);
                Assert.That(bootstrapRoot, Is.Not.Null, "Expected Bootstrap scene to keep the canonical BootstrapWorldRoot loaded.");

                var persistentRoot = BootstrapWorldRoot.Initialize();
                Assert.That(persistentRoot, Is.Not.Null, "Expected Bootstrap runtime initialization to produce a canonical PersistentPlayerRoot.");
                Assert.That(PersistentPlayerRoot.Instance, Is.SameAs(persistentRoot), "Expected one canonical PersistentPlayerRoot instance.");

                var playerRoot = persistentRoot.PlayerRootTransform;
                Assert.That(playerRoot, Is.Not.Null, "Expected a canonical runtime player root before exercising rebase flow.");

                var controller = persistentRoot.GetComponent<DynamicOriginRebaseController>();
                Assert.That(controller, Is.Not.Null, "Expected the persistent runtime owner to keep the canonical DynamicOriginRebaseController seam.");

                yield return WaitForPlayerStartupStabilization(playerRoot);
                var startupHorizontalBaseline = new Vector2(playerRoot.position.x, playerRoot.position.z);

                marker = new GameObject("DynamicOriginRebasePlayModeMarker");
                Assert.That(marker.scene.name, Is.EqualTo(BootstrapSceneName), "Expected the continuity marker to stay in the world scene instead of being moved into the DDOL player scene.");
                Assert.That(marker.scene, Is.Not.EqualTo(playerRoot.gameObject.scene), "Expected the continuity marker to remain outside the canonical player scene.");

                var relativeOffset = new Vector3(3f, 0f, 7f);
                var farLocalPosition = new Vector3(540f, playerRoot.position.y, 32f);
                playerRoot.position = farLocalPosition;
                marker.transform.position = farLocalPosition + relativeOffset;

                yield return null;
                yield return null;
                yield return null;

                Assert.That(controller.LastRebaseTime, Is.Not.EqualTo(float.NegativeInfinity), "Expected the canonical DynamicOriginRebaseController to record a rebase after the runtime player crossed the threshold.");
                AssertReturnsToStartupHorizontalBaseline(
                    playerRoot,
                    startupHorizontalBaseline,
                    "Expected the runtime floating-origin path to return the canonical player to its startup horizontal baseline window after crossing 500m.");

                var actualRelativeOffset = marker.transform.position - playerRoot.position;
                Assert.That(
                    Vector3.Distance(actualRelativeOffset, relativeOffset),
                    Is.LessThanOrEqualTo(0.05f),
                    "Expected rebase to preserve the relative local offset between the DDOL player root and the world-scene marker without reparenting the marker into the player scene.");
                Assert.That(marker.scene.name, Is.EqualTo(BootstrapSceneName), "Expected the world-scene marker to stay in the Bootstrap scene throughout the rebase.");

                Assert.That(Object.FindObjectsByType<PersistentPlayerRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(1));
                AssertSingletonCameraAndAudioIdentity(playerRoot);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                if (marker != null)
                {
                    Object.Destroy(marker);
                }
            }
        }

        private static IEnumerator WaitForPlayerStartupStabilization(Transform playerRoot)
        {
            Assert.That(playerRoot, Is.Not.Null);

            var stableFrameCount = 0;
            var previousPosition = playerRoot.position;
            var elapsed = 0f;
            while (elapsed < 2f && stableFrameCount < 5)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;

                var currentPosition = playerRoot.position;
                var currentHorizontal = new Vector2(currentPosition.x, currentPosition.z);
                var previousHorizontal = new Vector2(previousPosition.x, previousPosition.z);
                if (Vector2.Distance(currentHorizontal, previousHorizontal) <= 0.001f)
                {
                    stableFrameCount++;
                }
                else
                {
                    stableFrameCount = 0;
                }

                previousPosition = currentPosition;
            }

            Assert.That(stableFrameCount, Is.GreaterThanOrEqualTo(5), "Expected runtime player startup placement to settle before exercising floating-origin rebase.");
        }

        private static void AssertReturnsToStartupHorizontalBaseline(Transform playerRoot, Vector2 startupHorizontalBaseline, string message)
        {
            Assert.That(playerRoot, Is.Not.Null);

            var currentHorizontal = new Vector2(playerRoot.position.x, playerRoot.position.z);
            Assert.That(Vector2.Distance(currentHorizontal, startupHorizontalBaseline), Is.LessThanOrEqualTo(BaselineWindowToleranceMeters), message);
        }

        private static void AssertSingletonCameraAndAudioIdentity(Transform playerRoot)
        {
            Assert.That(playerRoot, Is.Not.Null);

            var mainCameraCount = 0;
            var audioListenerCount = 0;
            var gameplayCameraCount = 0;
            var viewmodelCameraCount = 0;
            var cinemachineBrainCount = 0;

            var allTransforms = playerRoot.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < allTransforms.Length; i++)
            {
                var current = allTransforms[i];
                if (current == null)
                {
                    continue;
                }

                if (current.CompareTag("MainCamera"))
                {
                    mainCameraCount++;
                }

                if (current.GetComponent<AudioListener>() != null)
                {
                    audioListenerCount++;
                }

                if (current.name == "ViewmodelCamera" && current.GetComponent<Camera>() != null)
                {
                    viewmodelCameraCount++;
                }

                if (current.GetComponent<Camera>() != null)
                {
                    gameplayCameraCount++;
                }

                if (HasComponentNamed(current.gameObject, "CinemachineBrain"))
                {
                    cinemachineBrainCount++;
                }
            }

            Assert.That(mainCameraCount, Is.EqualTo(1), "Expected exactly one tagged main camera on the canonical runtime player root.");
            Assert.That(audioListenerCount, Is.EqualTo(1), "Expected exactly one AudioListener on the canonical runtime player root.");
            Assert.That(cinemachineBrainCount, Is.EqualTo(1), "Expected exactly one CinemachineBrain on the canonical runtime player root.");
            Assert.That(gameplayCameraCount, Is.GreaterThanOrEqualTo(1), "Expected the runtime player root to keep a gameplay camera after rebase.");
            Assert.That(viewmodelCameraCount, Is.EqualTo(1), "Expected exactly one viewmodel overlay camera on the canonical runtime player root.");
        }

        private static bool HasComponentNamed(GameObject gameObject, string simpleTypeName)
        {
            if (gameObject == null || string.IsNullOrWhiteSpace(simpleTypeName))
            {
                return false;
            }

            var components = gameObject.GetComponents<Component>();
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i].GetType().Name == simpleTypeName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
