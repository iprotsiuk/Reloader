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
                var apertureRenderer = lensDisplay.GetType().GetProperty("ApertureRenderer")?.GetValue(lensDisplay) as Renderer;
                var hipLensRenderer = lensDisplay.GetType().GetProperty("HipLensRenderer")?.GetValue(lensDisplay) as Renderer;
                Assert.That(targetRenderer, Is.Not.Null, "The real Kar98k optic prefab should bind ScopeLensDisplay to an authored display renderer.");
                Assert.That(apertureRenderer, Is.Not.Null, "The real Kar98k optic prefab should bind an explicit blur-aperture renderer instead of reusing the PiP display surface bounds.");
                Assert.That(hipLensRenderer, Is.Not.Null, "PiP optics should bind an explicit HIP front-lens renderer instead of reusing the rear display plane outside ADS.");
                Assert.That(targetRenderer!.name, Is.EqualTo("PiPDisplayRear"), "The real Kar98k optic prefab should render PiP on the authored display surface, not a proxy.");
                Assert.That(apertureRenderer!.name, Is.EqualTo("WWII_Optic_Remote_Range_A_Lens"), "The real Kar98k optic prefab should source blur aperture from the visible optic opening, not the rear PiP display plane.");
                Assert.That(hipLensRenderer!.name, Is.EqualTo("WWII_Optic_Remote_Range_A_Lens"), "PiP optics should show the visible front lens glass in HIP instead of the rear display plane.");
                var isUsingProxySurface = lensDisplay.GetType().GetProperty("IsUsingProxySurface")?.GetValue(lensDisplay);
                Assert.That(isUsingProxySurface, Is.EqualTo(false), "The real Kar98k optic prefab should not fall back to a proxy display surface.");
                Assert.That(opticPrefab.transform.Find("ScopeDisplayProxy"), Is.Null, "The real Kar98k optic prefab should not contain a proxy display child.");

                var authoredMaterial = targetRenderer!.sharedMaterial;
                var authoredHipLensMaterial = hipLensRenderer.sharedMaterial;
                Assert.That(authoredMaterial, Is.Not.Null, "The authored PiP display surface should start from a real material contract.");
                Assert.That(authoredHipLensMaterial, Is.Not.Null, "The authored HIP front-lens renderer should start from a real glass material contract.");
                Assert.That(authoredMaterial!.HasProperty("_EdgeBlurStrength"), Is.True, "The authored PiP display surface should expose explicit edge-blur tuning.");
                Assert.That(authoredMaterial.HasProperty("_EdgeDistortionStrength"), Is.True, "The authored PiP display surface should expose explicit edge-distortion tuning.");
                Assert.That(authoredMaterial.HasProperty("_EdgeVignetteStrength"), Is.True, "The authored PiP display surface should expose explicit vignette tuning.");
                var authoredHipLensShader = authoredHipLensMaterial!.shader;
                Assert.That(authoredHipLensShader, Is.Not.Null);
                var authoredShader = authoredMaterial!.shader;
                Assert.That(authoredShader, Is.Not.Null);
                var authoredRenderQueue = authoredMaterial.renderQueue;
                var authoredCull = GetFloatIfPresent(authoredMaterial, "_Cull");
                var authoredSurface = GetFloatIfPresent(authoredMaterial, "_Surface");
                var authoredZWrite = GetFloatIfPresent(authoredMaterial, "_ZWrite");
                Assert.That((bool)lensDisplay.GetType().GetMethod("TrySetTexture")!.Invoke(lensDisplay, new object[] { Texture2D.blackTexture }), Is.True,
                    "The authored PiP display surface should accept a texture binding.");
                Assert.That(lensDisplay.GetType().GetProperty("CurrentTexture")?.GetValue(lensDisplay), Is.SameAs(Texture2D.blackTexture));

                var runtimeMaterial = targetRenderer!.sharedMaterial;
                Assert.That(runtimeMaterial, Is.Not.Null, "The authored PiP display surface should swap to a runtime display material when texture binding is active.");
                Assert.That(runtimeMaterial!.name, Does.Contain("Runtime"), "The runtime PiP surface should be a distinct runtime material instance.");
                Assert.That(runtimeMaterial, Is.Not.SameAs(authoredMaterial), "The runtime PiP surface should use a cloned material instance so authored content remains untouched.");
                Assert.That(runtimeMaterial.shader, Is.SameAs(authoredShader), "The runtime PiP surface should preserve the authored shader contract instead of swapping to a generic fallback material.");
                Assert.That(runtimeMaterial.renderQueue, Is.EqualTo(authoredRenderQueue), "The runtime PiP surface should preserve the authored render queue.");
                Assert.That(GetFloatIfPresent(runtimeMaterial, "_Cull"), Is.EqualTo(authoredCull), "The runtime PiP surface should preserve authored face-culling so the display remains visible from the intended eye side.");
                Assert.That(GetFloatIfPresent(runtimeMaterial, "_Surface"), Is.EqualTo(authoredSurface), "The runtime PiP surface should preserve the authored opaque/transparent surface mode.");
                Assert.That(GetFloatIfPresent(runtimeMaterial, "_ZWrite"), Is.EqualTo(authoredZWrite), "The runtime PiP surface should preserve the authored depth-write behavior.");
                Assert.That(targetRenderer.enabled, Is.True, "Active PiP should show the authored rear display surface.");
                Assert.That(hipLensRenderer.enabled, Is.False, "Active PiP should hide the front-lens HIP glass surface.");

                Assert.That((bool)lensDisplay.GetType().GetMethod("TrySetTexture")!.Invoke(lensDisplay, new object[] { null }), Is.True,
                    "Clearing PiP should succeed on the authored display surface.");
                Assert.That(lensDisplay.GetType().GetProperty("CurrentTexture")?.GetValue(lensDisplay), Is.Null);
                Assert.That(targetRenderer.enabled, Is.False, "Clearing PiP should hide the rear PiP display surface in HIP.");
                Assert.That(targetRenderer.sharedMaterial, Is.SameAs(authoredMaterial), "Clearing PiP should restore the authored material contract.");
                Assert.That(hipLensRenderer.enabled, Is.True, "Clearing PiP should show the explicit front-lens glass surface in HIP.");
                Assert.That(hipLensRenderer.sharedMaterial, Is.SameAs(authoredHipLensMaterial), "Clearing PiP should preserve the authored front-lens glass material.");
                Assert.That(hipLensRenderer.sharedMaterial.shader, Is.SameAs(authoredHipLensShader), "Clearing PiP should keep the authored front-lens glass shader.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(opticPrefab);
            }
        }

        private static float? GetFloatIfPresent(Material material, string propertyName)
        {
            if (material == null || string.IsNullOrWhiteSpace(propertyName) || !material.HasProperty(propertyName))
            {
                return null;
            }

            return material.GetFloat(propertyName);
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
