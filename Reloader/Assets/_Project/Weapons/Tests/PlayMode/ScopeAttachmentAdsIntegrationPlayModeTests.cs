using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class ScopeAttachmentAdsIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator EquipOptic_HotSwap_UpdatesActiveSightAnchor()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            Assert.That(attachmentManagerType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            var manager = root.AddComponent(attachmentManagerType);
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(root.transform, false);
            var ironAnchor = new GameObject("IronSightAnchor").transform;
            ironAnchor.SetParent(root.transform, false);

            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironAnchor);

            var lpvo = CreateOpticDefinition("lpvo", 1f, 6f, true, "Auto");
            var highMag = CreateOpticDefinition("high", 6f, 12f, true, "Auto");

            Assert.That((bool)Invoke(manager, "EquipOptic", lpvo), Is.True);
            var firstAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
            Assert.That(firstAnchor, Is.Not.Null);
            Assert.That(firstAnchor.name, Is.EqualTo("SightAnchor"));
            Assert.That(GetProperty(manager, "ActiveOpticDefinition"), Is.EqualTo(lpvo));

            Assert.That((bool)Invoke(manager, "EquipOptic", highMag), Is.True);
            var secondAnchor = Invoke(manager, "GetActiveSightAnchor") as Transform;
            Assert.That(secondAnchor, Is.Not.Null);
            Assert.That(secondAnchor, Is.Not.EqualTo(firstAnchor));
            Assert.That(GetProperty(manager, "ActiveOpticDefinition"), Is.EqualTo(highMag));

            Cleanup(root, lpvo, highMag);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AdsVisualMode_Auto_TogglesMaskAcrossOpticHotSwap()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var scopeMaskControllerType = ResolveType("Reloader.Game.Weapons.ScopeMaskController");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(scopeMaskControllerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);

            var root = new GameObject("AdsRoot");
            var manager = root.AddComponent(attachmentManagerType);
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(root.transform, false);
            var ironAnchor = new GameObject("IronSightAnchor").transform;
            ironAnchor.SetParent(root.transform, false);
            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironAnchor);

            var worldCamGo = new GameObject("WorldCam");
            var worldCamera = worldCamGo.AddComponent<Camera>();
            var viewmodelCamGo = new GameObject("ViewmodelCam");
            var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();

            var scopeMaskGo = new GameObject("ScopeMask");
            var scopeMask = scopeMaskGo.AddComponent(scopeMaskControllerType);

            var ads = root.AddComponent(adsStateControllerType);
            SetField(ads, "_worldCamera", worldCamera);
            SetField(ads, "_viewmodelCamera", viewmodelCamera);
            SetField(ads, "_attachmentManager", manager);
            SetField(ads, "_scopeMaskController", scopeMask);
            SetField(ads, "_useLegacyInput", false);

            var lowMag = CreateOpticDefinition("red-dot", 1f, 1f, false, "Auto");
            var highMag = CreateOpticDefinition("scope-high", 6f, 12f, true, "Auto");

            Assert.That((bool)Invoke(manager, "EquipOptic", highMag), Is.True);
            Invoke(ads, "SetAdsHeld", true);
            Invoke(ads, "SetMagnification", 8f);

            yield return null;
            yield return null;

            Assert.That((bool)GetProperty(scopeMask, "IsMaskVisible"), Is.True, "High magnification scope should activate mask in Auto mode.");

            Assert.That((bool)Invoke(manager, "EquipOptic", lowMag), Is.True);
            Invoke(ads, "SetMagnification", 1f);

            yield return null;
            yield return null;

            Assert.That((bool)GetProperty(scopeMask, "IsMaskVisible"), Is.False, "Low magnification optic should disable mask in Auto mode.");

            Cleanup(root, lowMag, highMag, worldCamGo, viewmodelCamGo, scopeMaskGo);
        }

        private static ScriptableObject CreateOpticDefinition(string id, float minMag, float maxMag, bool variableZoom, string visualMode)
        {
            var opticDefinitionType = ResolveType("Reloader.Game.Weapons.OpticDefinition");
            var adsVisualModeType = ResolveType("Reloader.Game.Weapons.AdsVisualMode");
            Assert.That(opticDefinitionType, Is.Not.Null);
            Assert.That(adsVisualModeType, Is.Not.Null);

            var definition = ScriptableObject.CreateInstance(opticDefinitionType);
            SetField(definition, "_opticId", id);
            SetField(definition, "_isVariableZoom", variableZoom);
            SetField(definition, "_magnificationMin", minMag);
            SetField(definition, "_magnificationMax", maxMag);
            SetField(definition, "_magnificationStep", 1f);
            SetField(definition, "_visualModePolicy", Enum.Parse(adsVisualModeType, visualMode));

            var prefab = new GameObject($"Optic_{id}");
            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(prefab.transform, false);
            SetField(definition, "_opticPrefab", prefab);

            return definition;
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

        private static void Cleanup(params UnityEngine.Object[] objects)
        {
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }
        }

        private static void SetField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {instance.GetType().Name}.");
            field.SetValue(instance, value);
        }
    }
}
