using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.Audio.Tests.PlayMode
{
    public class FootstepAndImpactAudioPlayModeTests
    {
        [UnityTest]
        public IEnumerator FootstepRouter_EmitsClip_ByLocomotionCadenceFromPlayerMover()
        {
            var footstepClip = AudioClip.Create("footstep", 128, 1, 44100, false);
            var catalog = CreateCatalogWithFootsteps(footstepClip);

            var go = new GameObject("PlayerWithFootstepAudio");
            var characterController = go.AddComponent<CharacterController>();
            Assert.That(characterController, Is.Not.Null);

            var playerMoverType = Type.GetType("Reloader.Player.PlayerMover, Reloader.Player");
            Assert.That(playerMoverType, Is.Not.Null, "PlayerMover type should resolve from Reloader.Player assembly.");
            var playerMover = go.AddComponent(playerMoverType);
            Assert.That(playerMover, Is.Not.Null);

            var router = go.AddComponent<FootstepAudioRouter>();
            SetPrivateField(router, "_catalog", catalog);
            SetPrivateField(router, "_surfaceId", "Default");
            SetPrivateField(router, "_metersPerStep", 1f);
            SetPrivateField(router, "_minimumSpeed", 0.1f);

            AudioClip playedClip = null;
            router.ClipPlayed += (_, clip, _) => playedClip = clip;

            var publishMethod = playerMoverType.GetMethod("PublishLocomotionFrame", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(publishMethod, Is.Not.Null, "PlayerMover should expose PublishLocomotionFrame for audio wiring.");

            publishMethod.Invoke(playerMover, new object[] { go.transform.position, new Vector3(0f, 0f, 4f), true, 0.1f });
            publishMethod.Invoke(playerMover, new object[] { go.transform.position, new Vector3(0f, 0f, 4f), true, 0.1f });
            publishMethod.Invoke(playerMover, new object[] { go.transform.position, new Vector3(0f, 0f, 4f), true, 0.1f });
            yield return null;

            Assert.That(playedClip, Is.SameAs(footstepClip));

            UnityEngine.Object.Destroy(go);
            UnityEngine.Object.Destroy(catalog);
            UnityEngine.Object.Destroy(footstepClip);
        }

        [UnityTest]
        public IEnumerator ProjectileImpact_UsesImpactRouterSurfaceMapping()
        {
            var defaultClip = AudioClip.Create("impact-default", 128, 1, 44100, false);
            var bodyClip = AudioClip.Create("impact-body", 128, 1, 44100, false);
            var catalog = CreateCatalogWithImpacts(defaultClip, bodyClip);

            var routerGo = new GameObject("ImpactAudioRouter");
            var router = routerGo.AddComponent<ImpactAudioRouter>();
            SetPrivateField(router, "_catalog", catalog);
            SetPrivateField(router, "_defaultSurfaceId", "Default");
            SetRouterTagSurfaceMappings(router, ("Player", "Body"));

            AudioClip playedClip = null;
            router.ClipPlayed += (_, clip, _) => playedClip = clip;

            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "ImpactTarget";
            target.tag = "Player";
            target.transform.position = new Vector3(0f, 0f, 4f);

            var projectileType = Type.GetType("Reloader.Weapons.Ballistics.WeaponProjectile, Reloader.Weapons");
            Assert.That(projectileType, Is.Not.Null, "WeaponProjectile type should resolve from Reloader.Weapons assembly.");

            var projectileGo = new GameObject("Projectile");
            projectileGo.transform.position = Vector3.zero;
            projectileGo.transform.forward = Vector3.forward;
            var projectile = projectileGo.AddComponent(projectileType);
            Assert.That(projectile, Is.Not.Null);

            var initializeMethod = projectileType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(initializeMethod, Is.Not.Null);
            initializeMethod.Invoke(projectile, new object[]
            {
                "weapon-kar98k",
                Vector3.forward,
                120f,
                0f,
                10f,
                2f,
                0.45f,
                null
            });

            var elapsed = 0f;
            while (playedClip == null && elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.That(playedClip, Is.SameAs(bodyClip));

            UnityEngine.Object.Destroy(projectileGo);
            UnityEngine.Object.Destroy(target);
            UnityEngine.Object.Destroy(routerGo);
            UnityEngine.Object.Destroy(catalog);
            UnityEngine.Object.Destroy(defaultClip);
            UnityEngine.Object.Destroy(bodyClip);
        }

        [UnityTest]
        public IEnumerator Projectile_Awake_CreatesRuntimeImpactRouter_WhenNoneExists()
        {
            var existingRouters = UnityEngine.Object.FindObjectsByType<ImpactAudioRouter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < existingRouters.Length; i++)
            {
                if (existingRouters[i] != null)
                {
                    UnityEngine.Object.Destroy(existingRouters[i].gameObject);
                }
            }

            yield return null;

            var projectileType = Type.GetType("Reloader.Weapons.Ballistics.WeaponProjectile, Reloader.Weapons");
            Assert.That(projectileType, Is.Not.Null, "WeaponProjectile type should resolve from Reloader.Weapons assembly.");

            var projectileGo = new GameObject("ProjectileNoRouter");
            projectileGo.AddComponent(projectileType);
            yield return null;

            var createdRouter = UnityEngine.Object.FindFirstObjectByType<ImpactAudioRouter>();
            Assert.That(createdRouter, Is.Not.Null, "Projectile should create a runtime impact router when none is present.");

            UnityEngine.Object.Destroy(projectileGo);
            if (createdRouter != null)
            {
                UnityEngine.Object.Destroy(createdRouter.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator FootstepRouter_RebindsToActiveMover_WhenCachedMoverIsInactive()
        {
            var playerMoverType = Type.GetType("Reloader.Player.PlayerMover, Reloader.Player");
            Assert.That(playerMoverType, Is.Not.Null);

            var inactivePlayer = new GameObject("InactivePlayer");
            inactivePlayer.AddComponent<CharacterController>();
            var inactiveMover = inactivePlayer.AddComponent(playerMoverType);
            inactivePlayer.SetActive(false);

            var activePlayer = new GameObject("ActivePlayer");
            activePlayer.AddComponent<CharacterController>();
            var activeMover = activePlayer.AddComponent(playerMoverType);

            var routerGo = new GameObject("FootstepRouter");
            var router = routerGo.AddComponent<FootstepAudioRouter>();
            SetPrivateField(router, "_playerMover", inactiveMover);
            SetPrivateField(router, "_subscribedMover", inactiveMover);

            InvokePrivateMethod(router, "TryBindPlayerMover", true);
            var subscribedMover = GetPrivateField(router, "_subscribedMover") as Component;
            Assert.That(subscribedMover, Is.Not.Null);
            Assert.That(subscribedMover, Is.EqualTo(activeMover));

            UnityEngine.Object.Destroy(routerGo);
            UnityEngine.Object.Destroy(activePlayer);
            UnityEngine.Object.Destroy(inactivePlayer);
            yield return null;
        }

        private static CombatAudioCatalog CreateCatalogWithFootsteps(AudioClip footstepClip)
        {
            var catalog = ScriptableObject.CreateInstance<CombatAudioCatalog>();
            SetSurfaceGroups(catalog, "_footstepGroups", ("Default", new[] { footstepClip }));
            return catalog;
        }

        private static CombatAudioCatalog CreateCatalogWithImpacts(AudioClip defaultClip, AudioClip bodyClip)
        {
            var catalog = ScriptableObject.CreateInstance<CombatAudioCatalog>();
            SetSurfaceGroups(catalog, "_impactGroups", ("Default", new[] { defaultClip }), ("Body", new[] { bodyClip }));
            return catalog;
        }

        private static void SetRouterTagSurfaceMappings(ImpactAudioRouter router, params (string tag, string surfaceId)[] mappings)
        {
            var mappingType = typeof(ImpactAudioRouter).GetNestedType("TagSurfaceMapping", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(mappingType, Is.Not.Null, "ImpactAudioRouter.TagSurfaceMapping type should exist.");

            var mappingArray = Array.CreateInstance(mappingType, mappings.Length);
            for (var i = 0; i < mappings.Length; i++)
            {
                var mapping = Activator.CreateInstance(mappingType);
                SetPrivateField(mapping, "_tag", mappings[i].tag);
                SetPrivateField(mapping, "_surfaceId", mappings[i].surfaceId);
                mappingArray.SetValue(mapping, i);
            }

            SetPrivateField(router, "_tagMappings", mappingArray);
        }

        private static void SetSurfaceGroups(CombatAudioCatalog catalog, string fieldName, params (string surfaceId, AudioClip[] clips)[] groups)
        {
            var groupType = typeof(CombatAudioCatalog).GetNestedType("SurfaceAudioGroup", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(groupType, Is.Not.Null);

            var values = Array.CreateInstance(groupType, groups.Length);
            for (var i = 0; i < groups.Length; i++)
            {
                var entry = Activator.CreateInstance(groupType);
                SetPrivateField(entry, "_surfaceId", groups[i].surfaceId);
                SetPrivateField(entry, "_clips", groups[i].clips);
                values.SetValue(entry, i);
            }

            SetPrivateField(catalog, fieldName, values);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = target.GetType().GetField(fieldName, flags);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = target.GetType().GetField(fieldName, flags);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {target.GetType().Name}.");
            return field.GetValue(target);
        }

        private static object InvokePrivateMethod(object target, string methodName, params object[] args)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var method = target.GetType().GetMethod(methodName, flags);
            Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found on {target.GetType().Name}.");
            return method.Invoke(target, args);
        }
    }
}
