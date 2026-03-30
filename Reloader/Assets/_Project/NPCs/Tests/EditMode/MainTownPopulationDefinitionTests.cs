using System;
using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Reloader.NPCs.Tests.EditMode
{
    public class MainTownPopulationDefinitionTests
    {
        private const string MainTownPopulationAssetPath = "Assets/_Project/NPCs/Data/Population/MainTownPopulationDefinition.asset";
        private const string MainTownScenePath = "Assets/_Project/World/Scenes/MainTown.unity";
        private const string DefinitionTypeName = "Reloader.NPCs.Generation.MainTownPopulationDefinition, Reloader.NPCs";
        private const string PoolTypeName = "Reloader.NPCs.Generation.MainTownPopulationPoolDefinition, Reloader.NPCs";
        private const string SlotTypeName = "Reloader.NPCs.Generation.MainTownPopulationSlotDefinition, Reloader.NPCs";
        private const string HabitatTypeName = "Reloader.NPCs.Generation.MainTownPopulationHabitat, Reloader.NPCs";
        private const string MarkerTypeName = "Reloader.NPCs.Generation.MainTownPlaygroundZoneMarker, Reloader.NPCs";

        [Test]
        public void Validate_WhenPoolsAreEmpty_ThrowsArgumentException()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);

            var definition = ScriptableObject.CreateInstance(definitionType);
            SetProperty(definition, "Pools", Array.CreateInstance(poolType, 0));

            var validate = definitionType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validate, Is.Not.Null, "Expected a public Validate() method on MainTownPopulationDefinition.");

            var ex = Assert.Throws<TargetInvocationException>(() => validate.Invoke(definition, null));
            Assert.That(ex?.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(ex?.InnerException?.Message, Does.Contain("at least one pool"));

            UnityEngine.Object.DestroyImmediate(definition);
        }

        [Test]
        public void Validate_WhenPopulationSlotIdsDuplicate_ThrowsArgumentException()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);
            var habitatType = ResolveRequiredType(HabitatTypeName);

            var definition = ScriptableObject.CreateInstance(definitionType);
            var pool = Activator.CreateInstance(poolType);
            var slotA = CreateSlot(slotType, habitatType, "quarry.worker.001", "quarry_workers", "quarry", "spawn.quarry.a", "Quarry", false);
            var slotB = CreateSlot(slotType, habitatType, "quarry.worker.001", "quarry_workers", "quarry", "spawn.quarry.b", "Quarry", false);

            SetProperty(pool, "PoolId", "quarry_workers");
            SetProperty(pool, "Slots", Array.CreateInstance(slotType, 2));
            var slots = (Array)GetProperty<object>(pool, "Slots");
            slots.SetValue(slotA, 0);
            slots.SetValue(slotB, 1);

            SetProperty(definition, "Pools", Array.CreateInstance(poolType, 1));
            var pools = (Array)GetProperty<object>(definition, "Pools");
            pools.SetValue(pool, 0);

            var validate = definitionType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validate, Is.Not.Null, "Expected a public Validate() method on MainTownPopulationDefinition.");

            var ex = Assert.Throws<TargetInvocationException>(() => validate.Invoke(definition, null));
            Assert.That(ex?.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(ex?.InnerException?.Message, Does.Contain("populationSlotId"));

            UnityEngine.Object.DestroyImmediate(definition);
        }

        [Test]
        public void Validate_WhenVendorPoolIsProtected_AcceptsStableVendorSlots()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);
            var habitatType = ResolveRequiredType(HabitatTypeName);

            var definition = ScriptableObject.CreateInstance(definitionType);
            var vendorPool = Activator.CreateInstance(poolType);
            var vendorSlot = CreateSlot(slotType, habitatType, "vendor.weapon.001", "vendors", "market", "spawn.vendor.weapon", "Town", true);

            SetProperty(vendorPool, "PoolId", "vendors");
            SetProperty(vendorPool, "Slots", Array.CreateInstance(slotType, 1));
            var slots = (Array)GetProperty<object>(vendorPool, "Slots");
            slots.SetValue(vendorSlot, 0);

            SetProperty(definition, "Pools", Array.CreateInstance(poolType, 1));
            var pools = (Array)GetProperty<object>(definition, "Pools");
            pools.SetValue(vendorPool, 0);

            var validate = definitionType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validate, Is.Not.Null, "Expected a public Validate() method on MainTownPopulationDefinition.");

            Assert.DoesNotThrow(() => validate.Invoke(definition, null));

            UnityEngine.Object.DestroyImmediate(definition);
        }

        [Test]
        public void Validate_WhenSlotHabitatUsesSelectorValue_ThrowsArgumentException()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);
            var habitatType = ResolveRequiredType(HabitatTypeName);

            var definition = ScriptableObject.CreateInstance(definitionType);
            var pool = Activator.CreateInstance(poolType);
            var slot = CreateSlot(slotType, habitatType, "townsfolk.001", "townsfolk", "downtown", "spawn.mainstreet.a", "Any", false);

            SetProperty(pool, "PoolId", "townsfolk");
            SetProperty(pool, "Slots", Array.CreateInstance(slotType, 1));
            var slots = (Array)GetProperty<object>(pool, "Slots");
            slots.SetValue(slot, 0);

            SetProperty(definition, "Pools", Array.CreateInstance(poolType, 1));
            var pools = (Array)GetProperty<object>(definition, "Pools");
            pools.SetValue(pool, 0);

            var validate = definitionType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validate, Is.Not.Null, "Expected a public Validate() method on MainTownPopulationDefinition.");

            var ex = Assert.Throws<TargetInvocationException>(() => validate.Invoke(definition, null));
            Assert.That(ex?.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(ex?.InnerException?.Message, Does.Contain("habitat"));

            UnityEngine.Object.DestroyImmediate(definition);
        }

        [Test]
        public void Validate_WhenPlaygroundAreaTagsDuplicate_ThrowsArgumentException()
        {
            var definitionType = ResolveRequiredType(DefinitionTypeName);
            var poolType = ResolveRequiredType(PoolTypeName);
            var slotType = ResolveRequiredType(SlotTypeName);
            var habitatType = ResolveRequiredType(HabitatTypeName);

            var definition = ScriptableObject.CreateInstance(definitionType);
            var townsfolkPool = Activator.CreateInstance(poolType);
            var copsPool = Activator.CreateInstance(poolType);
            var squareTownsfolkSlot = CreateSlot(slotType, habitatType, "townsfolk.playground.square.001", "townsfolk", "maintown.playground.square", "Anchor_Playground_Square_01", "Town", false);
            var squareCopSlot = CreateSlot(slotType, habitatType, "cops.playground.square.001", "cops", "maintown.playground.square", "Anchor_Playground_Watch_01", "Town", false);

            SetProperty(townsfolkPool, "PoolId", "townsfolk");
            SetProperty(townsfolkPool, "Slots", Array.CreateInstance(slotType, 1));
            ((Array)GetProperty<object>(townsfolkPool, "Slots")).SetValue(squareTownsfolkSlot, 0);

            SetProperty(copsPool, "PoolId", "cops");
            SetProperty(copsPool, "Slots", Array.CreateInstance(slotType, 1));
            ((Array)GetProperty<object>(copsPool, "Slots")).SetValue(squareCopSlot, 0);

            SetProperty(definition, "Pools", Array.CreateInstance(poolType, 2));
            var pools = (Array)GetProperty<object>(definition, "Pools");
            pools.SetValue(townsfolkPool, 0);
            pools.SetValue(copsPool, 1);

            var validate = definitionType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validate, Is.Not.Null, "Expected a public Validate() method on MainTownPopulationDefinition.");

            var ex = Assert.Throws<TargetInvocationException>(() => validate.Invoke(definition, null));
            Assert.That(ex?.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(ex?.InnerException?.Message, Does.Contain("playground areaTag"));

            UnityEngine.Object.DestroyImmediate(definition);
        }

        [Test]
        public void Validate_WhenLoadingSerializedMainTownPopulationAsset_DoesNotThrow()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(MainTownPopulationAssetPath);
            Assert.That(asset, Is.Not.Null, $"Expected population asset at '{MainTownPopulationAssetPath}'.");

            var validate = asset.GetType().GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance);
            Assert.That(validate, Is.Not.Null, "Expected a public Validate() method on MainTownPopulationDefinition.");

            Assert.DoesNotThrow(() => validate.Invoke(asset, null));
        }

        [Test]
        public void MainTownPlaygroundMarkers_MatchAuthoredPlaygroundPopulationSlots()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(MainTownPopulationAssetPath);
            Assert.That(asset, Is.Not.Null, $"Expected population asset at '{MainTownPopulationAssetPath}'.");

            var markerType = ResolveRequiredType(MarkerTypeName);
            var expectedByAreaTag = BuildExpectedPlaygroundSlotsByAreaTag(asset!);
            Assert.That(expectedByAreaTag.Count, Is.EqualTo(5), "Expected five authored playground slots in the MainTown population asset.");

            var scene = EditorSceneManager.OpenScene(MainTownScenePath, OpenSceneMode.Additive);
            try
            {
                var root = scene.GetRootGameObjects().SingleOrDefault(gameObject => gameObject.name == "MainTownPopulationRuntime");
                Assert.That(root, Is.Not.Null, "Expected MainTownPopulationRuntime root in MainTown scene.");

                var markers = root!.GetComponentsInChildren(markerType, true);
                Assert.That(markers.Length, Is.EqualTo(expectedByAreaTag.Count), "Expected one scene marker per authored playground slot.");

                for (var i = 0; i < markers.Length; i++)
                {
                    var marker = markers[i];
                    Assert.That(marker, Is.Not.Null);

                    var areaTag = GetProperty<string>(marker, "AreaTag");
                    Assert.That(expectedByAreaTag.TryGetValue(areaTag, out var expectedSlot), Is.True,
                        $"Unexpected playground marker areaTag '{areaTag}'.");

                    Assert.That(GetProperty<string>(marker, "PrimaryPoolId"), Is.EqualTo(expectedSlot.PoolId));
                    Assert.That(GetProperty<object>(marker, "Habitat"), Is.EqualTo(expectedSlot.Habitat));
                    Assert.That(GetProperty<string>(marker, "AnchorId"), Is.EqualTo(expectedSlot.SpawnAnchorId));

                    var markerComponent = marker as Component;
                    Assert.That(markerComponent, Is.Not.Null, "Expected playground marker to be a Component.");

                    var anchor = root.transform.Find(expectedSlot.SpawnAnchorId);
                    Assert.That(anchor, Is.Not.Null, $"Expected anchor '{expectedSlot.SpawnAnchorId}' for playground marker '{areaTag}'.");
                    Assert.That(markerComponent!.transform.position.x, Is.EqualTo(anchor!.position.x).Within(0.001f),
                        $"Expected marker '{areaTag}' to stay aligned with anchor '{expectedSlot.SpawnAnchorId}' on X.");
                    Assert.That(markerComponent.transform.position.y, Is.EqualTo(anchor.position.y).Within(0.001f),
                        $"Expected marker '{areaTag}' to stay aligned with anchor '{expectedSlot.SpawnAnchorId}' on Y.");
                    Assert.That(markerComponent.transform.position.z, Is.EqualTo(anchor.position.z).Within(0.001f),
                        $"Expected marker '{areaTag}' to stay aligned with anchor '{expectedSlot.SpawnAnchorId}' on Z.");
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void MainTownPlaygroundZoneMarker_OnValidate_SnapsToAnchorAndClampsGizmoSize()
        {
            var markerType = ResolveRequiredType(MarkerTypeName);
            var root = new GameObject("MainTownPopulationRuntime");
            var anchor = new GameObject("Anchor_Test").transform;
            var markerObject = new GameObject("PlaygroundMarker");

            try
            {
                root.transform.position = new Vector3(10f, 20f, 30f);
                anchor.SetParent(root.transform, worldPositionStays: false);
                anchor.localPosition = new Vector3(1f, 2f, 3f);
                anchor.localRotation = Quaternion.Euler(0f, 45f, 0f);

                markerObject.transform.SetParent(root.transform, worldPositionStays: false);
                markerObject.transform.localPosition = new Vector3(-5f, 9f, 12f);
                markerObject.transform.localRotation = Quaternion.Euler(15f, 25f, 35f);

                var marker = markerObject.AddComponent(markerType);
                SetField(marker, "_anchorId", "Anchor_Test");
                SetField(marker, "_gizmoSize", new Vector3(0.1f, 0.2f, 0.3f));

                var validate = markerType.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(validate, Is.Not.Null, "Expected a private OnValidate() method on MainTownPlaygroundZoneMarker.");
                validate!.Invoke(marker, null);

                Assert.That(markerObject.transform.position.x, Is.EqualTo(anchor.position.x).Within(0.001f));
                Assert.That(markerObject.transform.position.y, Is.EqualTo(anchor.position.y).Within(0.001f));
                Assert.That(markerObject.transform.position.z, Is.EqualTo(anchor.position.z).Within(0.001f));
                Assert.That(Quaternion.Angle(markerObject.transform.rotation, anchor.rotation), Is.LessThan(0.001f));
                Assert.That(GetProperty<Vector3>(marker, "GizmoSize"), Is.EqualTo(new Vector3(0.25f, 0.25f, 0.3f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static System.Collections.Generic.Dictionary<string, PlaygroundSlotExpectation> BuildExpectedPlaygroundSlotsByAreaTag(ScriptableObject asset)
        {
            var expectations = new System.Collections.Generic.Dictionary<string, PlaygroundSlotExpectation>(StringComparer.Ordinal);
            var pools = GetProperty<Array>(asset, "Pools");
            Assert.That(pools, Is.Not.Null, "Expected MainTown population pools.");

            for (var poolIndex = 0; poolIndex < pools.Length; poolIndex++)
            {
                var pool = pools.GetValue(poolIndex);
                if (pool == null)
                {
                    continue;
                }

                var slots = GetProperty<Array>(pool, "Slots");
                if (slots == null)
                {
                    continue;
                }

                for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                {
                    var slot = slots.GetValue(slotIndex);
                    if (slot == null)
                    {
                        continue;
                    }

                    var areaTag = GetProperty<string>(slot, "AreaTag");
                    if (string.IsNullOrWhiteSpace(areaTag) || !areaTag.StartsWith("maintown.playground.", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    expectations[areaTag] = new PlaygroundSlotExpectation(
                        GetProperty<string>(slot, "PoolId"),
                        GetProperty<object>(slot, "Habitat"),
                        GetProperty<string>(slot, "SpawnAnchorId"));
                }
            }

            return expectations;
        }

        private static object CreateSlot(
            Type slotType,
            Type habitatType,
            string slotId,
            string poolId,
            string areaTag,
            string spawnAnchorId,
            string habitatName,
            bool isProtected)
        {
            var slot = Activator.CreateInstance(slotType);
            SetProperty(slot, "PopulationSlotId", slotId);
            SetProperty(slot, "PoolId", poolId);
            SetProperty(slot, "AreaTag", areaTag);
            SetProperty(slot, "SpawnAnchorId", spawnAnchorId);
            SetProperty(slot, "Habitat", Enum.Parse(habitatType, habitatName));
            SetProperty(slot, "IsProtectedFromContracts", isProtected);
            return slot;
        }

        private static Type ResolveRequiredType(string assemblyQualifiedName)
        {
            var type = Type.GetType(assemblyQualifiedName, throwOnError: false);
            Assert.That(type, Is.Not.Null, $"Expected type '{assemblyQualifiedName}' to exist.");
            return type;
        }

        private static void SetProperty(object instance, string propertyName, object value)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on '{instance.GetType().FullName}'.");
            property.SetValue(instance, value);
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on '{instance.GetType().FullName}'.");
            field.SetValue(instance, value);
        }

        private static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            Assert.That(property, Is.Not.Null, $"Expected property '{propertyName}' on '{instance.GetType().FullName}'.");
            return (T)property.GetValue(instance);
        }

        private readonly struct PlaygroundSlotExpectation
        {
            public PlaygroundSlotExpectation(string poolId, object habitat, string spawnAnchorId)
            {
                PoolId = poolId ?? string.Empty;
                Habitat = habitat;
                SpawnAnchorId = spawnAnchorId ?? string.Empty;
            }

            public string PoolId { get; }
            public object Habitat { get; }
            public string SpawnAnchorId { get; }
        }
    }
}
