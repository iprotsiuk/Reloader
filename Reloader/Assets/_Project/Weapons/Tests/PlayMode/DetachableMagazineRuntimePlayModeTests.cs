using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class DetachableMagazineRuntimePlayModeTests
    {
        [Test]
        public void DetachableMagazineRuntime_ReloadCycle_HidesAndRestoresMagazineVisual()
        {
            var runtimeType = ResolveType("Reloader.Game.Weapons.DetachableMagazineRuntime");
            var definitionType = ResolveType("Reloader.Game.Weapons.MagazineAttachmentDefinition");
            Assert.That(runtimeType, Is.Not.Null);
            Assert.That(definitionType, Is.Not.Null);

            var root = new GameObject("ViewRoot");
            var magSocket = new GameObject("MagazineSocket").transform;
            magSocket.SetParent(root.transform, false);
            var dropSocket = new GameObject("MagazineDropSocket").transform;
            dropSocket.SetParent(root.transform, false);

            var runtime = root.AddComponent(runtimeType);
            SetField(runtimeType, runtime, "_magazineSocket", magSocket);
            SetField(runtimeType, runtime, "_magazineDropSocket", dropSocket);

            var definition = ScriptableObject.CreateInstance(definitionType);
            var magVisualPrefab = new GameObject("MagazineVisualPrefab");
            var droppedPrefab = new GameObject("DroppedMagazinePrefab");
            SetField(definitionType, definition, "_magazineVisualPrefab", magVisualPrefab);
            SetField(definitionType, definition, "_droppedMagazinePrefab", droppedPrefab);
            SetField(definitionType, definition, "_detachOnReloadStart", true);
            SetField(definitionType, definition, "_spawnDroppedMagazine", true);
            SetField(definitionType, definition, "_droppedMagazineLifetimeSeconds", 1f);

            runtimeType.GetMethod("SetAttachment", BindingFlags.Instance | BindingFlags.Public)?.Invoke(runtime, new object[] { definition });
            Assert.That(magSocket.childCount, Is.EqualTo(1), "Magazine visual should be attached at setup.");

            runtimeType.GetMethod("HandleReloadStarted", BindingFlags.Instance | BindingFlags.Public)?.Invoke(runtime, new object[] { "weapon-rifle-01" });
            Assert.That(magSocket.GetChild(0).gameObject.activeSelf, Is.False, "Reload start should hide attached magazine.");
            Assert.That(GameObject.Find("DroppedMagazinePrefab(Clone)"), Is.Not.Null, "Reload start should spawn dropped magazine when configured.");

            runtimeType.GetMethod("HandleMagazineInserted", BindingFlags.Instance | BindingFlags.Public)?.Invoke(runtime, new object[] { "weapon-rifle-01" });
            Assert.That(magSocket.GetChild(0).gameObject.activeSelf, Is.True, "Insert event should restore magazine visual.");

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(magVisualPrefab);
            UnityEngine.Object.DestroyImmediate(droppedPrefab);
            UnityEngine.Object.DestroyImmediate(definition);
        }

        private static Type ResolveType(string fullName)
        {
            var direct = Type.GetType(fullName);
            if (direct != null)
            {
                return direct;
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var resolved = assemblies[i].GetType(fullName);
                if (resolved != null)
                {
                    return resolved;
                }
            }

            return null;
        }

        private static void SetField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(instance, value);
        }
    }
}
