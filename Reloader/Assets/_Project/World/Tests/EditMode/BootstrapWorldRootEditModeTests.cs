using System.Reflection;
using NUnit.Framework;
using Reloader.World.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace Reloader.World.Tests.EditMode
{
    public sealed class BootstrapWorldRootEditModeTests
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        [TearDown]
        public void TearDown()
        {
            var playerRoot = PersistentPlayerRoot.Instance?.PlayerRootTransform;
            if (playerRoot != null)
            {
                Object.DestroyImmediate(playerRoot.gameObject);
            }

            if (PersistentPlayerRoot.Instance != null)
            {
                Object.DestroyImmediate(PersistentPlayerRoot.Instance.gameObject);
            }
        }

        [Test]
        public void Initialize_CreatesSingleCanonicalOriginSeamsOnPersistentRuntimeOwner_AndReusesThemAcrossRepeatedCalls()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                var bootstrapRoot = FindBootstrapWorldRoot(scene);

                var persistentRoot = BootstrapWorldRoot.Initialize();
                var stateType = GetOriginType("DynamicOriginRebaseState");
                var bridgeType = GetOriginType("StableWorldCoordinateBridge");
                var controllerType = GetOriginType("DynamicOriginRebaseController");

                var firstState = persistentRoot.GetComponent(stateType);
                var firstBridge = persistentRoot.GetComponent(bridgeType);
                var firstController = persistentRoot.GetComponent(controllerType);

                Assert.That(persistentRoot, Is.Not.Null);
                Assert.That(firstState, Is.Not.Null);
                Assert.That(firstBridge, Is.Not.Null);
                Assert.That(firstController, Is.Not.Null);
                Assert.That(bootstrapRoot.GetComponent(stateType), Is.Null);
                Assert.That(bootstrapRoot.GetComponent(bridgeType), Is.Null);
                Assert.That(bootstrapRoot.GetComponent(controllerType), Is.Null);

                BootstrapWorldRoot.Initialize();

                Assert.That(persistentRoot.GetComponents(stateType).Length, Is.EqualTo(1));
                Assert.That(persistentRoot.GetComponents(bridgeType).Length, Is.EqualTo(1));
                Assert.That(persistentRoot.GetComponents(controllerType).Length, Is.EqualTo(1));
                Assert.That(persistentRoot.GetComponent(stateType), Is.SameAs(firstState));
                Assert.That(persistentRoot.GetComponent(bridgeType), Is.SameAs(firstBridge));
                Assert.That(persistentRoot.GetComponent(controllerType), Is.SameAs(firstController));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void Initialize_ResetsOriginOffsetsAndLastRebaseStateDeterministically()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Additive);

            try
            {
                var persistentRoot = BootstrapWorldRoot.Initialize();
                var state = persistentRoot.GetComponent(GetOriginType("DynamicOriginRebaseState"));
                var controller = persistentRoot.GetComponent(GetOriginType("DynamicOriginRebaseController"));

                Invoke(state, "ApplyRebase", new Vector3(-120f, 0f, 45f), new Vector3(120f, 0f, -45f), 8f);
                SetSerializedField(controller, "_lastRebaseTime", 8f);

                BootstrapWorldRoot.Initialize();

                Assert.That(GetVector3Property(state, "StableOriginOffset"), Is.EqualTo(Vector3.zero));
                Assert.That(GetVector3Property(state, "LocalOriginOffset"), Is.EqualTo(Vector3.zero));
                Assert.That(GetFloatProperty(state, "LastRebaseTime"), Is.EqualTo(float.NegativeInfinity));
                Assert.That(GetFloatProperty(controller, "LastRebaseTime"), Is.EqualTo(float.NegativeInfinity));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        [Test]
        public void Initialize_WithoutBootstrapOwner_FailsClosedWithoutInventingOriginSeams()
        {
            var originalScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                LogAssert.Expect(LogType.Error,
                    "BootstrapWorldRoot failed: no loaded BootstrapWorldRoot instance is available to provide the canonical runtime player prefab.");
                var result = BootstrapWorldRoot.Initialize();

                Assert.That(result, Is.Null);
                Assert.That(PersistentPlayerRoot.Instance == null, Is.True);
                Assert.That(Object.FindObjectsByType(GetOriginType("DynamicOriginRebaseState"), FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
                Assert.That(Object.FindObjectsByType(GetOriginType("StableWorldCoordinateBridge"), FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
                Assert.That(Object.FindObjectsByType(GetOriginType("DynamicOriginRebaseController"), FindObjectsInactive.Include, FindObjectsSortMode.None), Is.Empty);
            }
            finally
            {
                if (scene.IsValid())
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (originalScene.IsValid())
                {
                    SceneManager.SetActiveScene(originalScene);
                }
            }
        }

        private static BootstrapWorldRoot FindBootstrapWorldRoot(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var bootstrapRoot = roots[i].GetComponent<BootstrapWorldRoot>();
                if (bootstrapRoot != null)
                {
                    return bootstrapRoot;
                }
            }

            Assert.Fail($"Expected BootstrapWorldRoot in scene '{scene.path}'.");
            return null;
        }

        private static System.Type GetOriginType(string typeName)
        {
            var resolvedType = System.Type.GetType($"Reloader.World.Runtime.Origin.{typeName}, Reloader.World");
            Assert.That(resolvedType, Is.Not.Null, $"Expected origin runtime type '{typeName}' in assembly 'Reloader.World'.");
            return resolvedType;
        }

        private static void Invoke(object target, string methodName, params object[] args)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected {target.GetType().Name}.{methodName} to exist.");
            method.Invoke(target, args);
        }

        private static Vector3 GetVector3Property(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Expected {target.GetType().Name}.{propertyName} property to exist.");
            return (Vector3)property.GetValue(target);
        }

        private static float GetFloatProperty(object target, string propertyName)
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Expected {target.GetType().Name}.{propertyName} property to exist.");
            return (float)property.GetValue(target);
        }

        private static void SetSerializedField(Object target, string fieldName, float value)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(fieldName);
            Assert.That(field, Is.Not.Null, $"Expected serialized field '{fieldName}' on {target.GetType().Name}.");
            field.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
