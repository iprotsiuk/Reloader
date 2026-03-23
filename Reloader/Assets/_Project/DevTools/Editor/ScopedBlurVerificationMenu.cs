#if UNITY_EDITOR
using System.Reflection;
using Reloader.DevTools.Runtime;
using Reloader.Game.Weapons;
using Reloader.Inventory;
using Reloader.Weapons.Controllers;
using UnityEditor;
using UnityEngine;

namespace Reloader.DevTools.Editor
{
    internal static class ScopedBlurVerificationMenu
    {
        private enum SetupPhase
        {
            Idle,
            WaitingForInventoryRuntime,
            DrivingAds
        }

        private const int MaxSetupFrames = 45;
        private static int s_remainingFrames;
        private static SetupPhase s_phase;

        [MenuItem("Tools/Scoped Blur/Seed Give Test ADS")]
        private static void SeedGiveTestAds()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Scoped blur verification requires play mode.");
                return;
            }

            var weaponController = Object.FindFirstObjectByType<PlayerWeaponController>();
            var inventoryController = Object.FindFirstObjectByType<PlayerInventoryController>();
            if (weaponController == null || inventoryController == null)
            {
                Debug.LogWarning("Scoped blur verification could not find PlayerWeaponController or PlayerInventoryController.");
                return;
            }

            s_remainingFrames = MaxSetupFrames;
            s_phase = SetupPhase.WaitingForInventoryRuntime;
            EditorApplication.update -= ContinueSetup;
            EditorApplication.update += ContinueSetup;
            Debug.Log("Scoped blur verification waiting for inventory runtime before seeding starter kit.");
        }

        private static void ContinueSetup()
        {
            if (!EditorApplication.isPlaying || s_remainingFrames <= 0)
            {
                s_phase = SetupPhase.Idle;
                EditorApplication.update -= ContinueSetup;
                return;
            }

            s_remainingFrames--;

            var weaponController = Object.FindFirstObjectByType<PlayerWeaponController>();
            if (weaponController == null)
            {
                return;
            }

            var inventoryController = Object.FindFirstObjectByType<PlayerInventoryController>();
            if (inventoryController == null)
            {
                return;
            }

            if (s_phase == SetupPhase.WaitingForInventoryRuntime)
            {
                if (inventoryController.Runtime == null)
                {
                    return;
                }

                var runtime = new DevToolsRuntime();
                runtime.Context.InventoryController = inventoryController;
                runtime.Context.WeaponController = weaponController;

                if (!runtime.TryExecute("give test", out var resultMessage))
                {
                    Debug.LogError($"Scoped blur verification failed to execute give test: {resultMessage}");
                    s_phase = SetupPhase.Idle;
                    EditorApplication.update -= ContinueSetup;
                    return;
                }

                s_phase = SetupPhase.DrivingAds;
                Debug.Log($"Scoped blur verification seeded starter kit: {resultMessage}");
                return;
            }

            var adsBridge = Object.FindFirstObjectByType<AdsStateController>();
            if (adsBridge == null || s_phase != SetupPhase.DrivingAds)
            {
                return;
            }

            adsBridge.SetUseLegacyInput(false);
            adsBridge.SetAdsHeld(true);
            adsBridge.SetMagnification(25f);
            adsBridge.RefreshVisualMode();

            typeof(PlayerWeaponController)
                .GetField("_isAiming", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(weaponController, true);
            typeof(PlayerWeaponController)
                .GetMethod("UpdateStableMagnifiedScopedAdsState", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(weaponController, null);
            typeof(PlayerWeaponController)
                .GetMethod("SyncScopedViewmodelStabilization", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(weaponController, null);

            if (s_remainingFrames == 0)
            {
                s_phase = SetupPhase.Idle;
                EditorApplication.update -= ContinueSetup;
                Debug.Log($"Scoped blur verification finished ADS/max-zoom setup. AdsActive={adsBridge.IsAdsActive} AdsT={adsBridge.AdsT:F2} Magnification={adsBridge.CurrentMagnification:F2}");
            }
        }
    }
}
#endif
