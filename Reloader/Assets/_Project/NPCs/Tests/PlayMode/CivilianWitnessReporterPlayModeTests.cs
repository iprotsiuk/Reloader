using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Combat;
using Reloader.NPCs.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace Reloader.NPCs.Tests.PlayMode
{
    public sealed class CivilianWitnessReporterPlayModeTests
    {
        [UnityTest]
        public IEnumerator LethalImpact_WithConfiguredReporter_ReportsMurderOnce()
        {
            GameObject civilian = null;

            try
            {
                civilian = CreateWitnessReadyCivilian(out var receiver, out var witness);
                var reporter = new RecordingCrimeReporter();
                witness.Configure(reporter);

                yield return null;

                ApplyLethalDamage(receiver, civilian);
                yield return null;

                Assert.That(reporter.ReportCount, Is.EqualTo(1));
                Assert.That(reporter.LastCrimeType, Is.EqualTo(CrimeType.Murder));
            }
            finally
            {
                if (civilian != null)
                {
                    UnityEngine.Object.Destroy(civilian);
                }
            }
        }

        [UnityTest]
        public IEnumerator RepeatedLethalImpact_AfterDeath_DoesNotReportTwice()
        {
            GameObject civilian = null;

            try
            {
                civilian = CreateWitnessReadyCivilian(out var receiver, out var witness);
                var reporter = new RecordingCrimeReporter();
                witness.Configure(reporter);

                yield return null;

                ApplyLethalDamage(receiver, civilian);
                ApplyLethalDamage(receiver, civilian);
                yield return null;

                Assert.That(reporter.ReportCount, Is.EqualTo(1));
                Assert.That(reporter.LastCrimeType, Is.EqualTo(CrimeType.Murder));
            }
            finally
            {
                if (civilian != null)
                {
                    UnityEngine.Object.Destroy(civilian);
                }
            }
        }

        [UnityTest]
        public IEnumerator LethalImpact_WithoutConfiguredReporter_RemainsInert()
        {
            GameObject civilian = null;
            GameObject providerGo = null;

            try
            {
                providerGo = new GameObject("SceneCrimeReporter");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();
                civilian = CreateWitnessReadyCivilian(out var receiver, out _);

                yield return null;

                Assert.DoesNotThrow(() => ApplyLethalDamage(receiver, civilian));
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear),
                    "Expected an unconfigured witness to avoid scene fallback lookups and remain inert.");
            }
            finally
            {
                if (civilian != null)
                {
                    UnityEngine.Object.Destroy(civilian);
                }

                if (providerGo != null)
                {
                    UnityEngine.Object.Destroy(providerGo);
                }
            }
        }

        [UnityTest]
        public IEnumerator SerializedForeignDamageReceiver_WhenActivated_ReportsOnlySameObjectDeath()
        {
            GameObject civilian = null;
            GameObject foreignCivilian = null;

            try
            {
                civilian = new GameObject("CivilianWitnessReporterFixture");
                civilian.SetActive(false);
                var ownReceiver = civilian.AddComponent<HumanoidDamageReceiver>();
                var witness = civilian.AddComponent<CivilianWitnessReporter>();

                foreignCivilian = new GameObject("ForeignDamageReceiverFixture");
                var foreignReceiver = foreignCivilian.AddComponent<HumanoidDamageReceiver>();
                SetSerializedDamageReceiver(witness, foreignReceiver);

                var reporter = new RecordingCrimeReporter();
                witness.Configure(reporter);
                civilian.SetActive(true);
                yield return null;

                ApplyLethalDamage(foreignReceiver, foreignCivilian);
                yield return null;

                Assert.That(reporter.ReportCount, Is.EqualTo(0),
                    "Foreign damage receiver death must not be reported by this witness.");

                ApplyLethalDamage(ownReceiver, civilian);
                yield return null;

                Assert.That(reporter.ReportCount, Is.EqualTo(1));
                Assert.That(reporter.LastCrimeType, Is.EqualTo(CrimeType.Murder));
            }
            finally
            {
                if (civilian != null)
                {
                    UnityEngine.Object.Destroy(civilian);
                }

                if (foreignCivilian != null)
                {
                    UnityEngine.Object.Destroy(foreignCivilian);
                }
            }
        }

        [UnityTest]
        public IEnumerator SpawnedEligibleCivilianDeath_WithStaticCrimeReporter_ReportsMurderIntoPoliceHeat()
        {
            GameObject bridgeGo = null;
            GameObject providerGo = null;

            try
            {
                providerGo = new GameObject("StaticContractRuntimeProvider");
                var provider = providerGo.AddComponent<StaticContractRuntimeProvider>();

                bridgeGo = new GameObject("CivilianPopulationRuntimeBridge");
                var bridge = bridgeGo.AddComponent<CivilianPopulationRuntimeBridge>();
                bridge.ConfigureCrimeReporter(provider);
                CreateAnchor(bridgeGo.transform, "Anchor_Witness", new Vector3(1f, 0f, 0f));
                bridge.Runtime.Civilians.Add(new CivilianPopulationRecord
                {
                    CivilianId = "citizen.mainTown.heatWitness",
                    FirstName = "Marta",
                    LastName = "Novak",
                    PopulationSlotId = "townsfolk.heatWitness",
                    PoolId = "townsfolk",
                    SpawnAnchorId = "Anchor_Witness",
                    AreaTag = "maintown.square",
                    IsAlive = true,
                    IsContractEligible = false,
                    IsProtectedFromContracts = false
                });

                bridge.RebuildScenePopulation();
                yield return null;

                Assert.That(bridge.TryResolveSpawnedCivilian("citizen.mainTown.heatWitness", out var spawned), Is.True);
                Assert.That(spawned!.GetComponent<CivilianWitnessReporter>(), Is.Not.Null);

                ApplyLethalDamage(spawned.GetComponent<HumanoidDamageReceiver>(), spawned.gameObject);
                yield return null;

                Assert.That(provider.CurrentHeatState.Level, Is.Not.EqualTo(PoliceHeatLevel.Clear));
                Assert.That(provider.CurrentHeatState.LastCrimeType, Is.EqualTo(CrimeType.Murder));
                Assert.That(provider.CurrentHeatState.WantedLevel, Is.EqualTo(3));
            }
            finally
            {
                if (bridgeGo != null)
                {
                    UnityEngine.Object.Destroy(bridgeGo);
                }

                if (providerGo != null)
                {
                    UnityEngine.Object.Destroy(providerGo);
                }
            }
        }

        private static GameObject CreateWitnessReadyCivilian(
            out HumanoidDamageReceiver receiver,
            out CivilianWitnessReporter witness)
        {
            var civilian = new GameObject("CivilianWitnessReporterFixture");
            receiver = civilian.AddComponent<HumanoidDamageReceiver>();
            witness = civilian.AddComponent<CivilianWitnessReporter>();
            return civilian;
        }

        private static void ApplyLethalDamage(HumanoidDamageReceiver receiver, GameObject hitObject)
        {
            var payload = CreateImpactPayload(hitObject);
            typeof(HumanoidDamageReceiver).GetMethod(
                nameof(HumanoidDamageReceiver.ApplyDamage),
                BindingFlags.Instance | BindingFlags.Public)!.Invoke(receiver, new[] { payload });
        }

        private static void SetSerializedDamageReceiver(
            CivilianWitnessReporter witness,
            HumanoidDamageReceiver receiver)
        {
            typeof(CivilianWitnessReporter).GetField(
                "_damageReceiver",
                BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(witness, receiver);
        }

        private static object CreateImpactPayload(GameObject hitObject)
        {
            var payloadType = Type.GetType("Reloader.Weapons.Ballistics.ProjectileImpactPayload, Reloader.Weapons", throwOnError: false);
            Assert.That(payloadType, Is.Not.Null, "Expected ProjectileImpactPayload type.");

            var constructor = payloadType!.GetConstructor(new[]
            {
                typeof(string),
                typeof(Vector3),
                typeof(Vector3),
                typeof(float),
                typeof(GameObject)
            });
            Assert.That(constructor, Is.Not.Null, "Expected ProjectileImpactPayload(string, Vector3, Vector3, float, GameObject).");

            return constructor!.Invoke(new object[]
            {
                "test-rifle",
                hitObject.transform.position,
                Vector3.back,
                20f,
                hitObject
            });
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector3 position)
        {
            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = position;
            return anchor;
        }

        private sealed class RecordingCrimeReporter : ILawEnforcementCrimeReporter
        {
            public int ReportCount { get; private set; }
            public CrimeType LastCrimeType { get; private set; }

            public void ReportCrime(CrimeType crimeType)
            {
                ReportCount++;
                LastCrimeType = crimeType;
            }
        }
    }
}
