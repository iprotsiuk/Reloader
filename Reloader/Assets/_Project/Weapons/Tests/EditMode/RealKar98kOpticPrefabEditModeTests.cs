using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Reloader.Weapons.Tests.EditMode
{
    public sealed class RealKar98kOpticPrefabEditModeTests
    {
        private const string OpticPrefabPath = "Assets/Low Poly Weapon Pack 4_WWII_1/Prefabs/Attachments/WWII_Optic_Remote_Range_A.prefab";

        [Test]
        public void RealKar98kOpticPrefab_UsesAuthoredPiPDisplaySurfaceOnPiPLens()
        {
            var opticPrefab = PrefabUtility.LoadPrefabContents(OpticPrefabPath);

            try
            {
                var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
                Assert.That(scopeLensDisplayType, Is.Not.Null, "Expected the ScopeLensDisplay runtime type to be available.");

                var lensDisplay = opticPrefab.GetComponentInChildren(scopeLensDisplayType, true);
                Assert.That(lensDisplay, Is.Not.Null, "The real Kar98k optic prefab should author a ScopeLensDisplay on the lens child.");
                var targetRenderer = lensDisplay!.GetType().GetProperty("TargetRenderer")?.GetValue(lensDisplay) as Renderer;
                Assert.That(targetRenderer, Is.Not.Null, "The real Kar98k optic prefab should bind ScopeLensDisplay to an authored display renderer.");
                Assert.That(targetRenderer!.name, Is.EqualTo("PiPDisplayRear"), "The real Kar98k optic prefab should render PiP on the authored display surface, not a proxy.");
                var isUsingProxySurface = lensDisplay.GetType().GetProperty("IsUsingProxySurface")?.GetValue(lensDisplay);
                Assert.That(isUsingProxySurface, Is.EqualTo(false), "The real Kar98k optic prefab should not fall back to a proxy display surface.");
                Assert.That(opticPrefab.transform.Find("ScopeDisplayProxy"), Is.Null, "The real Kar98k optic prefab should not contain a proxy display child.");

                var authoredMaterial = targetRenderer!.sharedMaterial;
                Assert.That((bool)lensDisplay.GetType().GetMethod("TrySetTexture")!.Invoke(lensDisplay, new object[] { Texture2D.blackTexture }), Is.True,
                    "The authored PiP display surface should accept a texture binding.");
                Assert.That(lensDisplay.GetType().GetProperty("CurrentTexture")?.GetValue(lensDisplay), Is.SameAs(Texture2D.blackTexture));

                var runtimeMaterial = targetRenderer!.sharedMaterial;
                Assert.That(runtimeMaterial, Is.Not.Null, "The authored PiP display surface should swap to a runtime display material when texture binding is active.");
                Assert.That(runtimeMaterial!.name, Does.Contain("Runtime"), "The runtime PiP surface should be a distinct runtime material instance.");
                Assert.That(runtimeMaterial.renderQueue, Is.EqualTo(authoredMaterial.renderQueue), "The runtime PiP surface should preserve the authored transparent render queue.");
                Assert.That(runtimeMaterial.shader.name, Is.EqualTo(authoredMaterial.shader.name), "The runtime PiP surface should preserve the authored lens shader.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(opticPrefab);
            }
        }

        private static Type ResolveType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return Type.GetType(typeName);
        }
    }
}
