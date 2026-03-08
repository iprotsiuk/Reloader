using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloader.World.Tests.PlayMode
{
    public class MainTownPopulationInfrastructurePlayModeTests
    {
        private const string MainTownSceneName = "MainTown";
        private const float SceneSwitchTimeoutSeconds = 8f;

        [UnityTest]
        public IEnumerator MainTownPopulationRuntime_HasPopulationDefinitionAndStarterPoolsConfigured()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var root = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(root, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = root!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            var bridgeType = typeof(CivilianPopulationRuntimeBridge);
            var definitionField = bridgeType.GetField("_populationDefinition", BindingFlags.Instance | BindingFlags.NonPublic);
            var libraryField = bridgeType.GetField("_appearanceLibrary", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(definitionField, Is.Not.Null);
            Assert.That(libraryField, Is.Not.Null);

            var definition = definitionField!.GetValue(bridge);
            Assert.That(definition, Is.Not.Null, "Expected MainTown population definition asset to be assigned.");

            var validateMethod = definition!.GetType().GetMethod("Validate", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(validateMethod, Is.Not.Null);
            Assert.DoesNotThrow(() => validateMethod!.Invoke(definition, null));

            var poolsProperty = definition.GetType().GetProperty("Pools", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(poolsProperty, Is.Not.Null);
            var pools = poolsProperty!.GetValue(definition) as System.Array;
            Assert.That(pools, Is.Not.Null);
            Assert.That(pools!.Length, Is.EqualTo(4), "Expected conservative starter pool count for MainTown.");

            var poolIds = pools.Cast<object>()
                .Select(pool => pool.GetType().GetProperty("PoolId", BindingFlags.Instance | BindingFlags.Public)!.GetValue(pool) as string)
                .ToArray();

            CollectionAssert.AreEquivalent(new[] { "townsfolk", "quarry_workers", "hobos", "cops" }, poolIds);

            var library = libraryField!.GetValue(bridge);
            Assert.That(library, Is.Not.Null, "Expected starter appearance library data to be serialized on the bridge.");

            AssertArrayConfigured(library!, "BaseBodyIds");
            AssertArrayConfigured(library!, "PresentationTypes");
            AssertArrayConfigured(library!, "HairIds");
            AssertArrayConfigured(library!, "HairColorIds");
            AssertArrayConfigured(library!, "BeardIds");
            AssertArrayConfigured(library!, "OutfitTopIds");
            AssertArrayConfigured(library!, "OutfitBottomIds");
            AssertArrayConfigured(library!, "OuterwearIds");
        }

        private static void AssertArrayConfigured(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}'.");
            var values = property!.GetValue(instance) as System.Array;
            Assert.That(values, Is.Not.Null, $"Expected '{propertyName}' to be an array.");
            Assert.That(values!.Length, Is.GreaterThan(0), $"Expected '{propertyName}' to have at least one configured value.");
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            var elapsed = 0f;
            while (elapsed < SceneSwitchTimeoutSeconds)
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded && activeScene.name == sceneName)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail($"Timed out waiting for active scene '{sceneName}'.");
        }
    }
}
