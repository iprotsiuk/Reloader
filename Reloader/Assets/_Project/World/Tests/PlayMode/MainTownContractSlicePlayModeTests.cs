using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Reloader.Contracts.Runtime;
using Reloader.Core.Events;
using Reloader.Core.Save;
using Reloader.Core.Save.IO;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Runtime;
using Reloader.World.Runtime;
using Reloader.World.Travel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Reloader.World.Tests.PlayMode
{
    public class MainTownContractSlicePlayModeTests
    {
        private const string BootstrapSceneName = "Bootstrap";
        private const string MainTownSceneName = "MainTown";
        private const float SceneSwitchTimeoutSeconds = 8f;

        [TearDown]
        public void TearDown()
        {
            var playerRoot = PersistentPlayerRoot.Instance?.PlayerRootTransform;
            if (playerRoot != null)
            {
                Object.DestroyImmediate(playerRoot.gameObject);
            }

            if (PersistentPlayerRoot.Instance != null)
            {
                Object.DestroyImmediate(PersistentPlayerRoot.Instance.gameObject);
            }
        }

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

            var bridge = FindPopulationBridge();
            var runtime = GetRuntime(provider);
            Assert.That(ReadActiveContract(runtime), Is.Not.Null, "Target elimination should keep the contract active until payout resolves.");
            Assert.That(targetRoot.activeSelf, Is.True, "Shared death presentation should keep the target GameObject active after elimination.");
            var retiredTarget = bridge.Runtime.Civilians.Find(record => string.Equals(record.CivilianId, activeSnapshot.TargetId, StringComparison.Ordinal));
            Assert.That(retiredTarget, Is.Not.Null, "Expected the eliminated target to remain tracked in the civilian roster.");
            Assert.That(retiredTarget!.IsAlive, Is.False, "Expected the eliminated target to retire from the live civilian roster.");
            Assert.That(retiredTarget.IsContractEligible, Is.False, "Expected the retired target to stop being contract-eligible immediately after elimination.");
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

            var bridge = FindPopulationBridge();
            var runtime = GetRuntime(provider);
            Assert.That(targetRoot.activeSelf, Is.True, "Shared death presentation should keep the target GameObject active after elimination.");
            var retiredTarget = bridge.Runtime.Civilians.Find(record => string.Equals(record.CivilianId, availableSnapshot.TargetId, StringComparison.Ordinal));
            Assert.That(retiredTarget, Is.Not.Null, "Expected the eliminated target to remain tracked in the civilian roster.");
            Assert.That(retiredTarget!.IsAlive, Is.False, "Expected the eliminated target to retire from the live civilian roster.");
            Assert.That(retiredTarget.IsContractEligible, Is.False, "Expected the retired target to stop being contract-eligible immediately after elimination.");
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
        public IEnumerator MainTownContractSlice_TargetElimination_KeepsDeathPresentationAliveForShortWindow()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True);

            var targetRoot = FindProceduralCivilianTarget(availableSnapshot.TargetId);
            Assert.That(targetRoot, Is.Not.Null, "Expected the available contract target to resolve to a spawned procedural civilian.");

            ApplyLethalDamage(targetRoot!);
            for (var frame = 0; frame < 20; frame++)
            {
                yield return null;
            }

            Assert.That(targetRoot, Is.Not.Null, "Expected the target GameObject reference to survive long enough to present ragdoll death.");
            Assert.That(targetRoot.activeSelf, Is.True, "Expected shared death presentation to keep the target GameObject active for a short post-kill window.");
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_AcceptedProceduralTarget_KeepsUnitPresentationScale()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTown contract runtime root.");
            Assert.That(provider!.TryGetContractSnapshot(out var availableSnapshot), Is.True);

            var targetRoot = FindProceduralCivilianTarget(availableSnapshot.TargetId);
            Assert.That(targetRoot, Is.Not.Null, "Expected the available contract target to resolve to a spawned procedural civilian.");

            Assert.That(provider.AcceptAvailableContract(), Is.True, "Expected the live procedural contract to be acceptible.");
            for (var frame = 0; frame < 10; frame++)
            {
                yield return null;
            }

            var visualRoot = targetRoot!.transform.Find("VisualRoot");
            Assert.That(visualRoot, Is.Not.Null, "Expected VisualRoot on the active MainTown contract target.");
            AssertUniformScale(targetRoot.transform.lossyScale, targetRoot.name, "contract target root");
            AssertUniformScale(visualRoot!.lossyScale, targetRoot.name, "contract target VisualRoot");

            var activeStyleRoot = visualRoot.Find("StyleMaleRoot");
            if (activeStyleRoot == null || !activeStyleRoot.gameObject.activeInHierarchy)
            {
                activeStyleRoot = visualRoot.Find("StyleFemaleRoot");
            }

            Assert.That(activeStyleRoot, Is.Not.Null, "Expected an active style root on the live MainTown contract target.");
            AssertUniformScale(activeStyleRoot!.lossyScale, targetRoot.name, activeStyleRoot.name);
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
            Assert.That(availableSnapshot.TargetDisplayName, Is.Not.Empty);
            Assert.That(availableSnapshot.TargetDisplayName, Is.Not.EqualTo(availableSnapshot.TargetId));
            Assert.That(availableSnapshot.TargetDescription, Is.Not.Empty);

            var offeredTarget = FindProceduralCivilianTarget(availableSnapshot.TargetId);
            Assert.That(offeredTarget, Is.Not.Null, "Expected the scene's available contract to target a spawned procedural civilian.");

            var targetDamageableType = Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons");
            Assert.That(targetDamageableType, Is.Not.Null, "Expected contract target damageable type to resolve.");
            var targetDamageable = offeredTarget!.GetComponent(targetDamageableType!);
            Assert.That(targetDamageable, Is.Not.Null, "Expected procedural offer target to expose the existing contract-target damageable seam.");
            Assert.That(GetProperty<string>(targetDamageable!, "TargetId"), Is.EqualTo(availableSnapshot.TargetId));
            Assert.That(GetProperty<string>(targetDamageable!, "DisplayName"), Is.EqualTo(availableSnapshot.TargetDisplayName));
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_WhenIdle_NonOfferedProceduralCivilianDoesNotExposeContractTargetDamageable()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True);
            Assert.That(availableSnapshot.TargetId, Does.StartWith("citizen.mainTown."));

            var bridge = FindPopulationBridge();
            var nonOfferedCivilian = bridge.Runtime.Civilians
                .Find(record => record != null
                    && record.IsAlive
                    && record.IsContractEligible
                    && !record.IsProtectedFromContracts
                    && !string.Equals(record.CivilianId, availableSnapshot.TargetId, StringComparison.Ordinal));
            Assert.That(nonOfferedCivilian, Is.Not.Null, "Expected MainTown starter population to include another live eligible civilian besides the offered target.");

            Assert.That(bridge.TryResolveSpawnedCivilian(nonOfferedCivilian!.CivilianId, out var spawnedCivilian), Is.True);
            Assert.That(spawnedCivilian, Is.Not.Null, "Expected the non-offered civilian to be present in the scene.");

            var damageableType = Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons");
            Assert.That(damageableType, Is.Not.Null, "Expected contract target damageable type to resolve.");
            Assert.That(spawnedCivilian!.GetComponent(damageableType!), Is.Null,
                "Expected idle non-offered civilians to stay outside the contract-target seam.");
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_WhenAccepted_OnlyActiveProceduralTargetExposesContractTargetDamageable()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True);
            Assert.That(availableSnapshot.TargetId, Does.StartWith("citizen.mainTown."));

            Assert.That(provider.AcceptAvailableContract(), Is.True, "Expected the scene contract provider to accept the live procedural offer.");
            Assert.That(provider.TryGetContractSnapshot(out var activeSnapshot), Is.True);
            Assert.That(activeSnapshot.HasActiveContract, Is.True);

            var bridge = FindPopulationBridge();
            var damageableType = Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons");
            Assert.That(damageableType, Is.Not.Null, "Expected contract target damageable type to resolve.");

            var spawned = bridge.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            var activeSpawn = Array.Find(spawned, candidate => candidate != null && string.Equals(candidate.CivilianId, activeSnapshot.TargetId, StringComparison.Ordinal));
            Assert.That(activeSpawn, Is.Not.Null, "Expected the accepted procedural target to remain spawned in-scene.");

            var damageableHolders = Array.FindAll(spawned, candidate => candidate != null && candidate.GetComponent(damageableType!) != null);
            Assert.That(damageableHolders.Length, Is.EqualTo(1), "Expected only one spawned procedural civilian to expose ContractTargetDamageable after accepting the contract.");
            Assert.That(damageableHolders[0].CivilianId, Is.EqualTo(activeSnapshot.TargetId), "Expected the active contract target to be the only spawned civilian on the contract-target damage seam.");

            var nonTargetSpawn = Array.Find(spawned, candidate => candidate != null
                && !string.Equals(candidate.CivilianId, activeSnapshot.TargetId, StringComparison.Ordinal)
                && bridge.Runtime.Civilians.Exists(record => record != null
                    && record.IsAlive
                    && record.IsContractEligible
                    && !record.IsProtectedFromContracts
                    && string.Equals(record.CivilianId, candidate.CivilianId, StringComparison.Ordinal)));
            Assert.That(nonTargetSpawn, Is.Not.Null, "Expected another live eligible procedural civilian besides the active target.");
            Assert.That(nonTargetSpawn!.GetComponent(damageableType!), Is.Null, "Expected non-target civilians to stay outside the contract-target damage seam after acceptance.");
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_WhenWrongTargetIsEliminated_ProceduralContractRemainsActive()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True);
            Assert.That(provider.AcceptAvailableContract(), Is.True);
            Assert.That(provider.TryGetContractSnapshot(out var activeSnapshot), Is.True);

            var wrongTargetRoot = FindDifferentProceduralCivilianTarget(activeSnapshot.TargetId);
            Assert.That(wrongTargetRoot, Is.Not.Null, "Expected another spawned civilian besides the active contract target.");

            provider.ReportContractTargetEliminated("citizen.mainTown.wrong-target", wasExposed: true);
            yield return null;

            var runtime = GetRuntime(provider);
            Assert.That(ReadActiveContract(runtime), Is.Not.Null, "Procedural contracts should stay active after killing a non-target NPC.");
            Assert.That(runtime.CurrentHeatState.Level, Is.EqualTo(PoliceHeatLevel.Search));
            Assert.That(provider.TryGetContractSnapshot(out var stillActiveSnapshot), Is.True,
                "Expected the procedural contract to remain visible after killing a different NPC.");
            Assert.That(stillActiveSnapshot.HasActiveContract, Is.True);
            Assert.That(stillActiveSnapshot.HasFailedContract, Is.False);
            Assert.That(stillActiveSnapshot.TargetId, Is.EqualTo(activeSnapshot.TargetId),
                "Expected the contract to keep tracking the original target after a wrong-target kill.");
            Assert.That(stillActiveSnapshot.StatusText, Is.EqualTo("Active contract"));
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_TargetEliminatedBeforeAccept_RebuildPublishesDifferentLiveTarget()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True);

            var bridge = FindPopulationBridge();
            var consumedTargetId = availableSnapshot.TargetId;
            var targetRoot = FindProceduralCivilianTarget(consumedTargetId);
            Assert.That(targetRoot, Is.Not.Null, "Expected the available contract target to resolve to a spawned procedural civilian.");

            ApplyLethalDamage(targetRoot!);
            yield return null;

            provider.AdvanceRuntime(31f);
            yield return null;

            Assert.That(provider.TryGetContractSnapshot(out _), Is.False, "Expected the consumed pre-accept offer to stay unavailable until the bridge rebuilds.");

            bridge.RebuildScenePopulation();
            yield return null;

            Assert.That(bridge.TryResolveSpawnedCivilian(consumedTargetId, out _), Is.False, "Expected the killed procedural civilian to be retired from the live population before the next rebuild.");
            Assert.That(provider.TryGetContractSnapshot(out var refreshedSnapshot), Is.True, "Expected a later rebuild to publish a replacement live offer from the remaining population.");
            Assert.That(refreshedSnapshot.TargetId, Does.StartWith("citizen.mainTown."));
            Assert.That(refreshedSnapshot.TargetId, Is.Not.EqualTo(consumedTargetId), "Expected the rebuilt offer to move away from the consumed dead target.");
            Assert.That(refreshedSnapshot.TargetDisplayName, Is.Not.Empty);
            Assert.That(refreshedSnapshot.TargetDisplayName, Is.Not.EqualTo(availableSnapshot.TargetDisplayName));

            var refreshedTarget = FindProceduralCivilianTarget(refreshedSnapshot.TargetId);
            Assert.That(refreshedTarget, Is.Not.Null, "Expected the rebuilt contract offer to resolve to a different live procedural civilian.");
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_SaveLoad_PreservesAcceptedProceduralContractTarget()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True);
            Assert.That(availableSnapshot.TargetId, Does.StartWith("citizen.mainTown."));

            var coordinator = SaveBootstrapper.CreateDefaultCoordinator();
            var repository = new SaveFileRepository();
            var tempDir = Path.Combine(Path.GetTempPath(), "reloader-maintown-contract-tests-" + Guid.NewGuid().ToString("N"));
            var savePath = Path.Combine(tempDir, "maintown-contract-load.json");
            Directory.CreateDirectory(tempDir);

            try
            {
                Assert.That(provider.AcceptAvailableContract(), Is.True);
                Assert.That(provider.TryGetContractSnapshot(out var activeSnapshot), Is.True);
                Assert.That(activeSnapshot.HasActiveContract, Is.True);

                var targetBeforeSave = FindProceduralCivilianTarget(activeSnapshot.TargetId);
                Assert.That(targetBeforeSave, Is.Not.Null, "Expected accepted procedural contract target to exist before save.");

                var envelope = coordinator.CaptureEnvelope("0.6.0-dev");
                repository.WriteEnvelope(savePath, envelope);

                coordinator.Load(savePath);
                yield return null;

                Assert.That(provider.TryGetContractSnapshot(out var restoredSnapshot), Is.True);
                Assert.That(restoredSnapshot.HasActiveContract, Is.True, "Expected accepted procedural contract to survive save/load.");
                Assert.That(restoredSnapshot.TargetId, Is.EqualTo(activeSnapshot.TargetId));

                var targetAfterLoad = FindProceduralCivilianTarget(restoredSnapshot.TargetId);
                Assert.That(targetAfterLoad, Is.Not.Null, "Expected restored active contract target to still resolve to a live procedural civilian.");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_AcceptedProceduralContract_ResolvesLiveTargetThroughPopulationBridge()
        {
            yield return LoadScene(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True);
            Assert.That(availableSnapshot.TargetId, Does.StartWith("citizen.mainTown."));

            Assert.That(provider.AcceptAvailableContract(), Is.True);
            Assert.That(provider.TryGetContractSnapshot(out var activeSnapshot), Is.True);
            Assert.That(activeSnapshot.HasActiveContract, Is.True);

            var bridge = FindPopulationBridge();
            var resolveMethod = typeof(CivilianPopulationRuntimeBridge).GetMethod(
                "TryResolveSpawnedCivilian",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.That(resolveMethod, Is.Not.Null, "Expected a public TryResolveSpawnedCivilian(string civilianId, out MainTownPopulationSpawnedCivilian civilian) method on CivilianPopulationRuntimeBridge.");

            var args = new object[] { activeSnapshot.TargetId, null };
            var resolved = (bool)resolveMethod!.Invoke(bridge, args)!;
            Assert.That(resolved, Is.True, "Expected the accepted procedural contract target to resolve through the population bridge.");

            var resolvedCivilian = args[1] as MainTownPopulationSpawnedCivilian;
            Assert.That(resolvedCivilian, Is.Not.Null, "Expected the population bridge to return the spawned civilian metadata component.");
            Assert.That(resolvedCivilian!.CivilianId, Is.EqualTo(activeSnapshot.TargetId));
            Assert.That(resolvedCivilian.PopulationSlotId, Is.Not.Empty);

            var damageableType = Type.GetType("Reloader.Weapons.World.ContractTargetDamageable, Reloader.Weapons");
            Assert.That(damageableType, Is.Not.Null, "Expected contract target damageable type to resolve.");
            var damageable = resolvedCivilian.GetComponent(damageableType!);
            Assert.That(damageable, Is.Not.Null, "Expected the resolved procedural target to expose the contract-target seam.");
            Assert.That(GetProperty<string>(damageable!, "TargetId"), Is.EqualTo(activeSnapshot.TargetId));
        }

        [UnityTest]
        public IEnumerator MainTownContractSlice_DemoScene_StagesLiveTargetNearMultipleAuthoredLanesAndReferenceSetup()
        {
            yield return LoadSceneWithoutPlayerStabilization(MainTownSceneName);
            yield return null;

            var providerRoot = GameObject.Find("MainTownContractRuntime");
            Assert.That(providerRoot, Is.Not.Null, "Expected authored MainTown contract runtime root.");

            var provider = providerRoot!.GetComponent<StaticContractRuntimeProvider>();
            Assert.That(provider, Is.Not.Null, "Expected StaticContractRuntimeProvider on MainTownContractRuntime.");
            Assert.That(provider.TryGetContractSnapshot(out var availableSnapshot), Is.True, "Expected authored MainTown contract offer.");

            var targetRoot = FindProceduralCivilianTarget(availableSnapshot.TargetId);
            Assert.That(targetRoot, Is.Not.Null, "Expected the available contract target to resolve to a spawned procedural civilian.");

            var returnEntry = FindEntryPoint("entry.maintown.return");
            var smokeTrigger = GameObject.Find("MainTown_SmokeToIndoor_Trigger");
            Assert.That(returnEntry, Is.Not.Null, "Expected MainTown return entry point for the demo reference setup.");
            Assert.That(smokeTrigger, Is.Not.Null, "Expected authored smoke trigger in MainTown.");

            var lanes = FindTargetLanes();
            Assert.That(lanes.Length, Is.EqualTo(3), "Expected MainTown demo slice to keep three authored firing lanes instead of one exact forced position.");

            var sameFacingLanePairs = 0;
            var closeLaneCount = 0;
            for (var i = 0; i < lanes.Length; i++)
            {
                var lane = lanes[i];
                Assert.That(lane, Is.Not.Null, "Expected every authored demo lane root to resolve.");

                var metrics = GetDedicatedRangeMetrics(lane!);
                Assert.That(GetProperty<string>(metrics, "TargetId"), Is.Not.Empty, "Expected each demo lane to expose a stable authored target id.");
                Assert.That(GetProperty<string>(metrics, "DisplayName"), Is.Not.Empty, "Expected each demo lane to expose a stable authored display name.");
                Assert.That(GetProperty<float>(metrics, "DistanceMeters"), Is.InRange(10f, 60f), "Expected each demo lane to publish a deterministic short-range smoke distance.");

                if (Vector3.Distance(lane.transform.position, targetRoot!.transform.position) <= 65f)
                {
                    closeLaneCount++;
                }

                for (var j = i + 1; j < lanes.Length; j++)
                {
                    Assert.That(Vector3.Distance(lane.transform.position, lanes[j]!.transform.position), Is.GreaterThan(1.5f),
                        "Expected authored firing lanes to occupy distinct sightlines.");

                    if (Vector3.Angle(lane.transform.forward, lanes[j]!.transform.forward) <= 1f)
                    {
                        sameFacingLanePairs++;
                    }
                }
            }

            Assert.That(closeLaneCount, Is.GreaterThanOrEqualTo(2), "Expected the live demo target to sit inside a stable exposure cluster with multiple plausible firing lanes.");
            Assert.That(sameFacingLanePairs, Is.EqualTo(3), "Expected all authored demo lanes to face the same exposure corridor.");
            Assert.That(Vector3.Distance(smokeTrigger!.transform.position, returnEntry!.transform.position), Is.LessThan(25f),
                "Expected the smoke travel trigger to stay inside the same deterministic demo reference setup as the return entry.");
            Assert.That(Vector3.Distance(targetRoot!.transform.position, returnEntry.transform.position), Is.LessThan(70f),
                "Expected the live contract target to stay near the arrival/reference cluster instead of a distant sandbox pocket.");

            var referenceMetrics = GetDedicatedRangeMetrics(GameObject.Find("TargetLane_2"));
            Assert.That(GetProperty<string>(referenceMetrics, "TargetId"), Does.Contain("reference"),
                "Expected one known-good reference lane for deterministic smoke coverage without forcing one exact firing spot.");
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

        private static CivilianPopulationRuntimeBridge FindPopulationBridge()
        {
            var populationRoot = GameObject.Find("MainTownPopulationRuntime");
            Assert.That(populationRoot, Is.Not.Null, "Expected authored MainTown population runtime root.");

            var bridge = populationRoot!.GetComponent<CivilianPopulationRuntimeBridge>();
            Assert.That(bridge, Is.Not.Null, "Expected CivilianPopulationRuntimeBridge on MainTownPopulationRuntime.");
            return bridge!;
        }

        private static GameObject FindProceduralCivilianTarget(string targetId)
        {
            var bridge = FindPopulationBridge();
            var spawned = bridge.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            for (var i = 0; i < spawned.Length; i++)
            {
                if (string.Equals(spawned[i].CivilianId, targetId, StringComparison.Ordinal))
                {
                    return spawned[i].gameObject;
                }
            }

            return null;
        }

        private static GameObject FindDifferentProceduralCivilianTarget(string excludedCivilianId)
        {
            var bridge = FindPopulationBridge();
            var spawned = bridge.GetComponentsInChildren<MainTownPopulationSpawnedCivilian>(includeInactive: true);
            for (var i = 0; i < spawned.Length; i++)
            {
                var candidate = spawned[i];
                if (candidate == null
                    || !candidate.gameObject.activeInHierarchy
                    || string.Equals(candidate.CivilianId, excludedCivilianId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!bridge.Runtime.Civilians.Exists(record => record != null
                    && record.IsAlive
                    && string.Equals(record.CivilianId, candidate.CivilianId, StringComparison.Ordinal)))
                {
                    continue;
                }

                return candidate.gameObject;
            }

            return null;
        }

        private static SceneEntryPoint FindEntryPoint(string entryPointId)
        {
            var candidates = Object.FindObjectsByType<SceneEntryPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate != null && string.Equals(candidate.EntryPointId, entryPointId, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static GameObject[] FindTargetLanes()
        {
            return new[]
            {
                GameObject.Find("TargetLane_1"),
                GameObject.Find("TargetLane_2"),
                GameObject.Find("TargetLane_3")
            };
        }

        private static object GetDedicatedRangeMetrics(GameObject laneRoot)
        {
            var metricsType = Type.GetType("Reloader.Weapons.World.DummyTargetRangeMetrics, Reloader.Weapons");
            Assert.That(metricsType, Is.Not.Null, "Expected DummyTargetRangeMetrics type to resolve.");

            var metrics = laneRoot.GetComponent(metricsType!);
            Assert.That(metrics, Is.Not.Null, $"Expected DummyTargetRangeMetrics on '{laneRoot.name}'.");
            return metrics!;
        }

        private static IEnumerator LoadScene(string sceneName)
        {
            Assert.That(sceneName, Is.EqualTo(MainTownSceneName), "MainTown contract tests should bootstrap through the runtime travel path.");

            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
                yield return null;

                var bootstrapRoot = Object.FindFirstObjectByType<BootstrapWorldRoot>(FindObjectsInactive.Include);
                Assert.That(bootstrapRoot, Is.Not.Null, "Expected Bootstrap to keep the canonical BootstrapWorldRoot loaded.");

                var persistentRoot = BootstrapWorldRoot.Initialize();
                Assert.That(persistentRoot, Is.Not.Null, "Expected Bootstrap runtime initialization to produce a canonical PersistentPlayerRoot.");
                Assert.That(PersistentPlayerRoot.Instance, Is.SameAs(persistentRoot), "Expected one canonical PersistentPlayerRoot instance.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.Not.Null, "Expected a canonical runtime player root before routing into MainTown.");

                yield return null;

                var started = WorldTravelCoordinator.TryLoadSceneAtEntry(MainTownSceneName, "entry.maintown.spawn");
                Assert.That(started, Is.True, "Expected Bootstrap to route into MainTown via the authored entry point.");

                yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
                yield return WaitForResolvedEntryPoint("entry.maintown.spawn", SceneSwitchTimeoutSeconds);
                yield return WaitForCanonicalPlayerStartupStabilization();
                yield return null;
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
        }

        private static IEnumerator LoadSceneWithoutPlayerStabilization(string sceneName)
        {
            Assert.That(sceneName, Is.EqualTo(MainTownSceneName), "MainTown contract tests should bootstrap through the runtime travel path.");

            var previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Single);
                yield return null;

                var bootstrapRoot = Object.FindFirstObjectByType<BootstrapWorldRoot>(FindObjectsInactive.Include);
                Assert.That(bootstrapRoot, Is.Not.Null, "Expected Bootstrap to keep the canonical BootstrapWorldRoot loaded.");

                var persistentRoot = BootstrapWorldRoot.Initialize();
                Assert.That(persistentRoot, Is.Not.Null, "Expected Bootstrap runtime initialization to produce a canonical PersistentPlayerRoot.");
                Assert.That(PersistentPlayerRoot.Instance, Is.SameAs(persistentRoot), "Expected one canonical PersistentPlayerRoot instance.");
                Assert.That(persistentRoot.PlayerRootTransform, Is.Not.Null, "Expected a canonical runtime player root before routing into MainTown.");

                yield return null;

                var started = WorldTravelCoordinator.TryLoadSceneAtEntry(MainTownSceneName, "entry.maintown.spawn");
                Assert.That(started, Is.True, "Expected Bootstrap to route into MainTown via the authored entry point.");

                yield return WaitForActiveScene(MainTownSceneName, SceneSwitchTimeoutSeconds);
                yield return WaitForResolvedEntryPoint("entry.maintown.spawn", SceneSwitchTimeoutSeconds);
                yield return null;
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
        }

        private static IEnumerator WaitForActiveScene(string expectedSceneName, float timeoutSeconds)
        {
            var elapsed = 0f;
            while (SceneManager.GetActiveScene().name != expectedSceneName && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(expectedSceneName),
                $"Timed out waiting for scene '{expectedSceneName}'.");
        }

        private static IEnumerator WaitForResolvedEntryPoint(string expectedEntryPointId, float timeoutSeconds)
        {
            var elapsed = 0f;
            while (WorldTravelCoordinator.LastResolvedEntryPointId != expectedEntryPointId && elapsed < timeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(
                WorldTravelCoordinator.LastResolvedEntryPointId,
                Is.EqualTo(expectedEntryPointId),
                $"Timed out waiting for resolved entry point '{expectedEntryPointId}'.");
        }

        private static IEnumerator WaitForCanonicalPlayerStartupStabilization()
        {
            var playerRoot = PersistentPlayerRoot.Instance?.PlayerRootTransform;
            Assert.That(playerRoot, Is.Not.Null, "Expected canonical runtime player root before startup stabilization.");

            var stableFrameCount = 0;
            var startPosition = playerRoot.position;
            var previousPosition = startPosition;
            var currentPosition = startPosition;
            var elapsed = 0f;
            while (elapsed < 2f && stableFrameCount < 3)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;

                currentPosition = playerRoot.position;
                if (Vector3.Distance(currentPosition, previousPosition) <= 0.001f)
                {
                    stableFrameCount++;
                }
                else
                {
                    stableFrameCount = 0;
                }

                previousPosition = currentPosition;
            }

            Assert.That(stableFrameCount, Is.GreaterThanOrEqualTo(3),
                $"Expected the canonical runtime player to settle before asserting travel state. Start={startPosition}, End={currentPosition}, Elapsed={elapsed:0.000}s.");
        }

        private static void AssertUniformScale(Vector3 lossyScale, string subjectName, string nodeName)
        {
            Assert.That(lossyScale.x, Is.EqualTo(lossyScale.y).Within(0.01f), $"Expected '{subjectName}' {nodeName} scale to stay uniform. Scale={lossyScale}.");
            Assert.That(lossyScale.x, Is.EqualTo(lossyScale.z).Within(0.01f), $"Expected '{subjectName}' {nodeName} scale to stay uniform. Scale={lossyScale}.");
            Assert.That(lossyScale.x, Is.EqualTo(1f).Within(0.01f), $"Expected '{subjectName}' {nodeName} scale to stay near unit scale. Scale={lossyScale}.");
        }
    }
}
