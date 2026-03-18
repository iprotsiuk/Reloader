using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Reloader.NPCs.Combat;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public class HumanoidBloodControllerPlayModeTests
    {
        [UnityTest]
        public IEnumerator ImpactResolution_MapsBodyZonesToSemanticImpactEffects()
        {
            var controllerType = ResolveType("Reloader.NPCs.Combat.HumanoidBloodController", "Reloader.NPCs");
            var effectKindType = ResolveType("Reloader.NPCs.Combat.BloodEffectKind", "Reloader.NPCs");
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(controllerType, Is.Not.Null, "Expected HumanoidBloodController to exist.");
            Assert.That(effectKindType, Is.Not.Null, "Expected BloodEffectKind enum to exist.");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            var cases = new[]
            {
                new ZoneEffectExpectation(HumanoidBodyZone.Head, "HeadImpact"),
                new ZoneEffectExpectation(HumanoidBodyZone.Neck, "NeckImpact"),
                new ZoneEffectExpectation(HumanoidBodyZone.Torso, "TorsoImpact"),
                new ZoneEffectExpectation(HumanoidBodyZone.ArmL, "ArmImpact"),
                new ZoneEffectExpectation(HumanoidBodyZone.LegR, "LegImpact")
            };

            for (var i = 0; i < cases.Length; i++)
            {
                var expectation = cases[i];
                GameObject npcRoot = null;
                GameObject zoneObject = null;
                try
                {
                    npcRoot = new GameObject($"NpcRoot-{expectation.Zone}");
                    npcRoot.AddComponent<HumanoidHitboxRig>();
                    var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                    var controller = npcRoot.AddComponent(controllerType!);

                    zoneObject = new GameObject($"{expectation.Zone}Zone");
                    zoneObject.transform.SetParent(npcRoot.transform, false);
                    zoneObject.AddComponent<BoxCollider>();
                    zoneObject.AddComponent<BodyZoneHitbox>().Configure(expectation.Zone);

                    yield return null;

                    InvokeApplyDamage(receiver, CreateImpactPayload(
                        projectileImpactPayloadType!,
                        itemId: "weapon-kar98k",
                        point: zoneObject.transform.position,
                        normal: Vector3.up,
                        damage: 1f,
                        hitObject: zoneObject,
                        sourcePoint: zoneObject.transform.position + (Vector3.back * 10f),
                        direction: Vector3.forward,
                        impactSpeedMetersPerSecond: 120f,
                        projectileMassGrains: 175f,
                        deliveredEnergyJoules: 100f));

                    var requestedEffects = ReadRequestedEffectNames(controller);
                    Assert.That(requestedEffects, Is.EquivalentTo(new[] { expectation.ExpectedEffectKindName }),
                        $"Expected {expectation.Zone} impact to request semantic blood effect '{expectation.ExpectedEffectKindName}'.");
                }
                finally
                {
                    if (zoneObject != null)
                    {
                        UnityEngine.Object.Destroy(zoneObject);
                    }

                    if (npcRoot != null)
                    {
                        UnityEngine.Object.Destroy(npcRoot);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator LethalImpact_RequestsImpactBloodAndDeathPuddle()
        {
            var controllerType = ResolveType("Reloader.NPCs.Combat.HumanoidBloodController", "Reloader.NPCs");
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(controllerType, Is.Not.Null, "Expected HumanoidBloodController to exist.");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            GameObject npcRoot = null;
            GameObject headZone = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var controller = npcRoot.AddComponent(controllerType!);

                headZone = new GameObject("HeadZone");
                headZone.transform.SetParent(npcRoot.transform, false);
                headZone.AddComponent<SphereCollider>();
                headZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Head);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: headZone.transform.position,
                    normal: Vector3.back,
                    damage: 1f,
                    hitObject: headZone,
                    sourcePoint: headZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                var requestedEffects = ReadRequestedEffectNames(controller);
                Assert.That(requestedEffects, Does.Contain("HeadImpact"),
                    "Expected lethal head impact to still request the impact blood effect.");
                Assert.That(requestedEffects, Does.Contain("DeathPuddle"),
                    "Expected lethal impact to request a follow-up death puddle effect.");
                var requestedPositions = ReadRequestedEffectPositions(controller);
                Assert.That(requestedPositions.Count, Is.EqualTo(requestedEffects.Count),
                    "Expected blood controller to record a position for each semantic request.");
                var expectedDeathPuddleRequestPosition = headZone.transform.position + (Vector3.back * 0.6f);
                Assert.That(requestedPositions[requestedPositions.Count - 1], Is.EqualTo(expectedDeathPuddleRequestPosition).Using(Vector3EqualityComparer.Instance),
                    "Expected death puddle request to offset from the lethal hit point toward the visible blood spray landing area.");
            }
            finally
            {
                if (headZone != null)
                {
                    UnityEngine.Object.Destroy(headZone);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.Destroy(npcRoot);
                }
            }
        }

        [UnityTest]
        public IEnumerator ImpactResolution_WithCatalogPrefab_InstantiatesConfiguredBloodPrefab()
        {
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            GameObject npcRoot = null;
            GameObject torsoZone = null;
            GameObject markerPrefab = null;
            BloodVfxCatalog catalog = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var controller = npcRoot.AddComponent<HumanoidBloodController>();

                markerPrefab = new GameObject("BloodMarkerTemplate");
                markerPrefab.transform.position = new Vector3(99f, 99f, 99f);
                var prefabMarker = markerPrefab.AddComponent<BloodPrefabMarker>();

                catalog = ScriptableObject.CreateInstance<BloodVfxCatalog>();
                ConfigureCatalogEntry(catalog, BloodEffectKind.TorsoImpact, markerPrefab);
                SetPrivateField(controller, "_catalog", catalog);

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                torsoZone.AddComponent<BoxCollider>();
                torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: torsoZone.transform.position,
                    normal: Vector3.up,
                    damage: 1f,
                    hitObject: torsoZone,
                    sourcePoint: torsoZone.transform.position + (Vector3.back * 10f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 120f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 100f));

                yield return null;

                var instantiatedMarker = FindInstantiatedMarker(prefabMarker, torsoZone.transform.position);
                Assert.That(instantiatedMarker, Is.Not.Null, "Expected HumanoidBloodController to instantiate the configured blood prefab.");
                Assert.That(instantiatedMarker!.gameObject.name, Is.EqualTo("BloodMarkerTemplate(Clone)"),
                    "Expected the instantiated blood object to be a clone of the configured prefab.");
                Assert.That(instantiatedMarker.transform.position, Is.EqualTo(torsoZone.transform.position).Using(Vector3EqualityComparer.Instance),
                    "Expected instantiated blood prefab to spawn at the impact point.");

                var requestedEffects = ReadRequestedEffectNames(controller);
                Assert.That(requestedEffects, Is.EquivalentTo(new[] { "TorsoImpact" }),
                    "Expected torso impact to still record the semantic effect request while spawning the prefab.");
            }
            finally
            {
                CleanupInstantiatedMarkers();

                if (markerPrefab != null)
                {
                    UnityEngine.Object.DestroyImmediate(markerPrefab);
                }

                if (catalog != null)
                {
                    UnityEngine.Object.DestroyImmediate(catalog);
                }

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

        [UnityTest]
        public IEnumerator ImpactResolution_WithCatalogPrefab_ParentsEffectToHitObjectForFollowMotion()
        {
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            GameObject npcRoot = null;
            GameObject torsoZone = null;
            GameObject markerPrefab = null;
            BloodVfxCatalog catalog = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                npcRoot.AddComponent<HumanoidBloodController>();

                torsoZone = new GameObject("TorsoZone");
                torsoZone.transform.SetParent(npcRoot.transform, false);
                torsoZone.AddComponent<BoxCollider>();
                torsoZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Torso);

                markerPrefab = new GameObject("BloodFollowMarkerTemplate");
                var prefabMarker = markerPrefab.AddComponent<BloodPrefabMarker>();

                catalog = ScriptableObject.CreateInstance<BloodVfxCatalog>();
                ConfigureCatalogEntry(catalog, BloodEffectKind.TorsoImpact, markerPrefab);
                SetPrivateField(npcRoot.GetComponent<HumanoidBloodController>(), "_catalog", catalog);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: torsoZone.transform.position,
                    normal: Vector3.up,
                    damage: 1f,
                    hitObject: torsoZone,
                    sourcePoint: torsoZone.transform.position + (Vector3.back * 10f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 120f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 100f));

                yield return null;

                var instantiatedMarker = FindInstantiatedMarker(prefabMarker, torsoZone.transform.position);
                Assert.That(instantiatedMarker, Is.Not.Null, "Expected HumanoidBloodController to instantiate the configured blood prefab.");
                Assert.That(instantiatedMarker!.transform.parent == torsoZone.transform, Is.True,
                    "Expected impact blood to stay attached to the struck hit object so it follows the body during ragdoll motion.");

                torsoZone.transform.position += new Vector3(0.25f, -0.5f, 0.4f);
                yield return null;

                Assert.That(instantiatedMarker.transform.position, Is.EqualTo(torsoZone.transform.position).Using(Vector3EqualityComparer.Instance),
                    "Expected the spawned impact blood to move with the struck hit object after impact.");
            }
            finally
            {
                CleanupInstantiatedMarkers();

                if (markerPrefab != null)
                {
                    UnityEngine.Object.DestroyImmediate(markerPrefab);
                }

                if (catalog != null)
                {
                    UnityEngine.Object.DestroyImmediate(catalog);
                }

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

        [UnityTest]
        public IEnumerator ImpactResolution_WithRootHitObject_AttachesEffectToResolvedZoneBone()
        {
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            GameObject npcRoot = null;
            GameObject bodyRoot = null;
            GameObject pelvisBone = null;
            GameObject torsoBone = null;
            GameObject markerPrefab = null;
            BloodVfxCatalog catalog = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                var hitboxRig = npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var controller = npcRoot.AddComponent<HumanoidBloodController>();

                bodyRoot = new GameObject("Body");
                bodyRoot.transform.SetParent(npcRoot.transform, false);
                bodyRoot.AddComponent<CapsuleCollider>();

                pelvisBone = new GameObject("PelvisBone");
                pelvisBone.transform.SetParent(npcRoot.transform, false);

                torsoBone = new GameObject("TorsoBone");
                torsoBone.transform.SetParent(pelvisBone.transform, false);
                SetPrivateField(hitboxRig, "_pelvis", pelvisBone.transform);
                SetPrivateField(hitboxRig, "_torso", torsoBone.transform);
                hitboxRig.ResolveBones();

                markerPrefab = new GameObject("BloodZoneAnchorMarkerTemplate");
                var prefabMarker = markerPrefab.AddComponent<BloodPrefabMarker>();

                catalog = ScriptableObject.CreateInstance<BloodVfxCatalog>();
                ConfigureCatalogEntry(catalog, BloodEffectKind.TorsoImpact, markerPrefab);
                SetPrivateField(controller, "_catalog", catalog);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: bodyRoot.transform.position,
                    normal: Vector3.up,
                    damage: 1f,
                    hitObject: bodyRoot,
                    sourcePoint: bodyRoot.transform.position + (Vector3.back * 10f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 120f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 100f));

                Assert.That(hitboxRig.TryResolveBone(HumanoidBodyZone.Torso, out var resolvedTorsoBone), Is.True,
                    "Expected HumanoidHitboxRig to resolve a torso bone for blood anchoring.");
                Assert.That(resolvedTorsoBone == torsoBone.transform, Is.True,
                    "Expected HumanoidHitboxRig to preserve the authored torso bone assignment for blood anchoring.");
                Assert.That(receiver.HitboxRig, Is.Not.Null, "Expected HumanoidDamageReceiver to retain the authored HumanoidHitboxRig.");
                var expectedAnchor = receiver.HitboxRig!.GetBoneOrNull(receiver.LastZone);
                Assert.That(expectedAnchor, Is.Not.Null,
                    "Expected root body collider hits to resolve to an authored humanoid zone bone for blood anchoring.");

                var resolveAnchorMethod = typeof(HumanoidBloodController).GetMethod("ResolveImpactAnchorTransform", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(resolveAnchorMethod, Is.Not.Null, "Expected HumanoidBloodController to define ResolveImpactAnchorTransform.");
                var resolvedAnchor = resolveAnchorMethod!.Invoke(controller, Array.Empty<object>()) as Transform;
                Assert.That(resolvedAnchor == expectedAnchor, Is.True,
                    "Expected HumanoidBloodController to resolve the classified humanoid zone bone before spawning blood.");

                yield return null;

                var instantiatedMarker = FindInstantiatedMarker(prefabMarker, bodyRoot.transform.position);
                Assert.That(instantiatedMarker, Is.Not.Null, "Expected HumanoidBloodController to instantiate the configured blood prefab.");
                Assert.That(instantiatedMarker!.transform.parent == expectedAnchor, Is.True,
                    "Expected impact blood to anchor to the resolved humanoid zone bone when the projectile hit the live root body collider.");
            }
            finally
            {
                CleanupInstantiatedMarkers();

                if (markerPrefab != null)
                {
                    UnityEngine.Object.DestroyImmediate(markerPrefab);
                }

                if (catalog != null)
                {
                    UnityEngine.Object.DestroyImmediate(catalog);
                }

                if (torsoBone != null)
                {
                    UnityEngine.Object.DestroyImmediate(torsoBone);
                }

                if (pelvisBone != null)
                {
                    UnityEngine.Object.DestroyImmediate(pelvisBone);
                }

                if (bodyRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(bodyRoot);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(npcRoot);
                }
            }
        }

        [UnityTest]
        public IEnumerator LethalImpact_WithoutDeathPuddlePrefab_SpawnsMaterialBackedPuddle()
        {
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            GameObject npcRoot = null;
            GameObject bodyRoot = null;
            GameObject headZone = null;
            GameObject ground = null;
            BloodVfxCatalog catalog = null;
            Material puddleMaterial = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var controller = npcRoot.AddComponent<HumanoidBloodController>();

                bodyRoot = new GameObject("Body");
                bodyRoot.transform.SetParent(npcRoot.transform, false);
                bodyRoot.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                var bodyCollider = bodyRoot.AddComponent<BoxCollider>();
                bodyCollider.size = new Vector3(0.6f, 0.4f, 0.6f);

                headZone = new GameObject("HeadZone");
                headZone.transform.SetParent(npcRoot.transform, false);
                headZone.transform.localPosition = Vector3.up;
                headZone.AddComponent<SphereCollider>();
                headZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Head);

                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;

                catalog = ScriptableObject.CreateInstance<BloodVfxCatalog>();
                ConfigureCatalogEntry(catalog, BloodEffectKind.HeadImpact, headZone);
                SetPrivateField(controller, "_catalog", catalog);

                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Assert.That(shader, Is.Not.Null, "Expected a test shader to create a temporary puddle material.");
                puddleMaterial = new Material(shader) { color = Color.red };
                SetPrivateField(controller, "_deathPuddleMaterial", puddleMaterial);

                yield return null;

                var impactNormal = Vector3.right;
                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: headZone.transform.position,
                    normal: impactNormal,
                    damage: 1f,
                    hitObject: headZone,
                    sourcePoint: headZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return null;

                var puddle = GameObject.Find("BloodPuddle");
                Assert.That(puddle, Is.Not.Null, "Expected lethal hit without a death puddle prefab to create a material-backed puddle fallback.");
                var renderer = puddle!.GetComponent<MeshRenderer>();
                Assert.That(renderer, Is.Not.Null, "Expected blood puddle fallback to include a renderer.");
                Assert.That(renderer!.sharedMaterial, Is.EqualTo(puddleMaterial),
                    "Expected blood puddle fallback to use the configured death puddle material.");
                Assert.That(puddle.transform.position.y, Is.EqualTo(0.02f).Within(0.05f),
                    "Expected blood puddle fallback to project onto the ground surface instead of staying at the standing NPC root height.");
                Assert.That(puddle.transform.position.y, Is.LessThan(bodyCollider.bounds.min.y - 0.01f),
                    "Expected blood puddle fallback to resolve below the corpse volume rather than on top of the body collider.");
            }
            finally
            {
                var puddle = GameObject.Find("BloodPuddle");
                if (puddle != null)
                {
                    UnityEngine.Object.DestroyImmediate(puddle);
                }

                if (puddleMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(puddleMaterial);
                }

                if (catalog != null)
                {
                    UnityEngine.Object.DestroyImmediate(catalog);
                }

                if (headZone != null)
                {
                    UnityEngine.Object.DestroyImmediate(headZone);
                }

                if (bodyRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(bodyRoot);
                }

                if (ground != null)
                {
                    UnityEngine.Object.DestroyImmediate(ground);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(npcRoot);
                }
            }
        }

        [UnityTest]
        public IEnumerator LethalImpact_WithCatalogDeathPuddlePrefab_ProjectsPrefabOntoGround()
        {
            var projectileImpactPayloadType = ResolveType("Reloader.Weapons.Ballistics.ProjectileImpactPayload", "Reloader.Weapons");
            Assert.That(projectileImpactPayloadType, Is.Not.Null, "Expected ProjectileImpactPayload to exist.");

            GameObject npcRoot = null;
            GameObject headZone = null;
            GameObject ground = null;
            GameObject impactPrefab = null;
            GameObject deathPuddlePrefab = null;
            BloodVfxCatalog catalog = null;
            try
            {
                npcRoot = new GameObject("NpcRoot");
                npcRoot.transform.position = new Vector3(2f, 1f, -1f);
                npcRoot.AddComponent<HumanoidHitboxRig>();
                var receiver = npcRoot.AddComponent<HumanoidDamageReceiver>();
                var controller = npcRoot.AddComponent<HumanoidBloodController>();

                headZone = new GameObject("HeadZone");
                headZone.transform.SetParent(npcRoot.transform, false);
                headZone.transform.localPosition = new Vector3(0.75f, 1f, 0.35f);
                headZone.AddComponent<SphereCollider>();
                headZone.AddComponent<BodyZoneHitbox>().Configure(HumanoidBodyZone.Head);

                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;

                impactPrefab = new GameObject("ImpactMarkerTemplate");
                impactPrefab.AddComponent<BloodPrefabMarker>();
                deathPuddlePrefab = new GameObject("DeathPuddleMarkerTemplate");
                var puddleMarker = deathPuddlePrefab.AddComponent<BloodPrefabMarker>();

                catalog = ScriptableObject.CreateInstance<BloodVfxCatalog>();
                ConfigureCatalogEntries(
                    catalog,
                    (BloodEffectKind.HeadImpact, impactPrefab),
                    (BloodEffectKind.DeathPuddle, deathPuddlePrefab));
                SetPrivateField(controller, "_catalog", catalog);

                yield return null;

                InvokeApplyDamage(receiver, CreateImpactPayload(
                    projectileImpactPayloadType!,
                    itemId: "weapon-kar98k",
                    point: headZone.transform.position,
                    normal: Vector3.right,
                    damage: 1f,
                    hitObject: headZone,
                    sourcePoint: headZone.transform.position + (Vector3.back * 25f),
                    direction: Vector3.forward,
                    impactSpeedMetersPerSecond: 240f,
                    projectileMassGrains: 175f,
                    deliveredEnergyJoules: 900f));

                yield return null;

                var expectedGroundPuddlePosition = new Vector3(
                    headZone.transform.position.x + 0.6f,
                    0.02f,
                    headZone.transform.position.z);
                var instantiatedPuddle = FindInstantiatedMarker(puddleMarker, expectedGroundPuddlePosition, tolerance: 0.08f);
                Assert.That(instantiatedPuddle, Is.Not.Null,
                    "Expected lethal impact with an authored death puddle prefab to project that prefab onto the ground near where the blood fountain would land, not straight under the body.");
            }
            finally
            {
                CleanupInstantiatedMarkers();

                if (catalog != null)
                {
                    UnityEngine.Object.DestroyImmediate(catalog);
                }

                if (impactPrefab != null)
                {
                    UnityEngine.Object.DestroyImmediate(impactPrefab);
                }

                if (deathPuddlePrefab != null)
                {
                    UnityEngine.Object.DestroyImmediate(deathPuddlePrefab);
                }

                if (headZone != null)
                {
                    UnityEngine.Object.DestroyImmediate(headZone);
                }

                if (ground != null)
                {
                    UnityEngine.Object.DestroyImmediate(ground);
                }

                if (npcRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(npcRoot);
                }
            }
        }

        private static IReadOnlyList<string> ReadRequestedEffectNames(Component controller)
        {
            var requestsProperty = controller.GetType().GetProperty("RequestedEffects", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(requestsProperty, Is.Not.Null, "Expected HumanoidBloodController to expose RequestedEffects for semantic verification.");

            var value = requestsProperty!.GetValue(controller);
            Assert.That(value, Is.Not.Null, "Expected RequestedEffects to return a collection.");
            Assert.That(value, Is.InstanceOf<System.Collections.IEnumerable>(), "Expected RequestedEffects to be enumerable.");

            var names = new List<string>();
            foreach (var effect in (System.Collections.IEnumerable)value)
            {
                if (effect == null)
                {
                    continue;
                }

                names.Add(effect.ToString());
            }

            return names;
        }

        private static IReadOnlyList<Vector3> ReadRequestedEffectPositions(Component controller)
        {
            var positionsProperty = controller.GetType().GetProperty("RequestedEffectPositions", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(positionsProperty, Is.Not.Null, "Expected HumanoidBloodController to expose RequestedEffectPositions for placement verification.");

            var value = positionsProperty!.GetValue(controller);
            Assert.That(value, Is.Not.Null, "Expected RequestedEffectPositions to return a collection.");
            Assert.That(value, Is.InstanceOf<System.Collections.IEnumerable>(), "Expected RequestedEffectPositions to be enumerable.");

            var positions = new List<Vector3>();
            foreach (var position in (System.Collections.IEnumerable)value)
            {
                if (position is Vector3 vector)
                {
                    positions.Add(vector);
                }
            }

            return positions;
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

        private static void InvokeApplyDamage(Component receiver, object payload)
        {
            var payloadType = payload.GetType();
            var method = receiver.GetType().GetMethod(
                "ApplyDamage",
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                types: new[] { payloadType },
                modifiers: null);
            Assert.That(method, Is.Not.Null, "Expected HumanoidDamageReceiver.ApplyDamage to exist.");
            method!.Invoke(receiver, new[] { payload });
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

        private static void ConfigureCatalogEntry(BloodVfxCatalog catalog, BloodEffectKind effectKind, GameObject prefab)
        {
            ConfigureCatalogEntries(catalog, (effectKind, prefab));
        }

        private static void ConfigureCatalogEntries(BloodVfxCatalog catalog, params (BloodEffectKind Kind, GameObject Prefab)[] configuredEntries)
        {
            Assert.That(catalog, Is.Not.Null);
            Assert.That(configuredEntries, Is.Not.Null);

            var entryType = typeof(BloodVfxCatalog).GetNestedType("BloodEffectEntry", BindingFlags.NonPublic);
            Assert.That(entryType, Is.Not.Null, "Expected BloodVfxCatalog to declare a private BloodEffectEntry type.");

            var kindField = entryType!.GetField("Kind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var prefabField = entryType.GetField("Prefab", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(kindField, Is.Not.Null, "Expected BloodEffectEntry.Kind field.");
            Assert.That(prefabField, Is.Not.Null, "Expected BloodEffectEntry.Prefab field.");

            var entries = Array.CreateInstance(entryType!, configuredEntries.Length);
            for (var i = 0; i < configuredEntries.Length; i++)
            {
                var entry = Activator.CreateInstance(entryType!);
                kindField!.SetValue(entry, configuredEntries[i].Kind);
                prefabField!.SetValue(entry, configuredEntries[i].Prefab);
                entries.SetValue(entry, i);
            }

            SetPrivateField(catalog, "_effectEntries", entries);
        }

        private static BloodPrefabMarker FindInstantiatedMarker(BloodPrefabMarker prefabMarker, Vector3 expectedPosition, float tolerance = 0.0001f)
        {
            var markers = UnityEngine.Object.FindObjectsByType<BloodPrefabMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                if (marker == null || marker == prefabMarker)
                {
                    continue;
                }

                if ((marker.transform.position - expectedPosition).sqrMagnitude <= (tolerance * tolerance))
                {
                    return marker;
                }
            }

            return null;
        }

        private static void CleanupInstantiatedMarkers()
        {
            var markers = UnityEngine.Object.FindObjectsByType<BloodPrefabMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < markers.Length; i++)
            {
                if (markers[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(markers[i].gameObject);
                }
            }
        }


        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field!.SetValue(target, value);
        }

        private readonly struct ZoneEffectExpectation
        {
            public ZoneEffectExpectation(HumanoidBodyZone zone, string expectedEffectKindName)
            {
                Zone = zone;
                ExpectedEffectKindName = expectedEffectKindName;
            }

            public HumanoidBodyZone Zone { get; }
            public string ExpectedEffectKindName { get; }
        }

        private sealed class Vector3EqualityComparer : IEqualityComparer<Vector3>
        {
            public static readonly Vector3EqualityComparer Instance = new Vector3EqualityComparer();

            public bool Equals(Vector3 x, Vector3 y)
            {
                return Vector3.Distance(x, y) <= 0.0001f;
            }

            public int GetHashCode(Vector3 obj)
            {
                return obj.GetHashCode();
            }
        }

        private sealed class BloodPrefabMarker : MonoBehaviour
        {
        }
    }
}
