using System;
using System.Collections.Generic;
using Reloader.Inventory;
using Reloader.NPCs.Runtime;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class HumanoidCorpseLootController : MonoBehaviour
    {
        [SerializeField] private HumanoidDamageReceiver _damageReceiver;
        [SerializeField] private Behaviour[] _disableBehavioursOnDeath = Array.Empty<Behaviour>();
        [SerializeField] private int _corpseSlotCapacity = 12;
        [SerializeField] private StorageContainerPolicy _corpsePolicy = StorageContainerPolicy.Persistent;
        [SerializeField] private string _containerIdPrefix = "corpse";
        [SerializeField] private string _displayNamePrefix = "Corpse of ";

        private readonly List<Behaviour> _resolvedDisableBehaviours = new List<Behaviour>();
        private readonly Dictionary<Behaviour, bool> _initialBehaviourEnabledStates = new Dictionary<Behaviour, bool>();
        private bool _hasTakenOver;

        public bool CanPresentDeathState
        {
            get
            {
                ResolveDependencies();
                return isActiveAndEnabled && _damageReceiver != null;
            }
        }

        private void Reset()
        {
            ResolveDependencies();
        }

        private void Awake()
        {
            ResolveDependencies();
            Subscribe();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void ResetRuntime()
        {
            ResolveDependencies();
            _hasTakenOver = false;
            RestoreDependencies();
            CleanupCorpseStorageContainer();
        }

        private void HandleDied()
        {
            if (_damageReceiver == null || !_damageReceiver.HasLastResult || _hasTakenOver)
            {
                return;
            }

            ResolveDependencies();
            _hasTakenOver = true;
            DisableDependencies();
            EnsureCorpseStorageContainer();
        }

        private void ResolveDependencies()
        {
            _damageReceiver ??= GetComponent<HumanoidDamageReceiver>();

            _resolvedDisableBehaviours.Clear();
            AddDisableBehaviour(GetComponentInChildren<Animator>(includeInactive: true));
            AddDisableBehaviour(GetComponent<NpcAiController>());
            AddDisableBehaviour(GetComponent<ContractTargetPatrolMotion>());

            if (_disableBehavioursOnDeath != null)
            {
                for (var i = 0; i < _disableBehavioursOnDeath.Length; i++)
                {
                    AddDisableBehaviour(_disableBehavioursOnDeath[i]);
                }
            }

            CaptureInitialState();
        }

        private void Subscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            Unsubscribe();
            _damageReceiver.Died += HandleDied;
        }

        private void Unsubscribe()
        {
            if (_damageReceiver == null)
            {
                return;
            }

            _damageReceiver.Died -= HandleDied;
        }

        private void DisableDependencies()
        {
            for (var i = 0; i < _resolvedDisableBehaviours.Count; i++)
            {
                var behaviour = _resolvedDisableBehaviours[i];
                if (behaviour != null)
                {
                    behaviour.enabled = false;
                }
            }
        }

        private void CaptureInitialState()
        {
            for (var i = 0; i < _resolvedDisableBehaviours.Count; i++)
            {
                var behaviour = _resolvedDisableBehaviours[i];
                if (behaviour != null && !_initialBehaviourEnabledStates.ContainsKey(behaviour))
                {
                    _initialBehaviourEnabledStates.Add(behaviour, behaviour.enabled);
                }
            }
        }

        private void RestoreDependencies()
        {
            foreach (var pair in _initialBehaviourEnabledStates)
            {
                if (pair.Key != null)
                {
                    pair.Key.enabled = pair.Value;
                }
            }
        }

        private void EnsureCorpseStorageContainer()
        {
            var container = GetComponent<WorldStorageContainer>();
            if (container == null)
            {
                container = gameObject.AddComponent<WorldStorageContainer>();
            }

            container.ConfigureRuntimeIdentity(
                BuildContainerId(),
                BuildDisplayName(),
                _corpseSlotCapacity,
                _corpsePolicy);
            container.EnsureRegistered();
        }

        private void CleanupCorpseStorageContainer()
        {
            var container = GetComponent<WorldStorageContainer>();
            if (container == null)
            {
                return;
            }

            var containerId = container.ContainerId;
            if (!string.IsNullOrWhiteSpace(containerId) &&
                !containerId.StartsWith($"{_containerIdPrefix}.", StringComparison.Ordinal))
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(container);
                return;
            }

            DestroyImmediate(container);
        }

        private string BuildContainerId()
        {
            var sourceId = ResolveSourceId();
            var guid = Guid.NewGuid().ToString("N");
            return string.IsNullOrWhiteSpace(sourceId)
                ? $"{_containerIdPrefix}.{guid}"
                : $"{_containerIdPrefix}.{sourceId}.{guid}";
        }

        private string BuildDisplayName()
        {
            var sourceName = ResolveDisplayName();
            return string.IsNullOrWhiteSpace(sourceName)
                ? "Corpse"
                : string.Concat(_displayNamePrefix, sourceName);
        }

        private string ResolveSourceId()
        {
            var spawnedCivilian = GetComponent<MainTownPopulationSpawnedCivilian>();
            if (spawnedCivilian != null && !string.IsNullOrWhiteSpace(spawnedCivilian.CivilianId))
            {
                return spawnedCivilian.CivilianId.Trim();
            }

            return string.IsNullOrWhiteSpace(gameObject.name) ? string.Empty : gameObject.name.Trim();
        }

        private string ResolveDisplayName()
        {
            var spawnedCivilian = GetComponent<MainTownPopulationSpawnedCivilian>();
            if (spawnedCivilian != null && !string.IsNullOrWhiteSpace(spawnedCivilian.PublicDisplayName))
            {
                return spawnedCivilian.PublicDisplayName.Trim();
            }

            return string.IsNullOrWhiteSpace(gameObject.name) ? string.Empty : gameObject.name.Trim();
        }

        private void AddDisableBehaviour(Behaviour behaviour)
        {
            if (behaviour == null || _resolvedDisableBehaviours.Contains(behaviour))
            {
                return;
            }

            _resolvedDisableBehaviours.Add(behaviour);
        }
    }
}
