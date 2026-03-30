using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Generation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class MainTownPlaygroundMarkerSyncEditModeTests
    {
        private const string SyncTypeName = "Reloader.NPCs.Editor.MainTownPlaygroundMarkerSync";
        private const string RuntimeBridgeTypeName = "Reloader.NPCs.Runtime.CivilianPopulationRuntimeBridge, Reloader.NPCs";

        [Test]
        public void SyncMarkers_CreatesUpdatesAndRemovesPlaygroundMarkers_Idempotently()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var definition = ScriptableObject.CreateInstance<MainTownPopulationDefinition>();
            var runtimeRoot = new GameObject("MainTownPopulationRuntime");

            try
            {
                var squareAnchor = CreateAnchor(runtimeRoot.transform, "Anchor_Playground_Square_01", new Vector3(5f, 0f, 2f), Quaternion.Euler(0f, 25f, 0f));
                var watchAnchor = CreateAnchor(runtimeRoot.transform, "Anchor_Playground_Watch_01", new Vector3(8f, 0f, 4f), Quaternion.identity);

                definition.Pools = new[]
                {
                    new MainTownPopulationPoolDefinition
                    {
                        PoolId = "townsfolk",
                        Slots = new[]
                        {
                            CreateSlot("townsfolk.playground.square.001", "townsfolk", "maintown.playground.square", squareAnchor.name, MainTownPopulationHabitat.Town)
                        }
                    },
                    new MainTownPopulationPoolDefinition
                    {
                        PoolId = "cops",
                        Slots = new[]
                        {
                            CreateSlot("cops.playground.watch.001", "cops", "maintown.playground.watch", watchAnchor.name, MainTownPopulationHabitat.Town)
                        }
                    }
                };

                var existingMarker = CreateMarker(
                    runtimeRoot.transform,
                    "WrongSquareMarkerName",
                    "maintown.playground.square",
                    "wrong_pool",
                    MainTownPopulationHabitat.Forest,
                    "Anchor_Wrong",
                    new Vector3(-10f, 0.5f, 4f),
                    Quaternion.Euler(0f, 90f, 0f));

                var staleMarker = CreateMarker(
                    runtimeRoot.transform,
                    "PlaygroundZoneMarker_Stale",
                    "maintown.playground.stale",
                    "hobos",
                    MainTownPopulationHabitat.Forest,
                    "Anchor_Playground_Stale_01",
                    new Vector3(99f, 0f, 99f),
                    Quaternion.identity);

                var syncMethod = ResolveRequiredSyncMethod();

                syncMethod.Invoke(null, new object[] { definition, runtimeRoot });

                var markers = runtimeRoot.GetComponentsInChildren<MainTownPlaygroundZoneMarker>(true)
                    .OrderBy(marker => marker.AreaTag, StringComparer.Ordinal)
                    .ToArray();

                Assert.That(markers.Length, Is.EqualTo(2), "Expected exact sync to leave one marker per authored playground slot.");
                Assert.That(staleMarker == null, Is.True, "Expected stale playground marker to be removed.");
                Assert.That(scene.isDirty, Is.True, "Expected sync to mark the scene dirty after mutations.");

                AssertMarkerMatches(markers.Single(marker => marker.AreaTag == "maintown.playground.square"), "PlaygroundZoneMarker_Square", "townsfolk", MainTownPopulationHabitat.Town, squareAnchor);
                AssertMarkerMatches(markers.Single(marker => marker.AreaTag == "maintown.playground.watch"), "PlaygroundZoneMarker_Watch", "cops", MainTownPopulationHabitat.Town, watchAnchor);

                var firstPassMarkerIds = markers.Select(marker => marker.GetInstanceID()).OrderBy(id => id).ToArray();

                syncMethod.Invoke(null, new object[] { definition, runtimeRoot });

                var secondPassMarkers = runtimeRoot.GetComponentsInChildren<MainTownPlaygroundZoneMarker>(true)
                    .OrderBy(marker => marker.AreaTag, StringComparer.Ordinal)
                    .ToArray();
                var secondPassMarkerIds = secondPassMarkers.Select(marker => marker.GetInstanceID()).OrderBy(id => id).ToArray();

                Assert.That(secondPassMarkers.Length, Is.EqualTo(2), "Expected sync to stay idempotent on a second run.");
                CollectionAssert.AreEqual(firstPassMarkerIds, secondPassMarkerIds, "Expected second sync run to reuse existing marker objects.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void SyncActiveScene_UsesBridgeAssignedPopulationDefinition()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var definition = ScriptableObject.CreateInstance<MainTownPopulationDefinition>();
            var runtimeRoot = new GameObject("MainTownPopulationRuntime");

            try
            {
                var bridgeType = ResolveRequiredType(RuntimeBridgeTypeName);
                var bridge = runtimeRoot.AddComponent(bridgeType);
                SetField(bridge, "_populationDefinition", definition);

                var squareAnchor = CreateAnchor(runtimeRoot.transform, "Anchor_Playground_Square_01", new Vector3(3f, 0f, 7f), Quaternion.Euler(0f, 10f, 0f));
                definition.Pools = new[]
                {
                    new MainTownPopulationPoolDefinition
                    {
                        PoolId = "townsfolk",
                        Slots = new[]
                        {
                            CreateSlot("townsfolk.playground.square.001", "townsfolk", "maintown.playground.square", squareAnchor.name, MainTownPopulationHabitat.Town)
                        }
                    }
                };

                CreateMarker(
                    runtimeRoot.transform,
                    "PlaygroundZoneMarker_Stale",
                    "maintown.playground.stale",
                    "hobos",
                    MainTownPopulationHabitat.Forest,
                    "Anchor_Playground_Stale_01",
                    new Vector3(44f, 0f, 12f),
                    Quaternion.identity);

                var syncMethod = ResolveRequiredSyncActiveSceneMethod();
                syncMethod.Invoke(null, null);

                var markers = runtimeRoot.GetComponentsInChildren<MainTownPlaygroundZoneMarker>(true);
                Assert.That(markers.Length, Is.EqualTo(1), "Expected SyncActiveScene to rebuild markers from the bridge population definition.");
                AssertMarkerMatches(markers[0], "PlaygroundZoneMarker_Square", "townsfolk", MainTownPopulationHabitat.Town, squareAnchor);
                Assert.That(scene.isDirty, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void AssertMarkerMatches(
            MainTownPlaygroundZoneMarker marker,
            string expectedName,
            string expectedPoolId,
            MainTownPopulationHabitat expectedHabitat,
            Transform expectedAnchor)
        {
            Assert.That(marker.name, Is.EqualTo(expectedName));
            Assert.That(marker.PrimaryPoolId, Is.EqualTo(expectedPoolId));
            Assert.That(marker.Habitat, Is.EqualTo(expectedHabitat));
            Assert.That(marker.AnchorId, Is.EqualTo(expectedAnchor.name));
            Assert.That(marker.transform.position.x, Is.EqualTo(expectedAnchor.position.x).Within(0.001f));
            Assert.That(marker.transform.position.y, Is.EqualTo(expectedAnchor.position.y).Within(0.001f));
            Assert.That(marker.transform.position.z, Is.EqualTo(expectedAnchor.position.z).Within(0.001f));
            Assert.That(Quaternion.Angle(marker.transform.rotation, expectedAnchor.rotation), Is.LessThan(0.001f));
        }

        private static MainTownPopulationSlotDefinition CreateSlot(
            string slotId,
            string poolId,
            string areaTag,
            string anchorId,
            MainTownPopulationHabitat habitat)
        {
            return new MainTownPopulationSlotDefinition
            {
                PopulationSlotId = slotId,
                PoolId = poolId,
                AreaTag = areaTag,
                SpawnAnchorId = anchorId,
                Habitat = habitat
            };
        }

        private static Transform CreateAnchor(Transform root, string name, Vector3 localPosition, Quaternion localRotation)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(root, worldPositionStays: false);
            anchor.localPosition = localPosition;
            anchor.localRotation = localRotation;
            return anchor;
        }

        private static MainTownPlaygroundZoneMarker CreateMarker(
            Transform root,
            string name,
            string areaTag,
            string primaryPoolId,
            MainTownPopulationHabitat habitat,
            string anchorId,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            var markerObject = new GameObject(name);
            markerObject.transform.SetParent(root, worldPositionStays: false);
            markerObject.transform.localPosition = localPosition;
            markerObject.transform.localRotation = localRotation;

            var marker = markerObject.AddComponent<MainTownPlaygroundZoneMarker>();
            SetField(marker, "_areaTag", areaTag);
            SetField(marker, "_primaryPoolId", primaryPoolId);
            SetField(marker, "_habitat", habitat);
            SetField(marker, "_anchorId", anchorId);
            return marker;
        }

        private static MethodInfo ResolveRequiredSyncMethod()
        {
            var syncType = ResolveRequiredType(SyncTypeName);

            var method = syncType!.GetMethod(
                "SyncMarkers",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(MainTownPopulationDefinition), typeof(GameObject) },
                modifiers: null);

            Assert.That(method, Is.Not.Null, "Expected public static SyncMarkers(MainTownPopulationDefinition, GameObject) test seam.");
            return method!;
        }

        private static MethodInfo ResolveRequiredSyncActiveSceneMethod()
        {
            var syncType = ResolveRequiredType(SyncTypeName);

            var method = syncType!.GetMethod(
                "SyncActiveScene",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

            Assert.That(method, Is.Not.Null, "Expected public static SyncActiveScene() entrypoint.");
            return method!;
        }

        private static Type ResolveRequiredType(string assemblyQualifiedName)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(assemblyQualifiedName, throwOnError: false))
                .FirstOrDefault(candidate => candidate != null) ??
                Type.GetType(assemblyQualifiedName, throwOnError: false);

            Assert.That(type, Is.Not.Null, $"Expected type '{assemblyQualifiedName}' to exist.");
            return type!;
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on '{instance.GetType().FullName}'.");
            field!.SetValue(instance, value);
        }
    }
}
