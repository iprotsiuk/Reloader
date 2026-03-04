using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class MuzzleAttachmentRuntimePlayModeTests
    {
        [Test]
        public void MuzzleAttachmentRuntime_Equip_ChangesAudioOverrideAndSpawnsFlash()
        {
            var runtimeType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentRuntime");
            var definitionType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentDefinition");
            Assert.That(runtimeType, Is.Not.Null);
            Assert.That(definitionType, Is.Not.Null);

            var root = new GameObject("ViewRoot");
            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            var slot = new GameObject("MuzzleAttachmentSlot").transform;
            slot.SetParent(root.transform, false);

            var runtime = root.AddComponent(runtimeType);
            SetField(runtimeType, runtime, "_muzzleSocket", muzzle);
            SetField(runtimeType, runtime, "_attachmentSlot", slot);

            var definition = ScriptableObject.CreateInstance(definitionType);
            var clip = AudioClip.Create("override", 128, 1, 44100, false);
            var muzzlePrefab = new GameObject("MuzzleDevicePrefab");
            var flashPrefab = new GameObject("MuzzleFlashPrefab");
            SetField(definitionType, definition, "_muzzlePrefab", muzzlePrefab);
            SetField(definitionType, definition, "_flashPrefab", flashPrefab);
            SetField(definitionType, definition, "_fireClipOverride", clip);

            runtimeType.GetMethod("Equip", BindingFlags.Instance | BindingFlags.Public)?.Invoke(runtime, new object[] { definition });
            var overrideClip = runtimeType.GetMethod("TryGetFireClipOverride", BindingFlags.Instance | BindingFlags.Public)?.Invoke(runtime, null);

            Assert.That(overrideClip, Is.SameAs(clip));
            Assert.That(slot.childCount, Is.EqualTo(1), "Muzzle prefab should be mounted into attachment slot.");

            runtimeType.GetMethod("HandleWeaponFired", BindingFlags.Instance | BindingFlags.Public)?.Invoke(runtime, new object[] { "weapon-rifle-01" });
            var spawnedFlash = GameObject.Find("MuzzleFlashPrefab(Clone)");
            Assert.That(spawnedFlash, Is.Not.Null, "Firing should spawn flash prefab when configured.");

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(muzzlePrefab);
            UnityEngine.Object.DestroyImmediate(flashPrefab);
            UnityEngine.Object.DestroyImmediate(definition);
            UnityEngine.Object.DestroyImmediate(clip);
        }

        [Test]
        public void AttachmentManager_EquipMuzzle_WithRuntimeMissingSlot_ReportsFailure()
        {
            var managerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var runtimeType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentRuntime");
            var definitionType = ResolveType("Reloader.Game.Weapons.MuzzleAttachmentDefinition");
            Assert.That(managerType, Is.Not.Null);
            Assert.That(runtimeType, Is.Not.Null);
            Assert.That(definitionType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            var manager = root.AddComponent(managerType);
            var runtime = root.AddComponent(runtimeType);

            var managerRuntimeField = managerType.GetField("_muzzleRuntime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(managerRuntimeField, Is.Not.Null);
            managerRuntimeField.SetValue(manager, runtime);

            // Intentionally do not set runtime _attachmentSlot so runtime.Equip cannot mount the prefab.
            var definition = ScriptableObject.CreateInstance(definitionType);
            var muzzlePrefab = new GameObject("MuzzleDevicePrefab");
            var muzzlePrefabField = definitionType.GetField("_muzzlePrefab", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(muzzlePrefabField, Is.Not.Null);
            muzzlePrefabField.SetValue(definition, muzzlePrefab);

            var result = managerType.GetMethod("EquipMuzzle", BindingFlags.Instance | BindingFlags.Public)?.Invoke(manager, new object[] { definition });
            Assert.That(result, Is.EqualTo(false));

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(muzzlePrefab);
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
