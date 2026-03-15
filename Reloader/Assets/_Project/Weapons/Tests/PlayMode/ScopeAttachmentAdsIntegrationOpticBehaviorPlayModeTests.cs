using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.Weapons.Tests.PlayMode
{
    public partial class ScopeAttachmentAdsIntegrationPlayModeTests
    {
        [Test]
        public void RealKar98kOpticAsset_UsesRenderTexturePipVisualMode()
        {
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            var adsVisualModeType = ResolveType("Reloader.Game.Weapons.AdsVisualMode");
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");
            Assert.That(adsVisualModeType, Is.Not.Null);

            var visualModePolicyProperty = opticDefinition.GetType().GetProperty("VisualModePolicy", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(visualModePolicyProperty, Is.Not.Null);

            var visualMode = visualModePolicyProperty.GetValue(opticDefinition);
            Assert.That(visualMode, Is.EqualTo(Enum.Parse(adsVisualModeType, "RenderTexturePiP")));
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_LensDisplay_UsesProxySurfaceWhenTextureApplied()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            scopeTexture.SetPixel(0, 0, Color.white);
            scopeTexture.SetPixel(1, 0, Color.black);
            scopeTexture.SetPixel(0, 1, Color.black);
            scopeTexture.SetPixel(1, 1, Color.white);
            scopeTexture.Apply(false, true);

            try
            {
                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                Assert.That((bool)GetProperty(lensDisplay, "IsUsingProxySurface"), Is.True,
                    "Real Kar98k optic should use a runtime proxy display surface when its authored lens mesh cannot present PiP imagery correctly.");
                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null, "Real Kar98k proxy display surface should be instantiated under the optic root.");

                var sightAnchor = opticInstance.transform.Find("SightAnchor");
                Assert.That(sightAnchor, Is.Not.Null);
                var lensTransform = GetComponentInChildren(opticInstance, scopeLensDisplayType).transform;

                var distanceToSightAnchor = Vector3.Distance(proxySurface.position, sightAnchor.position);
                var distanceToFrontLens = Vector3.Distance(proxySurface.position, lensTransform.position);
                Assert.That(distanceToSightAnchor, Is.LessThan(distanceToFrontLens),
                    "Proxy display surface should sit near the authored sight anchor on the eyepiece side, not on the front lens.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_UsesLensShapedMeshInsteadOfQuad()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                var internalLensMeshFilter = lensDisplay.GetComponent<MeshFilter>();
                Assert.That(internalLensMeshFilter, Is.Not.Null);
                Assert.That(internalLensMeshFilter.sharedMesh, Is.Not.Null);
                Assert.That(internalLensMeshFilter.sharedMesh.isReadable, Is.True,
                    "Real Kar98k optic lens mesh must stay readable so proxy fallback can preserve the authored lens silhouette.");

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);
                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyMeshFilter = proxySurface.GetComponent<MeshFilter>();
                Assert.That(proxyMeshFilter, Is.Not.Null);
                Assert.That(proxyMeshFilter.sharedMesh, Is.Not.Null);
                Assert.That(proxyMeshFilter.sharedMesh.vertexCount, Is.EqualTo(internalLensMeshFilter.sharedMesh.vertexCount),
                    "Proxy fallback should reuse the authored lens silhouette instead of reverting to a rectangular quad.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_FillsMeaningfulViewportAreaFromSightAnchor()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var previewCameraGo = new GameObject("ScopePreviewCamera");

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null, "Real Kar98k proxy display surface should be instantiated under the optic root.");

                var proxyRenderer = proxySurface.GetComponent<Renderer>();
                Assert.That(proxyRenderer, Is.Not.Null);

                var sightAnchor = opticInstance.transform.Find("SightAnchor");
                Assert.That(sightAnchor, Is.Not.Null);

                var previewCamera = previewCameraGo.AddComponent<Camera>();
                previewCamera.transform.SetPositionAndRotation(sightAnchor.position, sightAnchor.rotation);
                previewCamera.nearClipPlane = 0.001f;
                previewCamera.farClipPlane = 10f;
                previewCamera.fieldOfView = 60f;

                var viewportRect = CalculateViewportRect(previewCamera, proxyRenderer.bounds);
                Assert.That(viewportRect.width, Is.GreaterThan(0.12f),
                    $"Proxy display surface should occupy a meaningful amount of screen width from the eye box. Actual width={viewportRect.width:0.000}.");
                Assert.That(viewportRect.height, Is.GreaterThan(0.12f),
                    $"Proxy display surface should occupy a meaningful amount of screen height from the eye box. Actual height={viewportRect.height:0.000}.");
            }
            finally
            {
                Cleanup(previewCameraGo, opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_IsLargerThanInternalLensSurface()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                var internalLensRenderer = lensDisplay.GetComponent<Renderer>();
                Assert.That(internalLensRenderer, Is.Not.Null);

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyRenderer = proxySurface.GetComponent<Renderer>();
                Assert.That(proxyRenderer, Is.Not.Null);

                Assert.That(proxyRenderer.bounds.size.x, Is.GreaterThan(internalLensRenderer.bounds.size.x * 1.01f),
                    $"Proxy display surface should be larger than the internal lens aperture so the PiP image does not look buried inside the scope. Proxy width={proxyRenderer.bounds.size.x:0.0000}, internal width={internalLensRenderer.bounds.size.x:0.0000}.");
                Assert.That(proxyRenderer.bounds.size.y, Is.GreaterThan(internalLensRenderer.bounds.size.y * 1.01f),
                    $"Proxy display surface should be larger than the internal lens aperture so the PiP image does not look buried inside the scope. Proxy height={proxyRenderer.bounds.size.y:0.0000}, internal height={internalLensRenderer.bounds.size.y:0.0000}.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_PreservesAuthoredLensAspectRatio()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                var internalLensRenderer = lensDisplay.GetComponent<Renderer>();
                Assert.That(internalLensRenderer, Is.Not.Null);

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyRenderer = proxySurface.GetComponent<Renderer>();
                Assert.That(proxyRenderer, Is.Not.Null);

                var sourceAspect = internalLensRenderer.bounds.size.x / Mathf.Max(0.0001f, internalLensRenderer.bounds.size.y);
                var proxyAspect = proxyRenderer.bounds.size.x / Mathf.Max(0.0001f, proxyRenderer.bounds.size.y);
                Assert.That(proxyAspect, Is.EqualTo(sourceAspect).Within(0.05f),
                    $"Proxy fallback should preserve the authored lens aspect ratio instead of stretching vertically. Proxy aspect={proxyAspect:0.000}, source aspect={sourceAspect:0.000}.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator RealKar98kOpticAsset_ProxySurface_CentersOnAuthoredLensInLensPlane()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            var opticDefinition = ResolveOpticDefinitionById("att-kar98k-scope-remote-a");
            Assert.That(scopeLensDisplayType, Is.Not.Null);
            Assert.That(opticDefinition, Is.Not.Null, "Expected the real Kar98k optic definition asset to be loaded.");

            var opticPrefabProperty = opticDefinition.GetType().GetProperty("OpticPrefab", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(opticPrefabProperty, Is.Not.Null);

            var opticPrefab = opticPrefabProperty.GetValue(opticDefinition) as GameObject;
            Assert.That(opticPrefab, Is.Not.Null, "Real Kar98k optic should expose an authored optic prefab.");

            var opticInstance = UnityEngine.Object.Instantiate(opticPrefab);
            var scopeTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                scopeTexture.SetPixel(0, 0, Color.white);
                scopeTexture.SetPixel(1, 0, Color.black);
                scopeTexture.SetPixel(0, 1, Color.black);
                scopeTexture.SetPixel(1, 1, Color.white);
                scopeTexture.Apply(false, true);

                yield return null;

                var lensDisplay = GetComponentInChildren(opticInstance, scopeLensDisplayType);
                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = opticInstance.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var lensPlaneOffset = lensDisplay.transform.InverseTransformPoint(proxySurface.position);
                Assert.That(Mathf.Abs(lensPlaneOffset.x), Is.LessThan(0.0025f),
                    $"Proxy display surface should stay centered on the authored lens horizontally. Local x offset={lensPlaneOffset.x:0.0000}.");
                Assert.That(Mathf.Abs(lensPlaneOffset.y), Is.LessThan(0.0025f),
                    $"Proxy display surface should stay centered on the authored lens vertically. Local y offset={lensPlaneOffset.y:0.0000}.");
            }
            finally
            {
                Cleanup(opticInstance, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_ProxyDisplayLocalOffset_ShiftsProxyAlongLensAxes()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var unoffsetRoot = CreateProxyOffsetFixture(scopeLensDisplayType, Vector3.zero, 1f, out var unoffsetLensDisplay, out var unoffsetScopeTexture);
            var offsetRoot = CreateProxyOffsetFixture(scopeLensDisplayType, new Vector3(0.004f, -0.006f, 0.002f), 1f, out var offsetLensDisplay, out var offsetScopeTexture);

            try
            {
                yield return null;

                Assert.That((bool)Invoke(unoffsetLensDisplay, "TrySetTexture", new object[] { unoffsetScopeTexture }), Is.True);
                Assert.That((bool)Invoke(offsetLensDisplay, "TrySetTexture", new object[] { offsetScopeTexture }), Is.True);

                var unoffsetProxy = unoffsetRoot.transform.Find("ScopeDisplayProxy");
                var offsetProxy = offsetRoot.transform.Find("ScopeDisplayProxy");
                Assert.That(unoffsetProxy, Is.Not.Null);
                Assert.That(offsetProxy, Is.Not.Null);

                var expectedWorldDelta =
                    offsetLensDisplay.transform.right * 0.004f +
                    offsetLensDisplay.transform.up * -0.006f +
                    offsetLensDisplay.transform.forward * 0.002f;
                var actualWorldDelta = offsetProxy.position - unoffsetProxy.position;

                Assert.That(Vector3.Distance(actualWorldDelta, expectedWorldDelta), Is.LessThan(0.0015f),
                    $"Proxy display local offset should move the proxy along the authored lens axes. Expected delta={expectedWorldDelta}, actual delta={actualWorldDelta}.");
            }
            finally
            {
                Cleanup(unoffsetRoot, unoffsetScopeTexture, offsetRoot, offsetScopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_ProxySurface_LeavesVisibleEyepieceMargin()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var root = CreateProxyOffsetFixture(scopeLensDisplayType, Vector3.zero, 1f, out var lensDisplay, out var scopeTexture);

            try
            {
                lensDisplay.transform.localScale = Vector3.one * 0.2f;

                yield return null;

                Assert.That((bool)Invoke(lensDisplay, "TrySetTexture", new object[] { scopeTexture }), Is.True);

                var proxySurface = root.transform.Find("ScopeDisplayProxy");
                Assert.That(proxySurface, Is.Not.Null);

                var proxyRenderer = proxySurface.GetComponent<Renderer>();
                var rootRenderer = root.GetComponent<Renderer>();
                Assert.That(proxyRenderer, Is.Not.Null);
                Assert.That(rootRenderer, Is.Not.Null);

                Assert.That(proxyRenderer.bounds.size.x, Is.LessThan(rootRenderer.bounds.size.x * 0.98f),
                    $"Proxy PiP should leave a visible horizontal eyepiece frame margin. Proxy width={proxyRenderer.bounds.size.x:0.0000}, body width={rootRenderer.bounds.size.x:0.0000}.");
                Assert.That(proxyRenderer.bounds.size.y, Is.LessThan(rootRenderer.bounds.size.y * 0.98f),
                    $"Proxy PiP should leave a visible vertical eyepiece frame margin. Proxy height={proxyRenderer.bounds.size.y:0.0000}, body height={rootRenderer.bounds.size.y:0.0000}.");
            }
            finally
            {
                Cleanup(root, scopeTexture);
            }
        }

        [UnityTest]
        public IEnumerator ScopeLensDisplay_ProxyDisplayScaleMultiplier_ScalesProxyAtRuntime()
        {
            var scopeLensDisplayType = ResolveType("Reloader.Game.Weapons.ScopeLensDisplay");
            Assert.That(scopeLensDisplayType, Is.Not.Null);

            var defaultRoot = CreateProxyOffsetFixture(scopeLensDisplayType, Vector3.zero, 1f, out var defaultLensDisplay, out var defaultScopeTexture);
            var scaledRoot = CreateProxyOffsetFixture(scopeLensDisplayType, Vector3.zero, 0.95f, out var scaledLensDisplay, out var scaledScopeTexture);

            try
            {
                yield return null;

                Assert.That((bool)Invoke(defaultLensDisplay, "TrySetTexture", new object[] { defaultScopeTexture }), Is.True);
                Assert.That((bool)Invoke(scaledLensDisplay, "TrySetTexture", new object[] { scaledScopeTexture }), Is.True);

                var defaultProxy = defaultRoot.transform.Find("ScopeDisplayProxy");
                var scaledProxy = scaledRoot.transform.Find("ScopeDisplayProxy");
                Assert.That(defaultProxy, Is.Not.Null);
                Assert.That(scaledProxy, Is.Not.Null);

                var defaultRenderer = defaultProxy.GetComponent<Renderer>();
                var scaledRenderer = scaledProxy.GetComponent<Renderer>();
                Assert.That(defaultRenderer, Is.Not.Null);
                Assert.That(scaledRenderer, Is.Not.Null);

                Assert.That(scaledRenderer.bounds.size.x, Is.EqualTo(defaultRenderer.bounds.size.x * 0.95f).Within(0.01f),
                    $"Proxy display scale multiplier should shrink proxy width proportionally. Default width={defaultRenderer.bounds.size.x:0.0000}, scaled width={scaledRenderer.bounds.size.x:0.0000}.");
                Assert.That(scaledRenderer.bounds.size.y, Is.EqualTo(defaultRenderer.bounds.size.y * 0.95f).Within(0.01f),
                    $"Proxy display scale multiplier should shrink proxy height proportionally. Default height={defaultRenderer.bounds.size.y:0.0000}, scaled height={scaledRenderer.bounds.size.y:0.0000}.");
            }
            finally
            {
                Cleanup(defaultRoot, defaultScopeTexture, scaledRoot, scaledScopeTexture);
            }
        }

        [Test]
        public void ScopeAdjustmentController_ClampsWindageAndElevationToMechanicalLimits()
        {
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);

            var root = new GameObject("ScopeAdjustmentRoot");
            try
            {
                var controller = root.AddComponent(scopeAdjustmentControllerType);
                SetField(controller, "_minWindageClicks", -2);
                SetField(controller, "_maxWindageClicks", 2);
                SetField(controller, "_minElevationClicks", -3);
                SetField(controller, "_maxElevationClicks", 3);

                Invoke(controller, "AdjustWindageClicks", 100);
                Invoke(controller, "AdjustElevationClicks", -100);

                Assert.That((int)GetProperty(controller, "CurrentWindageClicks"), Is.EqualTo(2));
                Assert.That((int)GetProperty(controller, "CurrentElevationClicks"), Is.EqualTo(-3));
            }
            finally
            {
                Cleanup(root);
            }
        }

        [Test]
        public void EquipOptic_ScopeAdjustmentController_RestoresStateAcrossReequip()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            var opticDefinition = CreateOpticDefinition("scope-adjust-persist", 4f, 12f, true, "RenderTexturePiP");

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                var opticPrefab = GetProperty(opticDefinition, "OpticPrefab") as GameObject;
                Assert.That(opticPrefab, Is.Not.Null);
                opticPrefab.AddComponent(scopeAdjustmentControllerType);

                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var firstController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Invoke(firstController, "AdjustWindageClicks", 2);
                Invoke(firstController, "AdjustElevationClicks", -1);

                Invoke(manager, "UnequipOptic");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);

                var secondController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Assert.That((int)GetProperty(secondController, "CurrentWindageClicks"), Is.EqualTo(2));
                Assert.That((int)GetProperty(secondController, "CurrentElevationClicks"), Is.EqualTo(-1));
            }
            finally
            {
                Cleanup(root, opticDefinition);
            }
        }

        [Test]
        public void EquipOptic_ScopeAdjustmentController_UsesPendingStateKeyForDistinctScopeInstances()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var scopeAdjustmentControllerType = ResolveType("Reloader.Game.Weapons.ScopeAdjustmentController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(scopeAdjustmentControllerType, Is.Not.Null);

            var root = new GameObject("AttachmentRoot");
            var opticDefinition = CreateOpticDefinition("scope-adjust-state-key", 4f, 12f, true, "RenderTexturePiP");

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                var opticPrefab = GetProperty(opticDefinition, "OpticPrefab") as GameObject;
                Assert.That(opticPrefab, Is.Not.Null);
                opticPrefab.AddComponent(scopeAdjustmentControllerType);

                Invoke(manager, "SetPendingOpticAdjustmentStateKey", "scope-a");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var firstController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Invoke(firstController, "AdjustWindageClicks", 3);
                Invoke(manager, "UnequipOptic");

                Invoke(manager, "SetPendingOpticAdjustmentStateKey", "scope-b");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var secondController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Assert.That((int)GetProperty(secondController, "CurrentWindageClicks"), Is.EqualTo(0));
                Invoke(secondController, "AdjustWindageClicks", -2);
                Invoke(manager, "UnequipOptic");

                Invoke(manager, "SetPendingOpticAdjustmentStateKey", "scope-a");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var restoredFirstController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Assert.That((int)GetProperty(restoredFirstController, "CurrentWindageClicks"), Is.EqualTo(3));

                Invoke(manager, "UnequipOptic");
                Invoke(manager, "SetPendingOpticAdjustmentStateKey", "scope-b");
                Assert.That((bool)Invoke(manager, "EquipOptic", opticDefinition), Is.True);
                var restoredSecondController = GetComponentInChildren(GetProperty(manager, "ActiveOpticInstance") as GameObject, scopeAdjustmentControllerType);
                Assert.That((int)GetProperty(restoredSecondController, "CurrentWindageClicks"), Is.EqualTo(-2));
            }
            finally
            {
                Cleanup(root, opticDefinition);
            }
        }

    }
}
