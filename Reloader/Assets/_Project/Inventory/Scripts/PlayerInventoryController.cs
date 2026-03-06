using System;
using System.Collections.Generic;
using System.Text;
using Reloader.Core;
using Reloader.Core.Events;
using Reloader.Core.Items;
using Reloader.Core.Persistence;
using Reloader.Core.Runtime;
using Reloader.Player;
using Reloader.Player.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloader.Inventory
{
    public sealed class PlayerInventoryController : MonoBehaviour, IPlayerInteractionCandidateProvider, IPlayerInteractionCoordinatorModeAware
    {
        private const int DefaultBackpackCapacity = 9;
        private const string PickupHintContextId = "pickup";
        private const string PickupHintActionText = "Pick up";

        [SerializeField] private MonoBehaviour _inputSourceBehaviour;
        [SerializeField] private MonoBehaviour _pickupTargetResolverBehaviour;
        [SerializeField] private List<ItemDefinition> _itemDefinitionRegistry = new List<ItemDefinition>();
        [SerializeField] private int _startingBackpackCapacity = DefaultBackpackCapacity;
        [Header("Drop")]
        [SerializeField] private float _dropForwardDistance = 1.15f;
        [SerializeField] private float _dropVerticalOffset = 0.8f;
        [SerializeField] private float _dropImpulse = 2.1f;
        [SerializeField] private float _dropSpinImpulse = 0.45f;
        [SerializeField] private Vector3 _dropColliderSize = new Vector3(0.24f, 0.24f, 0.24f);
        [Header("Interaction")]
        [SerializeField] private bool _interactionCoordinatorModeEnabled;
        [SerializeField] private int _interactionPriority = 10;
        [Header("Debug (Runtime)")]
        [SerializeField] private int _selectedBeltIndexDebug = -1;
        [SerializeField] private string _selectedBeltItemIdDebug;
        [SerializeField] private bool _inputResolvedDebug;

        private IPlayerInputSource _inputSource;
        private IInventoryPickupTargetResolver _pickupTargetResolver;
        private IInventoryEvents _inventoryEvents;
        private IInventoryEvents _subscribedInventoryEvents;
        private bool _useRuntimeKernelInventoryEvents = true;
        private readonly Dictionary<string, IInventoryPickupTarget> _pendingPickupTargetsById = new Dictionary<string, IInventoryPickupTarget>();
        private bool _loggedMissingInputSource;
        private bool _flushPickupInputAtEndOfFrame;

        public PlayerInventoryRuntime Runtime { get; private set; }
        public int SelectedBeltIndexDebug => _selectedBeltIndexDebug;

        private void Awake()
        {
            Runtime = new PlayerInventoryRuntime();
            Runtime.SetBackpackCapacity(ResolveStartingBackpackCapacity());
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeToRuntimeHubReconfigure();
            SubscribeToInventoryEvents(ResolveInventoryEvents());
        }

        private void OnDisable()
        {
            UnsubscribeFromRuntimeHubReconfigure();
            UnsubscribeFromInventoryEvents();
            _pendingPickupTargetsById.Clear();
            _flushPickupInputAtEndOfFrame = false;
            if (!_interactionCoordinatorModeEnabled)
            {
                ClearInteractionHint();
            }
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

        public void Configure(
            IPlayerInputSource inputSource,
            IInventoryPickupTargetResolver pickupTargetResolver,
            PlayerInventoryRuntime runtime = null,
            IInventoryEvents inventoryEvents = null)
        {
            _inputSource = inputSource;
            _pickupTargetResolver = pickupTargetResolver;
            _useRuntimeKernelInventoryEvents = inventoryEvents == null;
            _inventoryEvents = inventoryEvents;
            if (isActiveAndEnabled)
            {
                SubscribeToInventoryEvents(ResolveInventoryEvents());
            }

            Runtime = runtime ?? new PlayerInventoryRuntime();
            Runtime.SetBackpackCapacity(ResolveStartingBackpackCapacity());
        }

        public void Tick()
        {
            UpdateDebugFields();
            if (Runtime == null)
            {
                return;
            }

            if (_inputSource == null)
            {
                ResolveReferences();
            }

            if (!DependencyResolutionGuard.HasRequiredReferences(
                    ref _loggedMissingInputSource,
                    this,
                    "PlayerInventoryController requires an IPlayerInputSource reference.",
                    _inputSource))
            {
                return;
            }

            var beltPressed = _inputSource.ConsumeBeltSelectPressed();
            if (beltPressed >= 0)
            {
                var previous = Runtime.SelectedBeltIndex;
                Runtime.SelectBeltSlot(beltPressed);
                if (Runtime.SelectedBeltIndex != previous)
                {
                    ResolveInventoryEvents().RaiseBeltSelectionChanged(Runtime.SelectedBeltIndex);
                    UpdateDebugFields();
                }
            }

            if (Keyboard.current != null
                && Keyboard.current.gKey.wasPressedThisFrame
                && !IsAnyMenuOpen())
            {
                TryDropSelectedBeltItem();
            }

            if (_interactionCoordinatorModeEnabled)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            if (_pickupTargetResolver == null || !_pickupTargetResolver.TryResolvePickupTarget(out var pickupTarget) || pickupTarget == null)
            {
                _flushPickupInputAtEndOfFrame = true;
                ClearInteractionHint();
                return;
            }

            var uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            if (uiStateEvents != null && uiStateEvents.IsAnyMenuOpen)
            {
                ClearInteractionHint();
            }
            else
            {
                PublishPickupHint(pickupTarget);
            }

            var pickupPressedThisFrame = IsPickupPressedThisFrame();
            if (!pickupPressedThisFrame)
            {
                _flushPickupInputAtEndOfFrame = false;
                return;
            }

            _flushPickupInputAtEndOfFrame = false;
            TryExecutePickupTarget(pickupTarget);
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

            if (_pickupTargetResolver == null)
            {
                ResolveReferences();
            }

            if (_pickupTargetResolver == null || !_pickupTargetResolver.TryResolvePickupTarget(out var pickupTarget) || pickupTarget == null)
            {
                return false;
            }

            var uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            if (uiStateEvents != null && uiStateEvents.IsAnyMenuOpen)
            {
                return false;
            }

            var resolvedItemId = ResolvePickupItemId(pickupTarget);
            if (string.IsNullOrWhiteSpace(resolvedItemId))
            {
                return false;
            }

            var subjectText = ResolvePickupDisplayName(pickupTarget);
            var targetInstanceId = pickupTarget is UnityEngine.Object pickupTargetObject ? pickupTargetObject.GetInstanceID() : 0;
            var stableTieBreaker = $"{resolvedItemId}:{targetInstanceId}";
            candidate = new PlayerInteractionCandidate(
                PickupHintContextId,
                PickupHintActionText,
                subjectText,
                _interactionPriority,
                stableTieBreaker,
                PlayerInteractionActionKind.Pickup,
                () => TryExecutePickupTarget(pickupTarget));
            return true;
        }

        public bool TryMoveItem(InventoryArea sourceArea, int sourceIndex, InventoryArea targetArea, int targetIndex)
        {
            if (Runtime == null)
            {
                return false;
            }

            var moved = Runtime.TryMoveItem(sourceArea, sourceIndex, targetArea, targetIndex);
            if (!moved)
            {
                return false;
            }

            ResolveInventoryEvents().RaiseInventoryChanged();
            UpdateDebugFields();
            return true;
        }

        public bool TryGetSelectedInventoryItemId(out string itemId)
        {
            itemId = Runtime?.SelectedBeltItemId;
            return !string.IsNullOrWhiteSpace(itemId);
        }

        public bool TryConsumeSelectedBeltItem(out string consumedItemId)
        {
            consumedItemId = null;
            if (Runtime == null)
            {
                return false;
            }

            var selectedBeltIndex = Runtime.SelectedBeltIndex;
            if (!Runtime.TryRemoveFromBeltSlot(selectedBeltIndex, 1, out var selectedItemId))
            {
                return false;
            }

            ResolveInventoryEvents()?.RaiseInventoryChanged();
            UpdateDebugFields();
            consumedItemId = selectedItemId;
            return true;
        }

        public bool TryDropSelectedBeltItem()
        {
            if (Runtime == null)
            {
                return false;
            }

            if (Runtime.SelectedBeltIndex >= 0
                && TryDropItemFromSlot(InventoryArea.Belt, Runtime.SelectedBeltIndex))
            {
                return true;
            }

            for (var i = 0; i < PlayerInventoryRuntime.BeltSlotCount; i++)
            {
                if (string.IsNullOrWhiteSpace(Runtime.BeltSlotItemIds[i]))
                {
                    continue;
                }

                if (TryDropItemFromSlot(InventoryArea.Belt, i))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryDropItemFromSlot(InventoryArea area, int slotIndex)
        {
            if (Runtime == null || !Runtime.TryRemoveFromSlot(area, slotIndex, out var droppedItemId, out var droppedQuantity))
            {
                return false;
            }

            var spawned = TrySpawnDroppedWorldItem(droppedItemId, droppedQuantity);
            if (!spawned)
            {
                Runtime.TryAddStackItem(droppedItemId, droppedQuantity, out _, out _, out _);
                return false;
            }

            ResolveInventoryEvents()?.RaiseInventoryChanged();
            UpdateDebugFields();
            return true;
        }

        public bool TryStoreItemWithBeltPriority(string itemId)
        {
            if (Runtime == null)
            {
                return false;
            }

            if (!Runtime.TryStoreItem(itemId, out var area, out var index, out _))
            {
                return false;
            }

            var inventoryEvents = ResolveInventoryEvents();
            inventoryEvents?.RaiseItemStored(itemId, area, index);
            inventoryEvents?.RaiseInventoryChanged();
            UpdateDebugFields();
            return true;
        }

        public bool CanStoreItemWithBeltPriority(string itemId)
        {
            if (Runtime == null || string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            return Runtime.CanStoreItem(itemId);
        }

        public IReadOnlyList<ItemDefinition> GetItemDefinitionRegistrySnapshot()
        {
            if (_itemDefinitionRegistry == null || _itemDefinitionRegistry.Count == 0)
            {
                return Array.Empty<ItemDefinition>();
            }

            return _itemDefinitionRegistry.ToArray();
        }

        private void HandleItemPickupRequested(string itemId)
        {
            if (Runtime == null || string.IsNullOrWhiteSpace(itemId))
            {
                return;
            }

            var hasPendingTarget = _pendingPickupTargetsById.TryGetValue(itemId, out var pendingTarget);
            if (hasPendingTarget && pendingTarget == null)
            {
                _pendingPickupTargetsById.Remove(itemId);
                hasPendingTarget = false;
            }

            if (hasPendingTarget
                && pendingTarget is IInventoryDefinitionPickupTarget definitionTarget
                && definitionTarget.SpawnDefinition != null
                && definitionTarget.SpawnDefinition.ItemDefinition != null)
            {
                EnsureItemDefinitionRegistered(definitionTarget.SpawnDefinition.ItemDefinition);
                var maxStack = definitionTarget.SpawnDefinition.ItemDefinition.MaxStack;
                Runtime.SetItemMaxStack(itemId, maxStack);
            }

            var stackTarget = pendingTarget as IInventoryStackPickupTarget;
            var isStackPickup = hasPendingTarget && stackTarget != null;
            var quantity = isStackPickup ? Mathf.Max(1, stackTarget.Quantity) : 1;
            var area = InventoryArea.Belt;
            var index = -1;
            var rejectReason = PickupRejectReason.InvalidItem;
            var stored = isStackPickup
                ? Runtime.TryAddStackItem(itemId, quantity, out area, out index, out rejectReason)
                : Runtime.TryStoreItem(itemId, out area, out index, out rejectReason);
            if (!stored)
            {
                ResolveInventoryEvents().RaiseItemPickupRejected(itemId, rejectReason);
                if (hasPendingTarget)
                {
                    _pendingPickupTargetsById.Remove(itemId);
                }

                return;
            }

            ResolveInventoryEvents().RaiseItemStored(itemId, area, index);
            ResolveInventoryEvents().RaiseInventoryChanged();
            if (hasPendingTarget && _pendingPickupTargetsById.TryGetValue(itemId, out var target) && target != null)
            {
                CapturePickupConsumedMutation(target);
                target.OnPickedUp();
                _pendingPickupTargetsById.Remove(itemId);
            }
        }

        private void ResolveReferences()
        {
            _inputSource ??= _inputSourceBehaviour as IPlayerInputSource;
            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponents<MonoBehaviour>());
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInParent<MonoBehaviour>(true));
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(GetComponentsInChildren<MonoBehaviour>(true));
            }

            if (_inputSource == null)
            {
                _inputSource = DependencyResolutionGuard.FindInterface<IPlayerInputSource>(FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None));
            }
            else
            {
                _loggedMissingInputSource = false;
            }

            _pickupTargetResolver ??= _pickupTargetResolverBehaviour as IInventoryPickupTargetResolver;
            if (_pickupTargetResolver == null)
            {
                _pickupTargetResolver = DependencyResolutionGuard.FindInterface<IInventoryPickupTargetResolver>(GetComponents<MonoBehaviour>());
            }

            ResolveInventoryEvents();
        }

        private void HandleRuntimeEventsReconfigured()
        {
            if (!isActiveAndEnabled || !_useRuntimeKernelInventoryEvents)
            {
                return;
            }

            SubscribeToInventoryEvents(ResolveInventoryEvents());
        }

        private IInventoryEvents ResolveInventoryEvents()
        {
            if (_useRuntimeKernelInventoryEvents)
            {
                var runtimeInventoryEvents = RuntimeKernelBootstrapper.InventoryEvents;
                if (!ReferenceEquals(_inventoryEvents, runtimeInventoryEvents))
                {
                    _inventoryEvents = runtimeInventoryEvents;
                    SubscribeToInventoryEvents(_inventoryEvents);
                }
                else if (!ReferenceEquals(_subscribedInventoryEvents, _inventoryEvents))
                {
                    SubscribeToInventoryEvents(_inventoryEvents);
                }

                return _inventoryEvents;
            }

            if (!ReferenceEquals(_subscribedInventoryEvents, _inventoryEvents))
            {
                SubscribeToInventoryEvents(_inventoryEvents);
            }

            return _inventoryEvents;
        }

        private void SubscribeToInventoryEvents(IInventoryEvents inventoryEvents)
        {
            if (inventoryEvents == null)
            {
                UnsubscribeFromInventoryEvents();
                return;
            }

            if (ReferenceEquals(_subscribedInventoryEvents, inventoryEvents))
            {
                return;
            }

            UnsubscribeFromInventoryEvents();
            _subscribedInventoryEvents = inventoryEvents;
            _subscribedInventoryEvents.OnItemPickupRequested += HandleItemPickupRequested;
        }

        private void UnsubscribeFromInventoryEvents()
        {
            if (_subscribedInventoryEvents == null)
            {
                return;
            }

            _subscribedInventoryEvents.OnItemPickupRequested -= HandleItemPickupRequested;
            _subscribedInventoryEvents = null;
        }

        private void SubscribeToRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
            RuntimeKernelBootstrapper.EventsReconfigured += HandleRuntimeEventsReconfigured;
        }

        private void UnsubscribeFromRuntimeHubReconfigure()
        {
            RuntimeKernelBootstrapper.EventsReconfigured -= HandleRuntimeEventsReconfigured;
        }

        private static string ResolvePickupItemId(IInventoryPickupTarget pickupTarget)
        {
            if (pickupTarget is IInventoryDefinitionPickupTarget definitionTarget
                && definitionTarget.SpawnDefinition != null
                && definitionTarget.SpawnDefinition.IsValid)
            {
                return definitionTarget.SpawnDefinition.ItemId;
            }

            return pickupTarget.ItemId;
        }

        private void PublishPickupHint(IInventoryPickupTarget pickupTarget)
        {
            var subjectText = ResolvePickupDisplayName(pickupTarget);
            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintShown(
                new InteractionHintPayload(PickupHintContextId, PickupHintActionText, subjectText));
        }

        private static void ClearInteractionHint()
        {
            RuntimeKernelBootstrapper.InteractionHintEvents?.RaiseInteractionHintCleared(PickupHintContextId);
        }

        private string ResolvePickupDisplayName(IInventoryPickupTarget pickupTarget)
        {
            if (pickupTarget is IInventoryDefinitionPickupTarget definitionTarget
                && definitionTarget.SpawnDefinition != null
                && definitionTarget.SpawnDefinition.ItemDefinition != null
                && !string.IsNullOrWhiteSpace(definitionTarget.SpawnDefinition.ItemDefinition.DisplayName))
            {
                return definitionTarget.SpawnDefinition.ItemDefinition.DisplayName;
            }

            var itemId = ResolvePickupItemId(pickupTarget);
            if (TryResolveDisplayNameFromRegistry(itemId, out var displayName))
            {
                return displayName;
            }

            return FormatItemId(itemId);
        }

        private bool TryResolveDisplayNameFromRegistry(string itemId, out string displayName)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                displayName = string.Empty;
                return false;
            }

            for (var i = 0; i < _itemDefinitionRegistry.Count; i++)
            {
                var definition = _itemDefinitionRegistry[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
                {
                    continue;
                }

                if (!string.Equals(definition.DefinitionId, itemId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    continue;
                }

                displayName = definition.DisplayName;
                return true;
            }

            displayName = string.Empty;
            return false;
        }

        private static string FormatItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return "Item";
            }

            var builder = new StringBuilder(itemId.Length);
            var uppercaseNext = true;
            for (var i = 0; i < itemId.Length; i++)
            {
                var character = itemId[i];
                if (character == '-' || character == '_')
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != ' ')
                    {
                        builder.Append(' ');
                    }

                    uppercaseNext = true;
                    continue;
                }

                if (uppercaseNext && char.IsLetter(character))
                {
                    builder.Append(char.ToUpperInvariant(character));
                    uppercaseNext = false;
                    continue;
                }

                if (char.IsLetter(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    uppercaseNext = false;
                    continue;
                }

                builder.Append(character);
                uppercaseNext = false;
            }

            return builder.ToString().Trim();
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

        private bool TryExecutePickupTarget(IInventoryPickupTarget pickupTarget)
        {
            var resolvedItemId = ResolvePickupItemId(pickupTarget);
            if (string.IsNullOrWhiteSpace(resolvedItemId))
            {
                return false;
            }

            _pendingPickupTargetsById[resolvedItemId] = pickupTarget;
            ResolveInventoryEvents().RaiseItemPickupRequested(resolvedItemId);
            return true;
        }

        private static void CapturePickupConsumedMutation(IInventoryPickupTarget pickupTarget)
        {
            if (pickupTarget is not Component component || component == null)
            {
                return;
            }

            if (!component.TryGetComponent<WorldObjectIdentity>(out var identity) || identity == null)
            {
                return;
            }

            var scene = component.gameObject.scene;
            if (!scene.IsValid() || string.IsNullOrWhiteSpace(scene.path))
            {
                return;
            }

            WorldObjectPersistenceRuntimeBridge.MarkConsumed(scene.path, identity.ObjectId);
        }

        private bool IsAnyMenuOpen()
        {
            var uiStateEvents = RuntimeKernelBootstrapper.UiStateEvents;
            return uiStateEvents != null && uiStateEvents.IsAnyMenuOpen;
        }

        private bool TrySpawnDroppedWorldItem(string itemId, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
            {
                return false;
            }

            TryResolveItemDefinition(itemId, out var definition);
            var spawnPosition = ResolveDropSpawnPosition();
            var spawnRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            if (!RuntimeDroppedItemFactory.TryCreate(
                    itemId,
                    quantity,
                    definition,
                    spawnPosition,
                    spawnRotation,
                    out _,
                    out var rigidbody,
                    _dropColliderSize))
            {
                return false;
            }

            var push = transform.forward * Mathf.Max(0f, _dropImpulse);
            rigidbody.AddForce(push, ForceMode.Impulse);
            rigidbody.AddTorque(UnityEngine.Random.onUnitSphere * Mathf.Max(0f, _dropSpinImpulse), ForceMode.Impulse);
            return true;
        }

        private Vector3 ResolveDropSpawnPosition()
        {
            var horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (horizontalForward.sqrMagnitude < 0.0001f)
            {
                horizontalForward = transform.forward;
            }

            horizontalForward.Normalize();
            return transform.position
                   + (horizontalForward * Mathf.Max(0.5f, _dropForwardDistance))
                   + (Vector3.up * Mathf.Max(0.2f, _dropVerticalOffset));
        }

        private bool TryResolveItemDefinition(string itemId, out ItemDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }

            if (TryResolveItemDefinitionFromRegistry(itemId, out definition))
            {
                return true;
            }

            return false;
        }

        private bool TryResolveItemDefinitionFromRegistry(string itemId, out ItemDefinition definition)
        {
            definition = null;
            if (_itemDefinitionRegistry == null)
            {
                return false;
            }

            for (var i = 0; i < _itemDefinitionRegistry.Count; i++)
            {
                var candidate = _itemDefinitionRegistry[i];
                if (candidate == null || string.IsNullOrWhiteSpace(candidate.DefinitionId))
                {
                    continue;
                }

                if (!string.Equals(candidate.DefinitionId, itemId, StringComparison.Ordinal))
                {
                    continue;
                }

                definition = candidate;
                return true;
            }

            definition = null;
            return false;
        }

        private void EnsureItemDefinitionRegistered(ItemDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.DefinitionId))
            {
                return;
            }

            for (var i = 0; i < _itemDefinitionRegistry.Count; i++)
            {
                var existing = _itemDefinitionRegistry[i];
                if (existing == null || string.IsNullOrWhiteSpace(existing.DefinitionId))
                {
                    continue;
                }

                if (string.Equals(existing.DefinitionId, definition.DefinitionId, StringComparison.Ordinal))
                {
                    return;
                }
            }

            _itemDefinitionRegistry.Add(definition);
        }

        private void UpdateDebugFields()
        {
            _inputResolvedDebug = _inputSource != null;
            if (Runtime == null)
            {
                _selectedBeltIndexDebug = -1;
                _selectedBeltItemIdDebug = null;
                return;
            }

            _selectedBeltIndexDebug = Runtime.SelectedBeltIndex;
            _selectedBeltItemIdDebug = Runtime.SelectedBeltItemId;
        }

        private int ResolveStartingBackpackCapacity()
        {
            return _startingBackpackCapacity > 0 ? _startingBackpackCapacity : DefaultBackpackCapacity;
        }

    }
}
