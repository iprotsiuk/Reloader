using System;
using System.Collections.Generic;
using Reloader.Player;
using Reloader.Player.Interaction;
using Reloader.Core.Events;
using Reloader.Core.Runtime;
using Reloader.Reloading.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.Reloading.World
{
    public sealed class PlayerReloadingBenchController : MonoBehaviour, IPlayerInteractionCandidateProvider, IPlayerInteractionCoordinatorModeAware
    {
        private const string BenchHintContextId = "bench";
        private const string BenchHintActionText = "Use bench";
        private const string MissingItemStatus = "missing";
        private const string InstalledItemStatus = "installed";

        private static readonly OperationDescriptor[] UiOperations =
        {
            new OperationDescriptor("Resize", ReloadingOperationType.ResizeCase),
            new OperationDescriptor("Prime", ReloadingOperationType.PrimeCase),
            new OperationDescriptor("Seat", ReloadingOperationType.SeatBullet)
        };

        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _resolverBehaviour;
        [Header("Interaction")]
        [SerializeField] private bool _interactionCoordinatorModeEnabled;
        [SerializeField] private int _interactionPriority = 50;

        private IPlayerInputSource _inputSource;
        private IPlayerReloadingBenchResolver _resolver;
        private IUiStateEvents _uiStateEvents;
        private IReloadingBenchTarget _activeTarget;
        private bool _useRuntimeKernelUiStateEvents = true;
        private bool _flushPickupInputAtEndOfFrame;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Update()
        {
            Tick();
        }

        private void LateUpdate()
        {
            if (_interactionCoordinatorModeEnabled)
            {
                return;
            }

            if (!_flushPickupInputAtEndOfFrame || _inputSource == null)
            {
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            _inputSource.ConsumePickupPressed();
        }

        private void OnDisable()
        {
            CloseActiveWorkbenchIfAny();
            _flushPickupInputAtEndOfFrame = false;
            if (!_interactionCoordinatorModeEnabled)
            {
                ClearInteractionHint();
            }
        }

        public void Configure(IPlayerInputSource inputSource, IPlayerReloadingBenchResolver resolver, IUiStateEvents uiStateEvents = null)
        {
            _inputSource = inputSource;
            _resolver = resolver;
            _useRuntimeKernelUiStateEvents = uiStateEvents == null;
            _uiStateEvents = uiStateEvents;
        }

        public void Tick()
        {
            ResolveReferences();

            if (_resolver == null || !_resolver.TryResolveBenchTarget(out var target) || target == null)
            {
                _flushPickupInputAtEndOfFrame = !_interactionCoordinatorModeEnabled;
                CloseActiveWorkbenchIfAny();
                if (!_interactionCoordinatorModeEnabled)
                {
                    ClearInteractionHint();
                }

                return;
            }

            var uiStateEvents = ResolveUiStateEvents();
            var isAnyMenuOpen = (uiStateEvents != null && uiStateEvents.IsAnyMenuOpen) || IsStorageUiOpen();
            if (!_interactionCoordinatorModeEnabled && isAnyMenuOpen)
            {
                ClearInteractionHint();
            }
            else if (!_interactionCoordinatorModeEnabled)
            {
                PublishInteractionHint();
            }

            if (_activeTarget != null && !ReferenceEquals(_activeTarget, target) && _activeTarget.IsWorkbenchOpen)
            {
                _activeTarget.CloseWorkbench();
                RaiseWorkbenchMenuVisibilityChanged(false);
            }

            var targetChanged = !ReferenceEquals(_activeTarget, target);
            _activeTarget = target;
            if (targetChanged)
            {
                PublishWorkbenchSnapshot(target);
            }

            if (target.IsWorkbenchOpen
                && ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    || (_inputSource != null && _inputSource.ConsumeMenuTogglePressed())))
            {
                target.CloseWorkbench();
                RaiseWorkbenchMenuVisibilityChanged(false);
                ReloadingWorkbenchUiContextStore.Clear();
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            if (_interactionCoordinatorModeEnabled)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            if (isAnyMenuOpen)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            var pickupPressedThisFrame = IsPickupPressedThisFrame();
            if (!pickupPressedThisFrame)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            target.OpenWorkbench();
            RaiseWorkbenchMenuVisibilityChanged(true);
            PublishWorkbenchSnapshot(target);
        }

        public void SetInteractionCoordinatorMode(bool isEnabled)
        {
            _interactionCoordinatorModeEnabled = isEnabled;
            if (isEnabled)
            {
                _flushPickupInputAtEndOfFrame = false;
                ClearInteractionHint();
            }
        }

        public bool TryGetInteractionCandidate(out PlayerInteractionCandidate candidate)
        {
            candidate = default;
            ResolveReferences();

            var uiStateEvents = ResolveUiStateEvents();
            if (uiStateEvents != null && uiStateEvents.IsAnyMenuOpen)
            {
                return false;
            }

            if (_resolver == null || !_resolver.TryResolveBenchTarget(out var target) || target == null)
            {
                return false;
            }

            var subjectText = target is MonoBehaviour benchBehaviour ? benchBehaviour.name : "Reloading Bench";
            var stableTieBreaker = target is UnityEngine.Object benchObject ? benchObject.GetInstanceID().ToString() : "bench";
            candidate = new PlayerInteractionCandidate(
                BenchHintContextId,
                BenchHintActionText,
                subjectText,
                _interactionPriority,
                stableTieBreaker,
                PlayerInteractionActionKind.Workbench,
                () =>
                {
                    target.OpenWorkbench();
                    RaiseWorkbenchMenuVisibilityChanged(true);
                    PublishWorkbenchSnapshot(target);
                });
            return true;
        }

        private void CloseActiveWorkbenchIfAny()
        {
            if (_activeTarget != null && _activeTarget.IsWorkbenchOpen)
            {
                _activeTarget.CloseWorkbench();
                RaiseWorkbenchMenuVisibilityChanged(false);
            }

            _activeTarget = null;
            ReloadingWorkbenchUiContextStore.Clear();
        }

        private bool IsPickupPressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                _inputSource?.ConsumePickupPressed();
                return true;
            }

            return _inputSource != null && _inputSource.ConsumePickupPressed();
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            _resolver ??= _resolverBehaviour as IPlayerReloadingBenchResolver;

            if (_inputSource == null)
            {
                _inputSource = GetInterfaceFromBehaviours<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_resolver == null)
            {
                _resolver = GetInterfaceFromBehaviours<IPlayerReloadingBenchResolver>(GetComponents<MonoBehaviour>());
            }

            ResolveUiStateEvents();
        }

        private static TInterface GetInterfaceFromBehaviours<TInterface>(MonoBehaviour[] behaviours) where TInterface : class
        {
            if (behaviours == null)
            {
                return null;
            }

            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is TInterface typed)
                {
                    return typed;
                }
            }

            return null;
        }

        private void RaiseWorkbenchMenuVisibilityChanged(bool isVisible)
        {
            ResolveUiStateEvents()?.RaiseWorkbenchMenuVisibilityChanged(isVisible);
        }

        private IUiStateEvents ResolveUiStateEvents()
        {
            if (_useRuntimeKernelUiStateEvents)
            {
                _uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            }

            return _uiStateEvents;
        }

        private static void PublishInteractionHint()
        {
            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintShown(
                new InteractionHintPayload(BenchHintContextId, BenchHintActionText));
        }

        private static void ClearInteractionHint()
        {
            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintCleared(BenchHintContextId);
        }

        private static bool IsStorageUiOpen()
        {
            var type = Type.GetType("Reloader.Inventory.StorageUiSession, Reloader.Inventory");
            if (type == null)
            {
                return false;
            }

            var prop = type.GetProperty("IsOpen");
            return prop != null
                   && prop.PropertyType == typeof(bool)
                   && prop.GetValue(null) is bool isOpen
                   && isOpen;
        }

        private static void PublishWorkbenchSnapshot(IReloadingBenchTarget target)
        {
            if (target is not ReloadingBenchTarget benchTarget)
            {
                ReloadingWorkbenchUiContextStore.Clear();
                return;
            }

            var loadout = benchTarget.LoadoutController;
            var runtimeState = benchTarget.RuntimeState;
            if (loadout == null || runtimeState == null)
            {
                ReloadingWorkbenchUiContextStore.Clear();
                return;
            }

            var setupSlots = BuildSetupSlotSnapshot(runtimeState);
            var operationSnapshot = BuildOperationSnapshot(loadout);
            ReloadingWorkbenchUiContextStore.Publish(new ReloadingWorkbenchUiSnapshot(setupSlots, operationSnapshot));
        }

        private static string[] BuildSetupSlotSnapshot(WorkbenchRuntimeState runtimeState)
        {
            var slotsById = runtimeState.SlotsById;
            if (slotsById == null || slotsById.Count == 0)
            {
                return Array.Empty<string>();
            }

            var slotIds = new List<string>(slotsById.Count);
            foreach (var kvp in slotsById)
            {
                slotIds.Add(kvp.Key);
            }

            slotIds.Sort(StringComparer.Ordinal);
            var lines = new string[slotIds.Count];
            for (var i = 0; i < slotIds.Count; i++)
            {
                var slotId = slotIds[i];
                slotsById.TryGetValue(slotId, out var slotState);
                var status = slotState != null && slotState.IsOccupied ? InstalledItemStatus : MissingItemStatus;
                lines[i] = $"{slotId}: {status}";
            }

            return lines;
        }

        private static ReloadingWorkbenchUiSnapshot.OperationGateSnapshot[] BuildOperationSnapshot(WorkbenchLoadoutController loadoutController)
        {
            var gate = new ReloadingOperationGate(loadoutController);
            var snapshots = new ReloadingWorkbenchUiSnapshot.OperationGateSnapshot[UiOperations.Length];
            for (var i = 0; i < UiOperations.Length; i++)
            {
                var operation = UiOperations[i];
                gate.IsOperationAllowed(operation.Operation, out var gateStatus);
                snapshots[i] = new ReloadingWorkbenchUiSnapshot.OperationGateSnapshot(
                    operation.Label,
                    gateStatus.IsAllowed,
                    BuildGateDiagnostic(gateStatus));
            }

            return snapshots;
        }

        private static string BuildGateDiagnostic(ReloadingOperationGate.OperationGateStatus gateStatus)
        {
            if (gateStatus.IsAllowed)
            {
                return string.Empty;
            }

            if (gateStatus.MissingCapabilities == null || gateStatus.MissingCapabilities.Count == 0)
            {
                return "Operation blocked.";
            }

            return $"Operation blocked: missing {string.Join(", ", gateStatus.MissingCapabilities)}.";
        }

        private readonly struct OperationDescriptor
        {
            public OperationDescriptor(string label, ReloadingOperationType operation)
            {
                Label = label;
                Operation = operation;
            }

            public string Label { get; }

            public ReloadingOperationType Operation { get; }
        }
    }
}
