using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Weapons.Tests.PlayMode
{
    public partial class ScopeAttachmentAdsIntegrationPlayModeTests
    {
        [UnityTest]
        public IEnumerator ScopedReticle_FfpScalesWithMagnification()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                worldCamGo = new GameObject("WorldCam");
                var worldCamera = worldCamGo.AddComponent<Camera>();
                viewmodelCamGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();
                scopeCameraGo = new GameObject("ScopeCam");
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                scopedOptic = CreateOpticDefinition("scope-pip-ffp", 4f, 12f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-ffp");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent(reticleControllerType);
                reticleDefinition = CreateReticleDefinition("Ffp", 4f);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);
                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(scopeController, "CurrentCompositeReticleScale") - 1f) <= 0.01f,
                    20,
                    "FFP reticle did not settle to 1x scale at reference magnification.");
                var firstScale = (float)GetProperty(scopeController, "CurrentCompositeReticleScale");

                Invoke(ads, "SetMagnification", 8f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(scopeController, "CurrentCompositeReticleScale") - 2f) <= 0.05f,
                    20,
                    "FFP reticle did not scale with magnification.");
                var secondScale = (float)GetProperty(scopeController, "CurrentCompositeReticleScale");

                Assert.That(firstScale, Is.EqualTo(1f).Within(0.01f));
                Assert.That(secondScale, Is.EqualTo(2f).Within(0.05f));
            }
            finally
            {
                Cleanup(root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedReticle_FfpUpdatesWithMagnificationWhenUsingFixedScopeCameraFov()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                worldCamGo = new GameObject("WorldCam");
                var worldCamera = worldCamGo.AddComponent<Camera>();
                viewmodelCamGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();
                scopeCameraGo = new GameObject("ScopeCam");
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                scopedOptic = CreateOpticDefinition("scope-pip-fixed-profile-ffp", 4f, 12f, true, "RenderTexturePiP");
                SetField(scopedOptic, "_hasScopeRenderProfile", true);
                SetField(scopedOptic, "_scopeRenderProfile", CreateScopeRenderProfile(1024, 18f));

                opticPrefab = new GameObject("Optic_scope-pip-fixed-profile-ffp");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent(reticleControllerType);
                reticleDefinition = CreateReticleDefinition("Ffp", 4f);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(scopeController, "CurrentCompositeReticleScale") - 1f) <= 0.01f,
                    20,
                    "FFP reticle did not settle to 1x scale at reference magnification with fixed scope FOV.");

                Invoke(ads, "SetMagnification", 8f);
                yield return WaitUntil(
                    () => Mathf.Abs((float)GetProperty(scopeController, "CurrentCompositeReticleScale") - 2f) <= 0.05f,
                    20,
                    "FFP reticle did not update when magnification changed under a fixed scope camera FOV.");

                Assert.That((float)GetProperty(scopeController, "CurrentCompositeReticleScale"), Is.EqualTo(2f).Within(0.05f));
            }
            finally
            {
                Cleanup(root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedReticle_AssignsSpriteRendererSpriteFromDefinition()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;
            Texture2D reticleTexture = null;
            Sprite reticleSprite = null;

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                worldCamGo = new GameObject("WorldCam");
                var worldCamera = worldCamGo.AddComponent<Camera>();
                viewmodelCamGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();
                scopeCameraGo = new GameObject("ScopeCam");
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);

                scopedOptic = CreateOpticDefinition("scope-pip-reticle-sprite", 4f, 12f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-reticle-sprite");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent<SpriteRenderer>();
                reticleGo.AddComponent(reticleControllerType);
                reticleGo.transform.localScale = new Vector3(0.01f, 0.02f, 0.03f);

                reticleTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                reticleTexture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                reticleTexture.Apply();
                reticleSprite = Sprite.Create(reticleTexture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);

                reticleDefinition = CreateReticleDefinition("Sfp", 4f);
                SetField(reticleDefinition, "_reticleSprite", reticleSprite);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);
                var liveReticleController = GetComponentInChildren(activeOpticInstance, reticleControllerType);
                var liveSpriteRenderer = liveReticleController.GetComponent<SpriteRenderer>();
                Assert.That(liveSpriteRenderer, Is.Not.Null);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return null;
                yield return null;

                Assert.That(GetProperty(scopeController, "CurrentCompositeReticleSprite"), Is.SameAs(reticleSprite));
                Assert.That((bool)GetProperty(scopeController, "IsCompositeReticleActive"), Is.True);
                Assert.That(liveSpriteRenderer.sprite, Is.Null);
                Assert.That(liveSpriteRenderer.enabled, Is.False);
                Assert.That(liveReticleController.transform.localScale, Is.EqualTo(new Vector3(0.01f, 0.02f, 0.03f)));

                Invoke(ads, "SetAdsHeld", false);
                yield return WaitUntil(
                    () =>
                        liveSpriteRenderer.sprite == null
                        && !liveSpriteRenderer.enabled
                        && !(bool)GetProperty(scopeController, "IsCompositeReticleActive")
                        && GetProperty(scopeController, "CurrentCompositeReticleSprite") == null,
                    60,
                    "Reticle sprite renderer did not clear after leaving ADS.");

                Assert.That(liveSpriteRenderer.sprite, Is.Null);
                Assert.That(liveSpriteRenderer.enabled, Is.False);
                Assert.That(GetProperty(scopeController, "CurrentCompositeReticleSprite"), Is.Null);
                Assert.That(liveReticleController.transform.localScale, Is.EqualTo(new Vector3(0.01f, 0.02f, 0.03f)));
            }
            finally
            {
                Cleanup(reticleSprite, reticleTexture, root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedPipOptic_CompositeReticle_PreservesSpriteAspectRatio()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedPipReticleAspectRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;
            Texture2D reticleTexture = null;
            Sprite reticleSprite = null;

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                worldCamGo = new GameObject("WorldCam");
                var worldCamera = worldCamGo.AddComponent<Camera>();
                viewmodelCamGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();
                scopeCameraGo = new GameObject("ScopeCam");
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();
                scopeCamera.aspect = 1f;

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                scopedOptic = CreateOpticDefinition("scope-pip-reticle-aspect", 4f, 12f, true, "RenderTexturePiP");
                SetField(scopedOptic, "_compositeReticleScale", 0.5f);

                opticPrefab = new GameObject("Optic_scope-pip-reticle-aspect");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent<SpriteRenderer>();
                reticleGo.AddComponent(reticleControllerType);

                reticleTexture = new Texture2D(4, 2, TextureFormat.RGBA32, false);
                reticleTexture.SetPixels(new[]
                {
                    Color.white, Color.white, Color.white, Color.white,
                    Color.white, Color.white, Color.white, Color.white
                });
                reticleTexture.Apply();
                reticleSprite = Sprite.Create(reticleTexture, new Rect(0f, 0f, 4f, 2f), new Vector2(0.5f, 0.5f), 100f);

                reticleDefinition = CreateReticleDefinition("Sfp", 4f);
                SetField(reticleDefinition, "_reticleSprite", reticleSprite);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);

                yield return WaitUntil(
                    () =>
                    {
                        var drawScale = (Vector2)GetProperty(scopeController, "CurrentCompositeReticleDrawScale");
                        return (bool)GetProperty(scopeController, "IsCompositeReticleActive")
                               && Vector2.Distance(drawScale, new Vector2(0.5f, 0.25f)) <= 0.0001f;
                    },
                    60,
                    "PiP composite reticle did not preserve the authored sprite aspect ratio.");

                Assert.That(
                    Vector2.Distance((Vector2)GetProperty(scopeController, "CurrentCompositeReticleDrawScale"), new Vector2(0.5f, 0.25f)),
                    Is.LessThanOrEqualTo(0.0001f));
            }
            finally
            {
                Cleanup(reticleSprite, reticleTexture, root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

        [UnityTest]
        public IEnumerator ScopedReticle_SfpRemainsStableAcrossMagnification()
        {
            var attachmentManagerType = ResolveType("Reloader.Game.Weapons.AttachmentManager");
            var adsStateControllerType = ResolveType("Reloader.Game.Weapons.AdsStateController");
            var renderTextureScopeControllerType = ResolveType("Reloader.Game.Weapons.RenderTextureScopeController");
            var reticleControllerType = ResolveType("Reloader.Game.Weapons.ScopeReticleController");
            Assert.That(attachmentManagerType, Is.Not.Null);
            Assert.That(adsStateControllerType, Is.Not.Null);
            Assert.That(renderTextureScopeControllerType, Is.Not.Null);
            Assert.That(reticleControllerType, Is.Not.Null);

            var root = new GameObject("ScopedAdsRoot");
            ScriptableObject scopedOptic = null;
            ScriptableObject reticleDefinition = null;
            GameObject opticPrefab = null;
            GameObject worldCamGo = null;
            GameObject viewmodelCamGo = null;
            GameObject scopeCameraGo = null;

            try
            {
                var manager = root.AddComponent(attachmentManagerType);
                var scopeSlot = new GameObject("ScopeSlot").transform;
                scopeSlot.SetParent(root.transform, false);
                var ironAnchor = new GameObject("IronSightAnchor").transform;
                ironAnchor.SetParent(root.transform, false);
                SetField(manager, "_scopeSlot", scopeSlot);
                SetField(manager, "_ironSightAnchor", ironAnchor);

                worldCamGo = new GameObject("WorldCam");
                var worldCamera = worldCamGo.AddComponent<Camera>();
                viewmodelCamGo = new GameObject("ViewmodelCam");
                var viewmodelCamera = viewmodelCamGo.AddComponent<Camera>();
                scopeCameraGo = new GameObject("ScopeCam");
                var scopeCamera = scopeCameraGo.AddComponent<Camera>();

                var scopeController = root.AddComponent(renderTextureScopeControllerType);
                SetField(scopeController, "_scopeCamera", scopeCamera);

                var ads = root.AddComponent(adsStateControllerType);
                SetField(ads, "_worldCamera", worldCamera);
                SetField(ads, "_viewmodelCamera", viewmodelCamera);
                SetField(ads, "_attachmentManager", manager);
                SetField(ads, "_renderTextureScopeController", scopeController);
                SetField(ads, "_useLegacyInput", false);
                SetField(ads, "_magnificationLerpSpeed", 1000f);

                scopedOptic = CreateOpticDefinition("scope-pip-sfp", 4f, 12f, true, "RenderTexturePiP");
                opticPrefab = new GameObject("Optic_scope-pip-sfp");
                new GameObject("SightAnchor").transform.SetParent(opticPrefab.transform, false);
                var reticleGo = new GameObject("Reticle");
                reticleGo.transform.SetParent(opticPrefab.transform, false);
                reticleGo.AddComponent(reticleControllerType);
                reticleDefinition = CreateReticleDefinition("Sfp", 4f);
                SetField(scopedOptic, "_scopeReticleDefinition", reticleDefinition);
                SetField(scopedOptic, "_opticPrefab", opticPrefab);

                Assert.That((bool)Invoke(manager, "EquipOptic", scopedOptic), Is.True);
                var activeOpticInstance = GetProperty(manager, "ActiveOpticInstance") as GameObject;
                Assert.That(activeOpticInstance, Is.Not.Null);
                var liveReticleController = GetComponentInChildren(activeOpticInstance, reticleControllerType);
                Assert.That(liveReticleController, Is.Not.Null);

                Invoke(ads, "SetAdsHeld", true);
                Invoke(ads, "SetMagnification", 4f);
                yield return null;
                yield return null;
                var firstScale = (float)GetProperty(liveReticleController, "CurrentScale");

                Invoke(ads, "SetMagnification", 8f);
                yield return null;
                yield return null;
                var secondScale = (float)GetProperty(liveReticleController, "CurrentScale");

                Assert.That(firstScale, Is.EqualTo(1f).Within(0.01f));
                Assert.That(secondScale, Is.EqualTo(1f).Within(0.01f));
            }
            finally
            {
                Cleanup(root, scopedOptic, reticleDefinition, opticPrefab, worldCamGo, viewmodelCamGo, scopeCameraGo);
            }
        }

    }
}
