using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public sealed class HumanoidHitboxRigPlayModeTests
    {
        [UnityTest]
        public IEnumerator ProjectileHit_OnHeadZoneCollider_RoutesThroughSharedHumanoidReceiverInsteadOfGenericRootDamageable()
        {
            var weaponProjectileType = ResolveType("Reloader.Weapons.Ballistics.WeaponProjectile", "Reloader.Weapons");
            var dummyTargetDamageableType = ResolveType("Reloader.Weapons.World.DummyTargetDamageable", "Reloader.Weapons");
            var bodyZoneHitboxType = ResolveType("Reloader.NPCs.Combat.BodyZoneHitbox", "Reloader.NPCs");
            var sharedReceiverType = ResolveType("Reloader.NPCs.Combat.HumanoidDamageReceiver", "Reloader.NPCs");
            var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");

            Assert.That(weaponProjectileType, Is.Not.Null, "Expected WeaponProjectile type for hit-routing coverage.");
            Assert.That(dummyTargetDamageableType, Is.Not.Null, "Expected DummyTargetDamageable as the generic root fallback seam.");
            Assert.That(bodyZoneHitboxType, Is.Not.Null, "Expected BodyZoneHitbox for semantic zone routing.");
            Assert.That(sharedReceiverType, Is.Not.Null, "Expected shared HumanoidDamageReceiver on NPC roots.");
            Assert.That(bodyZoneType, Is.Not.Null, "Expected HumanoidBodyZone enum for zone semantics.");

            GameObject npcRoot = null;
            GameObject headZone = null;
            GameObject projectileGo = null;
            GameObject markerPrefab = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.transform.position = new Vector3(0f, 0f, 6f);
                var rootCollider = npcRoot.AddComponent<BoxCollider>();
                rootCollider.size = new Vector3(1.5f, 1.8f, 1.5f);

                var rootDamageable = npcRoot.AddComponent(dummyTargetDamageableType!);
                markerPrefab = new GameObject("ImpactMarkerPrefab");
                SetPrivateField(rootDamageable, "_impactMarkerPrefab", markerPrefab);

                var sharedReceiver = npcRoot.AddComponent(sharedReceiverType!);

                headZone = new GameObject("HeadZone");
                headZone.transform.SetParent(npcRoot.transform, false);
                headZone.transform.localPosition = new Vector3(0f, 0f, -0.95f);
                headZone.AddComponent<SphereCollider>().radius = 0.3f;

                var headHitbox = headZone.AddComponent(bodyZoneHitboxType!);
                ConfigureBodyZoneHitbox(headHitbox, bodyZoneType!, "Head");

                var lastZoneProperty = sharedReceiverType!.GetProperty("LastZone", BindingFlags.Instance | BindingFlags.Public);
                var lastResultProperty = sharedReceiverType.GetProperty("LastResult", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(lastZoneProperty, Is.Not.Null, "Expected shared receiver to expose LastZone.");
                Assert.That(lastResultProperty, Is.Not.Null, "Expected shared receiver to expose LastResult.");

                projectileGo = new GameObject("Projectile");
                projectileGo.transform.position = Vector3.zero;
                projectileGo.transform.forward = Vector3.forward;
                var projectile = projectileGo.AddComponent(weaponProjectileType!);
                InitializeProjectile(projectile, "weapon-kar98k", Vector3.forward, speed: 240f, gravityMultiplier: 0f, damage: 25f);

                var elapsed = 0f;
                while (elapsed < 1f &&
                       !IsLastResultLethal(sharedReceiver, lastResultProperty!) &&
                       CountSpawnedMarkers(npcRoot, markerPrefab.name) == 0)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                Assert.That(CountSpawnedMarkers(npcRoot, markerPrefab.name), Is.EqualTo(0),
                    "Expected head-zone hitbox routing to bypass the generic root damageable.");

                var zone = lastZoneProperty!.GetValue(sharedReceiver);
                Assert.That(zone?.ToString(), Is.EqualTo("Head"));
                Assert.That(IsLastResultLethal(sharedReceiver, lastResultProperty!), Is.True);
            }
            finally
            {
                if (projectileGo != null)
                {
                    UnityEngine.Object.Destroy(projectileGo);
                }

                if (headZone != null)
                {
                    UnityEngine.Object.Destroy(headZone);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.Destroy(npcRoot);
                }

                if (markerPrefab != null)
                {
                    UnityEngine.Object.Destroy(markerPrefab);
                }
            }
        }

        [Test]
        public void BodyZoneHitbox_ConfigureAfterEnable_RebindsRigZoneLookup()
        {
            var hitboxRigType = ResolveType("Reloader.NPCs.Combat.HumanoidHitboxRig", "Reloader.NPCs");
            var bodyZoneHitboxType = ResolveType("Reloader.NPCs.Combat.BodyZoneHitbox", "Reloader.NPCs");
            var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");
            Assert.That(hitboxRigType, Is.Not.Null, "Expected HumanoidHitboxRig type.");
            Assert.That(bodyZoneHitboxType, Is.Not.Null, "Expected BodyZoneHitbox type.");
            Assert.That(bodyZoneType, Is.Not.Null, "Expected HumanoidBodyZone enum type.");

            GameObject root = null;
            GameObject zone = null;
            try
            {
                root = new GameObject("RigRoot");
                var rig = root.AddComponent(hitboxRigType!);

                zone = new GameObject("Zone");
                zone.transform.SetParent(root.transform, false);
                var hitbox = zone.AddComponent(bodyZoneHitboxType!);

                ConfigureBodyZoneHitbox(hitbox, bodyZoneType!, "Head");

                var headZone = Enum.Parse(bodyZoneType!, "Head");
                var torsoZone = Enum.Parse(bodyZoneType!, "Torso");
                Assert.That(TryGetRegisteredHitbox(rig, headZone, out var registeredHead), Is.True);
                Assert.That(registeredHead, Is.SameAs(hitbox));

                ConfigureBodyZoneHitbox(hitbox, bodyZoneType!, "Torso");

                Assert.That(TryGetRegisteredHitbox(rig, headZone, out _), Is.False,
                    "Expected previous zone registration to be removed after Configure changes body zone.");
                Assert.That(TryGetRegisteredHitbox(rig, torsoZone, out var registeredTorso), Is.True);
                Assert.That(registeredTorso, Is.SameAs(hitbox));
            }
            finally
            {
                if (zone != null)
                {
                    UnityEngine.Object.DestroyImmediate(zone);
                }

                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void ResolveBones_WithoutAnimator_NormalizesFallbackTokensForSeparatorNamedBones()
        {
            var hitboxRigType = ResolveType("Reloader.NPCs.Combat.HumanoidHitboxRig", "Reloader.NPCs");
            var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");
            Assert.That(hitboxRigType, Is.Not.Null, "Expected HumanoidHitboxRig type.");
            Assert.That(bodyZoneType, Is.Not.Null, "Expected HumanoidBodyZone enum type.");

            GameObject root = null;
            try
            {
                root = new GameObject("RigRoot");
                var pelvis = new GameObject("Pelvis");
                pelvis.transform.SetParent(root.transform, false);

                var spine = new GameObject("Spine");
                spine.transform.SetParent(pelvis.transform, false);

                var neck = new GameObject("Neck");
                neck.transform.SetParent(spine.transform, false);

                var head = new GameObject("Head");
                head.transform.SetParent(neck.transform, false);

                var armL = new GameObject("UpperArm_L");
                armL.transform.SetParent(spine.transform, false);

                var armR = new GameObject("UpperArm-R");
                armR.transform.SetParent(spine.transform, false);

                var legL = new GameObject("UpLeg_L");
                legL.transform.SetParent(pelvis.transform, false);

                var legR = new GameObject("UpLeg-R");
                legR.transform.SetParent(pelvis.transform, false);

                var rig = root.AddComponent(hitboxRigType!);
                var resolveBonesMethod = hitboxRigType!.GetMethod("ResolveBones", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(resolveBonesMethod, Is.Not.Null, "Expected ResolveBones() on HumanoidHitboxRig.");

                var resolved = resolveBonesMethod!.Invoke(rig, Array.Empty<object>());
                Assert.That(resolved, Is.EqualTo(true),
                    "Expected fallback bone-name resolution to handle underscore/hyphen variants when no humanoid Animator mapping is available.");

                Assert.That((bool)hitboxRigType.GetProperty("HasAllStandardBones", BindingFlags.Instance | BindingFlags.Public)!.GetValue(rig),
                    Is.True,
                    "Expected separator-named fallback bones to count toward a fully resolved rig.");

                AssertResolvedBone(rig, bodyZoneType!, "ArmL", armL.transform);
                AssertResolvedBone(rig, bodyZoneType!, "ArmR", armR.transform);
                AssertResolvedBone(rig, bodyZoneType!, "LegL", legL.transform);
                AssertResolvedBone(rig, bodyZoneType!, "LegR", legR.transform);
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void ResolveBones_WithoutAnimator_ResolvesStandardLeftRightUpperLimbNames()
        {
            var hitboxRigType = ResolveType("Reloader.NPCs.Combat.HumanoidHitboxRig", "Reloader.NPCs");
            var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");
            Assert.That(hitboxRigType, Is.Not.Null, "Expected HumanoidHitboxRig type.");
            Assert.That(bodyZoneType, Is.Not.Null, "Expected HumanoidBodyZone enum type.");

            GameObject root = null;
            try
            {
                root = new GameObject("RigRoot");
                var pelvis = new GameObject("Pelvis");
                pelvis.transform.SetParent(root.transform, false);

                var spine = new GameObject("Spine");
                spine.transform.SetParent(pelvis.transform, false);

                var neck = new GameObject("Neck");
                neck.transform.SetParent(spine.transform, false);

                var head = new GameObject("Head");
                head.transform.SetParent(neck.transform, false);

                var armL = new GameObject("LeftUpperArm");
                armL.transform.SetParent(spine.transform, false);

                var armR = new GameObject("RightUpperArm");
                armR.transform.SetParent(spine.transform, false);

                var legL = new GameObject("LeftUpperLeg");
                legL.transform.SetParent(pelvis.transform, false);

                var legR = new GameObject("RightUpperLeg");
                legR.transform.SetParent(pelvis.transform, false);

                var rig = root.AddComponent(hitboxRigType!);
                var resolveBonesMethod = hitboxRigType!.GetMethod("ResolveBones", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(resolveBonesMethod, Is.Not.Null, "Expected ResolveBones() on HumanoidHitboxRig.");

                var resolved = resolveBonesMethod!.Invoke(rig, Array.Empty<object>());
                Assert.That(resolved, Is.EqualTo(true),
                    "Expected fallback bone-name resolution to handle standard Left/RightUpperArm and Left/RightUpperLeg names when no humanoid Animator mapping is available.");

                AssertResolvedBone(rig, bodyZoneType!, "ArmL", armL.transform);
                AssertResolvedBone(rig, bodyZoneType!, "ArmR", armR.transform);
                AssertResolvedBone(rig, bodyZoneType!, "LegL", legL.transform);
                AssertResolvedBone(rig, bodyZoneType!, "LegR", legR.transform);
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void SharedReceiver_WhenExplicitLowEnergyIsProvided_DoesNotEscalateFromLegacyDamageFallback()
        {
            var sharedReceiverType = ResolveType("Reloader.NPCs.Combat.HumanoidDamageReceiver", "Reloader.NPCs");
            var bodyZoneHitboxType = ResolveType("Reloader.NPCs.Combat.BodyZoneHitbox", "Reloader.NPCs");
            var bodyZoneType = ResolveType("Reloader.NPCs.Combat.HumanoidBodyZone", "Reloader.NPCs");
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(sharedReceiverType, Is.Not.Null, "Expected shared humanoid receiver type.");
            Assert.That(bodyZoneHitboxType, Is.Not.Null, "Expected body-zone hitbox component type.");
            Assert.That(bodyZoneType, Is.Not.Null, "Expected HumanoidBodyZone enum type.");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload type.");

            GameObject npcRoot = null;
            GameObject torsoZone = null;
            try
            {
                var hitboxRigType = ResolveType("Reloader.NPCs.Combat.HumanoidHitboxRig", "Reloader.NPCs");
                npcRoot = new GameObject("NpcRoot");
                Assert.That(hitboxRigType, Is.Not.Null, "Expected HumanoidHitboxRig type.");
                npcRoot.AddComponent(hitboxRigType!);
                var sharedReceiver = npcRoot.AddComponent(sharedReceiverType!);

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                torsoZone.AddComponent<BoxCollider>();
                var hitbox = torsoZone.AddComponent(bodyZoneHitboxType!);
                ConfigureBodyZoneHitbox(hitbox, bodyZoneType!, "Torso");

                InvokeApplyDamage(sharedReceiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: torsoZone.transform.position,
                    normal: Vector3.up,
                    damage: 25f,
                    hitObject: torsoZone,
                    sourcePoint: torsoZone.transform.position + (Vector3.back * 50f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 0f,
                    projectileMassGrains: 0f,
                    deliveredEnergyJoules: 50f));

                var lastResultProperty = sharedReceiverType!.GetProperty("LastResult", BindingFlags.Instance | BindingFlags.Public);
                Assert.That(lastResultProperty, Is.Not.Null, "Expected shared receiver to expose LastResult.");
                Assert.That(IsLastResultLethal(sharedReceiver, lastResultProperty!), Is.False,
                    "Expected explicit low-energy payloads to stay non-lethal instead of being inflated by the legacy damage heuristic.");
            }
            finally
            {
                if (torsoZone != null)
                {
                    UnityEngine.Object.DestroyImmediate(torsoZone);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(npcRoot);
                }
            }
        }

        private static Type ResolveType(string fullName, string assemblyName)
        {
            var type = Type.GetType($"{fullName}, {assemblyName}", throwOnError: false);
            if (type != null)
            {
                return type;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on '{target.GetType().FullName}'.");
            field!.SetValue(target, value);
        }

        private static void ConfigureBodyZoneHitbox(Component bodyZoneHitbox, Type bodyZoneType, string zoneName)
        {
            var zoneValue = Enum.Parse(bodyZoneType, zoneName);
            var hitboxType = bodyZoneHitbox.GetType();

            var configureMethods = hitboxType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (var i = 0; i < configureMethods.Length; i++)
            {
                var method = configureMethods[i];
                if (!string.Equals(method.Name, "Configure", StringComparison.Ordinal))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == bodyZoneType)
                {
                    method.Invoke(bodyZoneHitbox, new[] { zoneValue });
                    return;
                }
            }

            if (TrySetZoneMember(bodyZoneHitbox, hitboxType, "_bodyZone", zoneValue) ||
                TrySetZoneMember(bodyZoneHitbox, hitboxType, "_zone", zoneValue) ||
                TrySetZoneMember(bodyZoneHitbox, hitboxType, "BodyZone", zoneValue) ||
                TrySetZoneMember(bodyZoneHitbox, hitboxType, "Zone", zoneValue))
            {
                return;
            }

            Assert.Fail("Expected BodyZoneHitbox to support assigning a HumanoidBodyZone value.");
        }

        private static bool TrySetZoneMember(object instance, Type type, string memberName, object value)
        {
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == value.GetType())
            {
                field.SetValue(instance, value);
                return true;
            }

            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null &&
                property.CanWrite &&
                property.PropertyType == value.GetType())
            {
                property.SetValue(instance, value);
                return true;
            }

            return false;
        }

        private static bool TryGetRegisteredHitbox(Component rig, object zoneValue, out object registeredHitbox)
        {
            registeredHitbox = null;
            var bodyZoneHitboxType = ResolveType("Reloader.NPCs.Combat.BodyZoneHitbox", "Reloader.NPCs");
            Assert.That(bodyZoneHitboxType, Is.Not.Null, "Expected BodyZoneHitbox type.");
            var method = rig.GetType().GetMethod(
                "TryGetHitbox",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { zoneValue.GetType(), bodyZoneHitboxType!.MakeByRefType() },
                modifiers: null);
            Assert.That(method, Is.Not.Null, "Expected TryGetHitbox(HumanoidBodyZone, out BodyZoneHitbox).");

            var args = new object[] { zoneValue, null };
            var found = (bool)method!.Invoke(rig, args);
            registeredHitbox = args[1];
            return found;
        }

        private static void AssertResolvedBone(Component rig, Type bodyZoneType, string zoneName, Transform expectedBone)
        {
            var zone = Enum.Parse(bodyZoneType, zoneName);
            var method = rig.GetType().GetMethod(
                "TryResolveBone",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { bodyZoneType, typeof(Transform).MakeByRefType() },
                modifiers: null);
            Assert.That(method, Is.Not.Null, "Expected TryResolveBone(HumanoidBodyZone, out Transform).");

            var args = new object[] { zone, null };
            var found = (bool)method!.Invoke(rig, args);
            Assert.That(found, Is.True, $"Expected zone '{zoneName}' to resolve a fallback bone.");
            Assert.That(args[1], Is.SameAs(expectedBone), $"Expected zone '{zoneName}' to bind the authored separator-named transform.");
        }

        private static void InitializeProjectile(
            Component projectile,
            string itemId,
            Vector3 direction,
            float speed,
            float gravityMultiplier,
            float damage)
        {
            var method = projectile.GetType().GetMethod(
                "Initialize",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[]
                {
                    typeof(string),
                    typeof(Vector3),
                    typeof(float),
                    typeof(float),
                    typeof(float),
                    typeof(float),
                    typeof(float),
                    typeof(Transform)
                },
                modifiers: null);

            Assert.That(method, Is.Not.Null, "Expected WeaponProjectile.Initialize signature for runtime routing coverage.");
            method!.Invoke(projectile, new object[] { itemId, direction, speed, gravityMultiplier, damage, 0.45f, 175f, null });
        }

        private static object CreateImpactPayload(
            Type payloadType,
            string itemId,
            Vector3 point,
            Vector3 normal,
            float damage,
            GameObject hitObject,
            Vector3? sourcePoint,
            Vector3? direction,
            float impactSpeedMetersPerSecond,
            float projectileMassGrains,
            float deliveredEnergyJoules)
        {
            return Activator.CreateInstance(
                payloadType,
                itemId,
                point,
                normal,
                damage,
                hitObject,
                sourcePoint,
                direction,
                impactSpeedMetersPerSecond,
                projectileMassGrains,
                deliveredEnergyJoules);
        }

        private static void InvokeApplyDamage(Component damageableComponent, object payload)
        {
            var payloadType = payload.GetType();
            var method = damageableComponent.GetType().GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { payloadType },
                modifiers: null);
            Assert.That(method, Is.Not.Null, "Expected ApplyDamage to exist on the shared receiver.");
            method!.Invoke(damageableComponent, new[] { payload });
        }

        private static bool IsLastResultLethal(object receiver, PropertyInfo lastResultProperty)
        {
            var result = lastResultProperty.GetValue(receiver);
            if (result == null)
            {
                return false;
            }

            var isLethalProperty = result.GetType().GetProperty("IsLethal", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(isLethalProperty, Is.Not.Null, "Expected LastResult to expose IsLethal.");
            var value = isLethalProperty!.GetValue(result);
            Assert.That(value, Is.Not.Null, "Expected IsLethal to return a value.");
            return (bool)value!;
        }

        private static int CountSpawnedMarkers(GameObject root, string markerBaseName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(includeInactive: true);
            var count = 0;
            for (var i = 0; i < transforms.Length; i++)
            {
                var candidate = transforms[i];
                if (candidate == null || candidate == root.transform)
                {
                    continue;
                }

                if (candidate.name.StartsWith(markerBaseName, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
