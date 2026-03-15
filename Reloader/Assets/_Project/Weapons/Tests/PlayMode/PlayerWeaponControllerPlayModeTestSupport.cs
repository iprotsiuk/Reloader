using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Audio;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Ballistics;
using Reloader.Weapons.Cinematics;
using Reloader.Weapons.Controllers;
using Reloader.Weapons.Data;
using Reloader.Weapons.Runtime;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Weapons.Tests.PlayMode
{
    public partial class PlayerWeaponControllerPlayModeTests
    {
        private static object Invoke(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found on {instance.GetType().Name}.");
            return method.Invoke(instance, args);
        }

        private static object GetProperty(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(property, Is.Not.Null, $"Property '{propertyName}' was not found on {instance.GetType().Name}.");
            return property.GetValue(instance);
        }

        private static void SetField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(instance, value);
        }

        private static object GetField(Type type, object instance, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return field.GetValue(instance);
        }

        private static void ConfigureTestWeaponViewMounts(
            GameObject viewPrefab,
            Transform adsPivot = null,
            Transform muzzleFirePoint = null,
            Transform ironSightAnchor = null,
            Transform magazineSocket = null,
            Transform magazineDropSocket = null,
            Transform scopeSlot = null,
            Transform muzzleSlot = null)
        {
            Assert.That(viewPrefab, Is.Not.Null);

            var mountsType = ResolveType("Reloader.Weapons.Runtime.WeaponViewAttachmentMounts");
            Assert.That(mountsType, Is.Not.Null, "WeaponViewAttachmentMounts should exist so runtime views declare slots explicitly.");

            var mounts = viewPrefab.GetComponent(mountsType) ?? viewPrefab.AddComponent(mountsType);
            SetField(mountsType, mounts, "_adsPivot", adsPivot != null ? adsPivot : viewPrefab.transform);
            SetField(mountsType, mounts, "_muzzleTransform", muzzleFirePoint);
            SetField(mountsType, mounts, "_ironSightAnchor", ironSightAnchor);
            SetField(mountsType, mounts, "_magazineSocket", magazineSocket);
            SetField(mountsType, mounts, "_magazineDropSocket", magazineDropSocket);

            var slotEntryType = mountsType.GetNestedType("AttachmentSlotMount", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(slotEntryType, Is.Not.Null);

            var entries = Array.CreateInstance(slotEntryType, scopeSlot != null && muzzleSlot != null ? 2 : scopeSlot != null || muzzleSlot != null ? 1 : 0);
            var index = 0;
            if (scopeSlot != null)
            {
                var entry = Activator.CreateInstance(slotEntryType);
                SetField(slotEntryType, entry, "_slotType", WeaponAttachmentSlotType.Scope);
                SetField(slotEntryType, entry, "_slotTransform", scopeSlot);
                entries.SetValue(entry, index++);
            }

            if (muzzleSlot != null)
            {
                var entry = Activator.CreateInstance(slotEntryType);
                SetField(slotEntryType, entry, "_slotType", WeaponAttachmentSlotType.Muzzle);
                SetField(slotEntryType, entry, "_slotTransform", muzzleSlot);
                entries.SetValue(entry, index);
            }

            SetField(mountsType, mounts, "_attachmentSlots", entries);
        }

        private static MonoBehaviour FindFirstCinemachineCamera()
        {
            var allCameras = FindAllCinemachineCameras();
            return allCameras.Count > 0 ? allCameras[0] : null;
        }

        private static Camera FindShotRenderCamera()
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (camera != null && camera.name == "ShotCameraRuntime_Camera")
                {
                    return camera;
                }
            }

            return null;
        }

        private static UIDocument CreateRuntimeHudDocument(string screenId, Transform parent)
        {
            var screenGo = new GameObject(screenId);
            screenGo.transform.SetParent(parent, false);
            return screenGo.AddComponent<UIDocument>();
        }

        private static bool IsDocumentVisible(UIDocument document)
        {
            if (document == null || !document.enabled)
            {
                return false;
            }

            var root = document.rootVisualElement;
            return root != null && root.visible;
        }

        private static List<MonoBehaviour> FindAllCinemachineCameras()
        {
            var matches = new List<MonoBehaviour>();
            foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour != null && behaviour.GetType().FullName == "Unity.Cinemachine.CinemachineCamera")
                {
                    matches.Add(behaviour);
                }
            }

            return matches;
        }

        private sealed class TestInputSource : MonoBehaviour, IPlayerInputSource, IShotCameraInputSource
        {
            public bool FirePressedThisFrame;
            public bool ReloadPressedThisFrame;
            public bool ShotCameraCancelPressedThisFrame;
            public bool SprintHeldValue;
            public bool AimHeldValue;
            public bool ShotCameraSpeedUpHeldValue;
            public Vector2 LookInputValue;
            public float ZoomQueued;
            public int ZeroAdjustQueued;

            public Vector2 MoveInput => Vector2.zero;
            public Vector2 LookInput => LookInputValue;
            public bool SprintHeld => SprintHeldValue;
            public bool AimHeld => AimHeldValue;
            public bool ShotCameraSpeedUpHeld => ShotCameraSpeedUpHeldValue;
            public bool ConsumeJumpPressed() => false;
            public bool ConsumePickupPressed() => false;
            public int ConsumeBeltSelectPressed() => -1;
            public bool ConsumeMenuTogglePressed() => false;
            public bool ConsumeDevConsoleTogglePressed() => false;
            public bool ConsumeAutocompletePressed() => false;
            public int ConsumeSuggestionDelta() => 0;
            public bool ConsumeAimTogglePressed() => false;
            public bool ConsumeShotCameraCancelPressed()
            {
                if (!ShotCameraCancelPressedThisFrame)
                {
                    return false;
                }

                ShotCameraCancelPressedThisFrame = false;
                return true;
            }

            public float ConsumeZoomInput()
            {
                if (Mathf.Approximately(ZoomQueued, 0f))
                {
                    return 0f;
                }

                var queued = ZoomQueued;
                ZoomQueued = 0f;
                return queued;
            }

            public int ConsumeZeroAdjustStep()
            {
                if (ZeroAdjustQueued == 0)
                {
                    return 0;
                }

                var queued = ZeroAdjustQueued;
                ZeroAdjustQueued = 0;
                return queued;
            }

            public bool ConsumeFirePressed()
            {
                if (!FirePressedThisFrame)
                {
                    return false;
                }

                FirePressedThisFrame = false;
                return true;
            }

            public bool ConsumeReloadPressed()
            {
                if (!ReloadPressedThisFrame)
                {
                    return false;
                }

                ReloadPressedThisFrame = false;
                return true;
            }
        }

        private sealed class MinimalBridgeMarker : MonoBehaviour
        {
        }

        private sealed class StubAdsStateRuntimeBridge : MonoBehaviour
        {
            public float CurrentMagnification { get; private set; } = 1f;
            public float LastSetMagnification { get; private set; } = float.NaN;

            public void SetAdsHeld(bool held)
            {
            }

            public void SetMagnification(float magnification)
            {
                LastSetMagnification = magnification;
                CurrentMagnification = magnification;
            }

            public bool ApplyScopeAdjustmentInput(int windageClicks, int elevationClicks)
            {
                return false;
            }
        }

        private sealed class Vector3EqualityComparer : System.Collections.IEqualityComparer
        {
            public static readonly Vector3EqualityComparer Instance = new();

            public new bool Equals(object x, object y)
            {
                if (x is not Vector3 lhs || y is not Vector3 rhs)
                {
                    return false;
                }

                return Vector3.Distance(lhs, rhs) <= 0.0001f;
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }

        private sealed class QuaternionEqualityComparer : System.Collections.IEqualityComparer
        {
            public static readonly QuaternionEqualityComparer Instance = new();

            public new bool Equals(object x, object y)
            {
                if (x is not Quaternion lhs || y is not Quaternion rhs)
                {
                    return false;
                }

                return Quaternion.Angle(lhs, rhs) <= 0.1f;
            }

            public int GetHashCode(object obj)
            {
                return obj?.GetHashCode() ?? 0;
            }
        }

        private sealed class TestDamageable : MonoBehaviour, IDamageable
        {
            public int HitCount { get; private set; }
            public float LastDamage { get; private set; }

            public void ApplyDamage(ProjectileImpactPayload payload)
            {
                HitCount++;
                LastDamage = payload.Damage;
            }
        }

        private sealed class TestPickupResolver : MonoBehaviour, IInventoryPickupTargetResolver
        {
            public bool TryResolvePickupTarget(out IInventoryPickupTarget target)
            {
                target = null;
                return false;
            }
        }

        private sealed class ShotCameraRegistrationSpy : MonoBehaviour
        {
            public int RequestCount { get; private set; }
            public WeaponProjectile LastProjectile { get; private set; }

            public void RegisterShotCameraRequest(WeaponProjectile projectile)
            {
                RequestCount++;
                LastProjectile = projectile;
            }
        }

        private static float MagnificationToFieldOfView(float referenceFieldOfView, float magnification)
        {
            var safeReferenceFov = Mathf.Clamp(referenceFieldOfView, 1f, 179f);
            var safeMagnification = Mathf.Max(1f, magnification);
            var referenceHalfAngle = safeReferenceFov * 0.5f * Mathf.Deg2Rad;
            var zoomedHalfAngle = Mathf.Atan(Mathf.Tan(referenceHalfAngle) / safeMagnification);
            return Mathf.Clamp(zoomedHalfAngle * 2f * Mathf.Rad2Deg, 5f, safeReferenceFov);
        }

        private static void SetControllerField(PlayerWeaponController controller, string fieldName, object value)
        {
            var field = typeof(PlayerWeaponController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            field.SetValue(controller, value);
        }

        private static void SetPrivateField(Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on type '{type.FullName}'.");
            field!.SetValue(instance, value);
        }

        private static T GetControllerField<T>(PlayerWeaponController controller, string fieldName)
        {
            var field = typeof(PlayerWeaponController).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found.");
            return (T)field.GetValue(controller);
        }

        private static void SetControllerWeaponViewBinding(PlayerWeaponController controller, string itemId, GameObject viewPrefab)
        {
            Assert.That(controller, Is.Not.Null);
            Assert.That(viewPrefab, Is.Not.Null);

            var weaponViewParentField = typeof(PlayerWeaponController).GetField("_weaponViewParent", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(weaponViewParentField, Is.Not.Null, "Field '_weaponViewParent' was not found.");
            if (weaponViewParentField.GetValue(controller) == null)
            {
                weaponViewParentField.SetValue(controller, controller.transform);
            }

            var bindingType = typeof(WeaponViewPrefabBinding);
            var binding = Activator.CreateInstance(bindingType);
            SetField(bindingType, binding, "_itemId", itemId);
            SetField(bindingType, binding, "_viewPrefab", viewPrefab);

            var bindings = new[] { (WeaponViewPrefabBinding)binding };
            SetControllerField(controller, "_weaponViewPrefabs", bindings);
            Invoke(controller, "UpdateEquipFromSelection");
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

        private sealed class ClipCaptureWeaponCombatAudioEmitter : WeaponCombatAudioEmitter
        {
            public AudioClip LastFireOverrideClip { get; private set; }

            public override void EmitWeaponFire(string weaponId, Vector3 muzzlePosition, AudioClip overrideClip = null)
            {
                LastFireOverrideClip = overrideClip;
            }
        }

    }
}
