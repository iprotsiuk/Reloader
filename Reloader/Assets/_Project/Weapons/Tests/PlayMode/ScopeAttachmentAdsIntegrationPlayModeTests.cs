using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Game.Weapons;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public class ScopeAttachmentAdsIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator EquipOptic_HotSwap_UpdatesActiveSightAnchor()
        {
            var root = new GameObject("AttachmentRoot");
            var manager = root.AddComponent<AttachmentManager>();
            var scopeSlot = new GameObject("ScopeSlot").transform;
            scopeSlot.SetParent(root.transform, false);
            var ironAnchor = new GameObject("IronSightAnchor").transform;
            ironAnchor.SetParent(root.transform, false);

            SetField(manager, "_scopeSlot", scopeSlot);
            SetField(manager, "_ironSightAnchor", ironAnchor);

            var lpvo = CreateOpticDefinition("lpvo", 1f, 6f, true, AdsVisualMode.Auto);
            var highMag = CreateOpticDefinition("high", 6f, 12f, true, AdsVisualMode.Auto);

            Assert.That(manager.EquipOptic(lpvo), Is.True);
            var firstAnchor = manager.GetActiveSightAnchor();
            Assert.That(firstAnchor, Is.Not.Null);
            Assert.That(firstAnchor.name, Is.EqualTo("SightAnchor"));
            Assert.That(manager.ActiveOpticDefinition, Is.EqualTo(lpvo));

            Assert.That(manager.EquipOptic(highMag), Is.True);
            var secondAnchor = manager.GetActiveSightAnchor();
            Assert.That(secondAnchor, Is.Not.Null);
            Assert.That(secondAnchor, Is.Not.EqualTo(firstAnchor));
            Assert.That(manager.ActiveOpticDefinition, Is.EqualTo(highMag));

            Cleanup(root, lpvo, highMag);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AdsVisualMode_Auto_TogglesMaskAcrossOpticHotSwap()
        {
            var root = new GameObject("AdsRoot");

            var manager = root.AddComponent<AttachmentManager>();
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
            var scopeMask = scopeMaskGo.AddComponent<ScopeMaskController>();

            var ads = root.AddComponent<AdsStateController>();
            SetField(ads, "_worldCamera", worldCamera);
            SetField(ads, "_viewmodelCamera", viewmodelCamera);
            SetField(ads, "_attachmentManager", manager);
            SetField(ads, "_scopeMaskController", scopeMask);
            SetField(ads, "_useLegacyInput", false);

            var lowMag = CreateOpticDefinition("red-dot", 1f, 1f, false, AdsVisualMode.Auto);
            var highMag = CreateOpticDefinition("scope-high", 6f, 12f, true, AdsVisualMode.Auto);

            Assert.That(manager.EquipOptic(highMag), Is.True);
            ads.SetAdsHeld(true);
            ads.SetMagnification(8f);

            yield return null;
            yield return null;

            Assert.That(scopeMask.IsMaskVisible, Is.True, "High magnification scope should activate mask in Auto mode.");

            Assert.That(manager.EquipOptic(lowMag), Is.True);
            ads.SetMagnification(1f);

            yield return null;
            yield return null;

            Assert.That(scopeMask.IsMaskVisible, Is.False, "Low magnification optic should disable mask in Auto mode.");

            Cleanup(root, lowMag, highMag, worldCamGo, viewmodelCamGo, scopeMaskGo);
        }

        private static OpticDefinition CreateOpticDefinition(string id, float minMag, float maxMag, bool variableZoom, AdsVisualMode mode)
        {
            var definition = ScriptableObject.CreateInstance<OpticDefinition>();
            SetField(definition, "_opticId", id);
            SetField(definition, "_isVariableZoom", variableZoom);
            SetField(definition, "_magnificationMin", minMag);
            SetField(definition, "_magnificationMax", maxMag);
            SetField(definition, "_magnificationStep", 1f);
            SetField(definition, "_visualModePolicy", mode);

            var prefab = new GameObject($"Optic_{id}");
            var sightAnchor = new GameObject("SightAnchor");
            sightAnchor.transform.SetParent(prefab.transform, false);
            SetField(definition, "_opticPrefab", prefab);

            return definition;
        }

        private static void Cleanup(params Object[] objects)
        {
            for (var i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                {
                    Object.DestroyImmediate(objects[i]);
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
