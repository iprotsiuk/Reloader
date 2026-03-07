using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
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
        public IEnumerator MainTownContractSlice_AcceptsTargetEliminationAndAwardsPayoutAfterSearchClears()
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
            Assert.That(ReadActiveContract(runtime), Is.Null, "Contract should complete once the search timer clears.");
            Assert.That(provider.TryGetContractSnapshot(out _), Is.False, "Authored offer should be consumed after the scene contract completes.");
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
