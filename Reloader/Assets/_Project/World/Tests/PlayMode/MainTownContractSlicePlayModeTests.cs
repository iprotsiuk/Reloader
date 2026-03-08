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
            Assert.That(availableSnapshot.TargetId, Does.StartWith("citizen.mainTown."), "Expected MainTown to offer a live procedural civilian target on scene load.");

            var targetRoot = FindProceduralCivilianTarget(availableSnapshot.TargetId);
            Assert.That(targetRoot, Is.Not.Null, "Expected the available contract target to resolve to a spawned procedural civilian.");

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

            var targetRoot = FindProceduralCivilianTarget(availableSnapshot.TargetId);
            Assert.That(targetRoot, Is.Not.Null, "Expected the available contract target to resolve to a spawned procedural civilian.");

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

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");

            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True);
            Assert.That(availableSnapshot.HasAvailableContract, Is.True);
            Assert.That(availableSnapshot.TargetId, Does.StartWith("citizen.mainTown."));

            var offeredTarget = FindProceduralCivilianTarget(availableSnapshot.TargetId);
            Assert.That(offeredTarget, Is.Not.Null, "Expected the scene's available contract to target a spawned procedural civilian.");

            var targetDamageableType = Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons");
            Assert.That(targetDamageableType, Is.Not.Null, "Expected contract target damageable type to resolve.");
            var targetDamageable = offeredTarget!.GetComponent(targetDamageableType!);
            Assert.That(targetDamageable, Is.Not.Null, "Expected procedural offer target to expose the existing contract-target damageable seam.");
            Assert.That(GetProperty<string>(targetDamageable!, "TargetId"), Is.EqualTo(availableSnapshot.TargetId));
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

        private static T GetProperty<T>(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(property, Is.Not.Null, $"Expected public property '{propertyName}' on {instance.GetType().Name}.");
            return (T)property!.GetValue(instance)!;
        }

        private static GameObject FindProceduralCivilianTarget(string targetId)
        {
            var populationRoot = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(populationRoot, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var spawned = populationRoot!.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            for (var i = 0; i < spawned.Length; i++)
            {
                if (string.Equals(spawned[i].CivilianId, targetId, StringComparison.Ordinal))
                {
                    return spawned[i].gameObject;
                }
            }

            return null;
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
