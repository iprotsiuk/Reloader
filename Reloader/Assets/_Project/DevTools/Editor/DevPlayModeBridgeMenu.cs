#if UNITY_EDITOR
using System.Reflection;
using Reloader.DevTools.Runtime;
using Reloader.Game.Weapons;
using Reloader.Inventory;
using Reloader.Player;
using Reloader.Weapons.Controllers;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.DevTools.Editor
{
    internal static class DevPlayModeBridgeMenu
    {
        private enum BridgeAction
        {
            None,
            GiveTest,
            SeedHipReady,
            SeedAdsReady
        }

        private enum StepResult
        {
            Waiting,
            Completed,
            Failed
        }

        private const int MaxActionFrames = 180;
        private const float AdsReadyBlendThreshold = 0.995f;
        private const float HipReadyBlendThreshold = 0.005f;
        private const float AdsReadyMagnification = 25f;
        private const float HipReadyMagnification = 1f;

        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static BridgeAction s_activeAction;
        private static int s_remainingFrames;
        private static bool s_starterKitGranted;
        private static string s_lastStarterKitMessage = string.Empty;

        [MenuItem("Tools/DevTools/MCP Bridge/Play Mode/Give Test")]
        private static void GiveTest()
        {
            QueueAction(BridgeAction.GiveTest, "MCP bridge queued: Give Test.");
        }

        [MenuItem("Tools/DevTools/MCP Bridge/Play Mode/Seed Hip Ready")]
        private static void SeedHipReady()
        {
            QueueAction(BridgeAction.SeedHipReady, "MCP bridge queued: Seed Hip Ready.");
        }

        [MenuItem("Tools/DevTools/MCP Bridge/Play Mode/Seed ADS Ready")]
        private static void SeedAdsReady()
        {
            QueueAction(BridgeAction.SeedAdsReady, "MCP bridge queued: Seed ADS Ready.");
        }

        [MenuItem("Tools/DevTools/MCP Bridge/Play Mode/Step Look Yaw +1 (Live Input)")]
        private static void StepLookYawPlusOneLiveInput()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("MCP bridge live look step requires play mode.");
                return;
            }

            var inputReader = Object.FindFirstObjectByType<PlayerInputReader>();
            if (inputReader == null)
            {
                Debug.LogWarning("MCP bridge live look step could not find PlayerInputReader.");
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                Debug.LogWarning("MCP bridge live look step requires an active Mouse device.");
                return;
            }

            const float rawMouseDeltaX = 20f;
            var rawDelta = new Vector2(rawMouseDeltaX, 0f);
            var normalizedDelta = LookInputNormalization.NormalizeLookDelta(rawDelta, "<Pointer>/delta");
            InputSystem.QueueDeltaStateEvent(mouse.delta, rawDelta);
            Debug.Log($"MCP bridge queued live look delta via InputSystem for PlayerInputReader. raw={rawDelta} normalized={normalizedDelta}");
        }

        private static void QueueAction(BridgeAction action, string logMessage)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("MCP bridge actions require play mode.");
                return;
            }

            s_activeAction = action;
            s_remainingFrames = MaxActionFrames;
            s_starterKitGranted = false;
            s_lastStarterKitMessage = string.Empty;

            EditorApplication.update -= TickAction;
            EditorApplication.update += TickAction;
            Debug.Log(logMessage);
        }

        private static void TickAction()
        {
            if (!EditorApplication.isPlaying || s_activeAction == BridgeAction.None)
            {
                CompleteAction();
                return;
            }

            if (s_remainingFrames <= 0)
            {
                Debug.LogError($"MCP bridge action '{s_activeAction}' timed out after {MaxActionFrames} frames.");
                CompleteAction();
                return;
            }

            s_remainingFrames--;

            var weaponController = Object.FindFirstObjectByType<PlayerWeaponController>();
            var inventoryController = Object.FindFirstObjectByType<PlayerInventoryController>();
            if (weaponController == null || inventoryController == null || inventoryController.Runtime == null)
            {
                return;
            }

            if (!s_starterKitGranted)
            {
                using var runtime = new DevToolsRuntime();
                runtime.Context.InventoryController = inventoryController;
                runtime.Context.WeaponController = weaponController;
                if (!runtime.TryExecute("give test", out var resultMessage))
                {
                    Debug.LogError($"MCP bridge failed to execute give test: {resultMessage}");
                    CompleteAction();
                    return;
                }

                s_starterKitGranted = true;
                s_lastStarterKitMessage = resultMessage;
                if (s_activeAction == BridgeAction.GiveTest)
                {
                    Debug.Log($"MCP bridge give test complete: {resultMessage}");
                    CompleteAction();
                }

                return;
            }

            StepResult stepResult;
            string message;
            switch (s_activeAction)
            {
                case BridgeAction.SeedHipReady:
                    stepResult = TryApplyHipReady(weaponController, out message);
                    break;
                case BridgeAction.SeedAdsReady:
                    stepResult = TryApplyAdsReady(weaponController, out message);
                    break;
                default:
                    stepResult = StepResult.Completed;
                    message = $"MCP bridge give test complete: {s_lastStarterKitMessage}";
                    break;
            }

            if (stepResult == StepResult.Waiting)
            {
                return;
            }

            if (stepResult == StepResult.Completed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }

            CompleteAction();
        }

        private static StepResult TryApplyHipReady(PlayerWeaponController weaponController, out string message)
        {
            var adsBridge = Object.FindFirstObjectByType<AdsStateController>();
            if (adsBridge == null)
            {
                message = "MCP bridge waiting for AdsStateController before applying hip-ready seed.";
                return StepResult.Waiting;
            }

            adsBridge.SetUseLegacyInput(false);
            adsBridge.SetAdsHeld(false);
            adsBridge.SetMagnification(HipReadyMagnification);
            adsBridge.RefreshVisualMode();
            ForceWeaponAimState(weaponController, false);

            if (adsBridge.AdsT > HipReadyBlendThreshold)
            {
                message = string.Empty;
                return StepResult.Waiting;
            }

            message = $"MCP bridge hip-ready seed complete. giveTest='{s_lastStarterKitMessage}' AdsT={adsBridge.AdsT:F3} Mag={adsBridge.CurrentMagnification:F2}";
            return StepResult.Completed;
        }

        private static StepResult TryApplyAdsReady(PlayerWeaponController weaponController, out string message)
        {
            var adsBridge = Object.FindFirstObjectByType<AdsStateController>();
            if (adsBridge == null)
            {
                message = "MCP bridge waiting for AdsStateController before applying ADS-ready seed.";
                return StepResult.Waiting;
            }

            adsBridge.SetUseLegacyInput(false);
            adsBridge.SetAdsHeld(true);
            adsBridge.SetMagnification(AdsReadyMagnification);
            adsBridge.RefreshVisualMode();
            ForceWeaponAimState(weaponController, true);

            if (adsBridge.AdsT < AdsReadyBlendThreshold)
            {
                message = string.Empty;
                return StepResult.Waiting;
            }

            message = $"MCP bridge ADS-ready seed complete. giveTest='{s_lastStarterKitMessage}' AdsT={adsBridge.AdsT:F3} Mag={adsBridge.CurrentMagnification:F2}";
            return StepResult.Completed;
        }

        private static void ForceWeaponAimState(PlayerWeaponController weaponController, bool isAiming)
        {
            if (weaponController == null)
            {
                return;
            }

            typeof(PlayerWeaponController)
                .GetField("_isAiming", InstanceFlags)
                ?.SetValue(weaponController, isAiming);
            typeof(PlayerWeaponController)
                .GetMethod("UpdateStableMagnifiedScopedAdsState", InstanceFlags)
                ?.Invoke(weaponController, null);
            typeof(PlayerWeaponController)
                .GetMethod("SyncScopedViewmodelStabilization", InstanceFlags)
                ?.Invoke(weaponController, null);
        }

        private static void CompleteAction()
        {
            s_activeAction = BridgeAction.None;
            s_remainingFrames = 0;
            s_starterKitGranted = false;
            s_lastStarterKitMessage = string.Empty;
            EditorApplication.update -= TickAction;
        }
    }
}
#endif
