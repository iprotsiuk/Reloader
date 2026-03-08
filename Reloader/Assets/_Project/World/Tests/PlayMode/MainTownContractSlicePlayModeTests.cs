using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Reloader.World.Tests.PlayMode
{
    public class MainTownContractSlicePlayModeTests
    {
        private const string MainTownSceneName = "MainTown";
        private const float SceneSwitchTimeoutSeconds = 8f;

        [UnityTest]
        public IEnumerator MainTownContractSlice_AcceptsTargetEliminationAndRequiresExplicitClaimAfterSearchClears()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True, "Expected authored MainTown contract offer.");
            Assert.That(availableSnapshot.HasAvailableContract, Is.True);
            Assert.That(availableSnapshot.HasActiveContract, Is.False);
            Assert.That(availableSnapshot.TargetId, Is.EqualTo("target.maintown.volkov"));

            var targetRoot = GameObject.Find("ContractTarget_Volkov");
            Assert.That(targetRoot, Is.Not.Null, "Expected authored contract target root in MainTown.");

            var startingMoney = ReadEconomyMoney();

            Assert.That(provider.AcceptAvailableContract(), Is.True, "Expected scene contract provider to accept the authored offer.");
            Assert.That(provider.TryGetContractSnapshot(out var activeSnapshot), Is.True);
            Assert.That(activeSnapshot.HasAvailableContract, Is.False);
            Assert.That(activeSnapshot.HasActiveContract, Is.True);
            Assert.That(activeSnapshot.StatusText, Is.EqualTo("Active contract"));

            ApplyLethalDamage(targetRoot!);

            var runtime = GetRuntime(provider);
            Assert.That(ReadActiveContract(runtime), Is.Not.Null, "Target elimination should keep the contract active until payout resolves.");
            Assert.That(targetRoot.activeSelf, Is.False, "Contract target should disable itself after the lethal hit.");
            Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(ReadEconomyMoney(), Is.EqualTo(startingMoney), "Payout must stay gated while police heat remains active.");

            Assert.That(provider.TryGetContractSnapshot(out var escapeSnapshot), Is.True);
            Assert.That(escapeSnapshot.StatusText, Does.StartWith("Escape search:"));

            provider.AdvanceRuntime(31f);
            yield return null;

            Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
            Assert.That(ReadActiveContract(runtime), Is.Not.Null, "Contract should stay claimable until the reward is explicitly collected.");
            Assert.That(provider.TryGetContractSnapshot(out var readySnapshot), Is.True);
            Assert.That(readySnapshot.StatusText, Is.EqualTo("Ready to claim"));
            Assert.That(readySnapshot.CanClaimReward, Is.True);
            Assert.That(ReadEconomyMoney(), Is.EqualTo(startingMoney));

            Assert.That(provider.ClaimCompletedContractReward(), Is.True);

            Assert.That(ReadActiveContract(runtime), Is.Null, "Claiming the reward should complete the active contract.");
            Assert.That(provider.TryGetContractSnapshot(out _), Is.False, "Authored offer should stay consumed after the scene contract reward is claimed.");
            Assert.That(ReadEconomyMoney(), Is.EqualTo(startingMoney + activeSnapshot.Payout));
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_TargetEliminatedBeforeAccept_ConsumesOfferAndStartsSearch()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True, "Expected authored MainTown contract offer.");
            Assert.That(availableSnapshot.HasAvailableContract, Is.True);
            Assert.That(availableSnapshot.CanAccept, Is.True);

            var targetRoot = GameObject.Find("ContractTarget_Volkov");
            Assert.That(targetRoot, Is.Not.Null, "Expected authored contract target root in MainTown.");

            var startingMoney = ReadEconomyMoney();

            ApplyLethalDamage(targetRoot!);
            yield return null;

            var runtime = GetRuntime(provider);
            Assert.That(targetRoot.activeSelf, Is.False, "Contract target should disable itself after the lethal hit.");
            Assert.That(provider.TryGetContractSnapshot(out _), Is.False, "Dead authored targets must consume the scene offer before acceptance.");
            Assert.That(provider.AcceptAvailableContract(), Is.False, "The scene offer must not remain acceptible once the authored target is dead.");
            Assert.That(ReadActiveContract(runtime), Is.Null);
            Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(ReadEconomyMoney(), Is.EqualTo(startingMoney), "Pre-accept target kills must not award contract payout.");

            provider.AdvanceRuntime(31f);
            yield return null;

            Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
            Assert.That(provider.TryGetContractSnapshot(out _), Is.False);
            Assert.That(ReadActiveContract(runtime), Is.Null);
            Assert.That(ReadEconomyMoney(), Is.EqualTo(startingMoney));
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_ProceduralCivilianTarget_CanBeAcceptedAndCompleted()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var populationRoot = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(populationRoot, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = populationRoot!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");

            bridge!.Runtime.Civilians.Clear();
            bridge.Runtime.PendingReplacements.Clear();
            bridge.Runtime.Civilians.Add(CreateCivilianRecord(
                civilianId: "citizen.mainTown.7001",
                populationSlotId: "townsfolk.001",
                poolId: "townsfolk",
                spawnAnchorId: "Anchor_Townsfolk_01",
                areaTag: "maintown.square"));

            bridge.RebuildScenePopulation();
            yield return null;

            var spawned = populationRoot.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            Assert.That(spawned.Length, Is.EqualTo(1), "Expected one procedural civilian to rebuild into MainTown.");

            var proceduralCivilian = spawned[0].gameObject;
            var targetDamageableType = Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons");
            Assert.That(targetDamageableType, Is.Not.Null, "Expected contract target damageable type to resolve.");

            var targetDamageable = proceduralCivilian.GetComponent(targetDamageableType!);
            Assert.That(targetDamageable, Is.Not.Null, "Expected procedural civilian to expose the existing contract-target damageable seam.");

            const string expectedTargetId = "citizen.mainTown.7001";
            Assert.That(GetProperty<string>(targetDamageable!, "TargetId"), Is.EqualTo(expectedTargetId));

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");

            SetAvailableContract(provider!, CreateContractDefinition(
                contractId: "contract.procedural.maintown.7001",
                targetId: expectedTargetId,
                targetDisplayName: "citizen.mainTown.7001",
                payout: 1500));

            var startingMoney = ReadEconomyMoney();

            Assert.That(provider.AcceptAvailableContract(), Is.True, "Expected runtime-authored procedural civilian contract to be accepted.");
            Assert.That(provider.TryGetContractSnapshot(out var activeSnapshot), Is.True);
            Assert.That(activeSnapshot.HasActiveContract, Is.True);
            Assert.That(activeSnapshot.TargetId, Is.EqualTo(expectedTargetId));

            ApplyLethalDamage(proceduralCivilian);
            yield return null;

            var runtime = GetRuntime(provider);
            Assert.That(ReadActiveContract(runtime), Is.Not.Null, "Target elimination should keep the procedural contract active until payout resolves.");
            Assert.That(proceduralCivilian.activeSelf, Is.False, "Procedural contract target should disable itself after the lethal hit.");
            Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(ReadEconomyMoney(), Is.EqualTo(startingMoney));

            provider.AdvanceRuntime(31f);
            yield return null;

            Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Clear));
            Assert.That(provider.TryGetContractSnapshot(out var readySnapshot), Is.True);
            Assert.That(readySnapshot.StatusText, Is.EqualTo("Ready to claim"));
            Assert.That(readySnapshot.CanClaimReward, Is.True);

            Assert.That(provider.ClaimCompletedContractReward(), Is.True);
            Assert.That(ReadActiveContract(runtime), Is.Null);
            Assert.That(provider.TryGetContractSnapshot(out _), Is.False);
            Assert.That(ReadEconomyMoney(), Is.EqualTo(startingMoney + 1500));
        }

        private static ContractEscapeResolutionRuntime GetRuntime(StaticContractRuntimeProvider provider)
        {
            var runtimeField = typeof(StaticContractRuntimeProvider).GetField("_runtime", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(runtimeField, Is.Not.Null, "Expected private runtime field on StaticContractRuntimeProvider.");

            var runtime = runtimeField.GetValue(provider) as ContractEscapeResolutionRuntime;
            Assert.That(runtime, Is.Not.Null, "Expected scene contract provider to build its runtime.");
            return runtime!;
        }

        private static object ReadActiveContract(ContractEscapeResolutionRuntime runtime)
        {
            var activeContractProperty = typeof(ContractEscapeResolutionRuntime).GetProperty("ActiveContract", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(activeContractProperty, Is.Not.Null, "Expected ActiveContract property on contract runtime.");
            return activeContractProperty!.GetValue(runtime);
        }

        private static int ReadEconomyMoney()
        {
            var economyRoot = GameObject.Find("EconomyController");
            Assert.That(economyRoot, Is.Not.Null, "Expected authored EconomyController root in MainTown.");

            var economyType = Type.GetType("Reloader.Economy.EconomyController, Reloader.Economy");
            Assert.That(economyType, Is.Not.Null);

            var economyController = economyRoot!.GetComponent(economyType!);
            Assert.That(economyController, Is.Not.Null, "Expected EconomyController component on authored root.");

            var runtimeProperty = economyType!.GetProperty("Runtime", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(runtimeProperty, Is.Not.Null);

            var runtime = runtimeProperty!.GetValue(economyController);
            Assert.That(runtime, Is.Not.Null, "Expected EconomyController runtime to initialize in-scene.");

            var moneyProperty = runtime.GetType().GetProperty("Money", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(moneyProperty, Is.Not.Null);

            return (int)moneyProperty!.GetValue(runtime)!;
        }

        private static UnityEngine.Object CreateContractDefinition(
            string contractId,
            string targetId,
            string targetDisplayName,
            int payout)
        {
            var definitionType = Type.GetType("Reloader.Contracts.Runtime.AssassinationContractDefinition, Reloader.Contracts");
            Assert.That(definitionType, Is.Not.Null, "Expected AssassinationContractDefinition type to resolve.");

            var definition = ScriptableObject.CreateInstance(definitionType!);
            Assert.That(definition, Is.Not.Null, "Expected procedural contract definition to be created.");

            SetPrivateField(definitionType!, definition!, "_contractId", contractId);
            SetPrivateField(definitionType!, definition!, "_targetId", targetId);
            SetPrivateField(definitionType!, definition!, "_title", "Procedural Target");
            SetPrivateField(definitionType!, definition!, "_targetDisplayName", targetDisplayName);
            SetPrivateField(definitionType!, definition!, "_targetDescription", "Procedural civilian target");
            SetPrivateField(definitionType!, definition!, "_briefingText", "Locate and eliminate the procedural target.");
            SetPrivateField(definitionType!, definition!, "_distanceBand", 85f);
            SetPrivateField(definitionType!, definition!, "_payout", payout);
            return definition;
        }

        private static CivilianPopulationRecord CreateCivilianRecord(
            string civilianId,
            string populationSlotId,
            string poolId,
            string spawnAnchorId,
            string areaTag)
        {
            return new CivilianPopulationRecord
            {
                CivilianId = civilianId,
                PopulationSlotId = populationSlotId,
                PoolId = poolId,
                SpawnAnchorId = spawnAnchorId,
                AreaTag = areaTag,
                IsAlive = true,
                IsContractEligible = true,
                IsProtectedFromContracts = false,
                CreatedAtDay = 0,
                RetiredAtDay = -1
            };
        }

        private static void ApplyLethalDamage(GameObject targetRoot)
        {
            var damageableType = Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons");
            var impactPayloadType = Type.GetType("Reloader.Weapons.Ballistics.ProjectileImpactPayload, Reloader.Weapons");
            Assert.That(damageableType, Is.Not.Null);
            Assert.That(impactPayloadType, Is.Not.Null);

            var damageable = targetRoot.GetComponent(damageableType!);
            Assert.That(damageable, Is.Not.Null, "Expected ContractTargetDamageable on the authored MainTown target.");

            var payload = Activator.CreateInstance(
                impactPayloadType!,
                "weapon-kar98k",
                targetRoot.transform.position,
                Vector3.up,
                999f,
                targetRoot,
                (Vector3?)(targetRoot.transform.position + (Vector3.back * 250f)));
            Assert.That(payload, Is.Not.Null);

            var applyDamageMethod = damageableType!.GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(applyDamageMethod, Is.Not.Null);
            applyDamageMethod!.Invoke(damageable, new[] { payload });
        }

        private static void SetPrivateField(Type targetType, object instance, string fieldName, object value)
        {
            var field = targetType.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on {targetType.Name}.");
            field!.SetValue(instance, value);
        }

        private static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected public property '{propertyName}' on {instance.GetType().Name}.");
            return (T)property!.GetValue(instance)!;
        }

        private static void SetAvailableContract(StaticContractRuntimeProvider provider, UnityEngine.Object definition)
        {
            var method = typeof(StaticContractRuntimeProvider).GetMethod("SetAvailableContract", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Expected StaticContractRuntimeProvider.SetAvailableContract().");
            method!.Invoke(provider, new object[] { definition });
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            var elapsed = 0f;
            while (elapsed < SceneSwitchTimeoutSeconds)
            {
                var activeScene = SceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded && activeScene.name == sceneName)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail($"Timed out waiting for active scene '{sceneName}'.");
        }
    }
}
