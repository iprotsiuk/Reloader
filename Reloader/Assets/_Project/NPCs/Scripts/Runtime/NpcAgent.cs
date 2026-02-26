using System.Collections.Generic;
using Reloader.NPCs.Data;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class NpcAgent : MonoBehaviour
    {
        [SerializeField] private NpcDefinition _definition;

        private readonly List<INpcCapability> _capabilities = new List<INpcCapability>();
        private readonly List<INpcActionExecutor> _actionExecutors = new List<INpcActionExecutor>();
        private bool _initialized;

        public NpcDefinition Definition => _definition;

        public void InitializeCapabilities()
        {
            SynchronizeCapabilities();
            _initialized = true;
        }

        public NpcActionCollection CollectActions()
        {
            InitializeCapabilities();

            var actions = new List<NpcActionDefinition>();
            for (var i = 0; i < _capabilities.Count; i++)
            {
                if (!(_capabilities[i] is INpcActionProvider provider))
                {
                    continue;
                }

                if (_capabilities[i] is MonoBehaviour behaviour && !behaviour.isActiveAndEnabled)
                {
                    continue;
                }

                var provided = provider.GetActions();
                if (provided == null || provided.Length == 0)
                {
                    continue;
                }

                actions.AddRange(provided);
            }

            return new NpcActionCollection(actions);
        }

        public bool TryExecuteAction(string actionKey, string payload, out NpcActionExecutionResult result)
        {
            InitializeCapabilities();

            if (string.IsNullOrWhiteSpace(actionKey))
            {
                result = new NpcActionExecutionResult(string.Empty, false, "npc.action.invalid-key");
                return false;
            }

            var context = new NpcActionExecutionContext(actionKey, payload);
            for (var i = 0; i < _actionExecutors.Count; i++)
            {
                var executor = _actionExecutors[i];
                if (executor == null || !executor.CanExecuteAction(actionKey))
                {
                    continue;
                }

                return executor.TryExecuteAction(in context, out result);
            }

            result = new NpcActionExecutionResult(actionKey, false, "npc.action.unhandled");
            return false;
        }

        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            for (var i = 0; i < _capabilities.Count; i++)
            {
                var capability = _capabilities[i];
                if (capability == null)
                {
                    continue;
                }

                if (capability is UnityEngine.Object unityObject && unityObject == null)
                {
                    continue;
                }

                capability.Shutdown();
            }

            _capabilities.Clear();
            _initialized = false;
            _actionExecutors.Clear();
        }

        private void SynchronizeCapabilities()
        {
            var activeCapabilities = new List<INpcCapability>();
            var behaviours = new List<MonoBehaviour>();
            GetComponents(behaviours);
            for (var i = 0; i < behaviours.Count; i++)
            {
                if (behaviours[i] == null || !behaviours[i].isActiveAndEnabled || !(behaviours[i] is INpcCapability capability))
                {
                    continue;
                }

                activeCapabilities.Add(capability);
            }

            for (var i = _capabilities.Count - 1; i >= 0; i--)
            {
                var existing = _capabilities[i];
                if (existing == null || !activeCapabilities.Contains(existing))
                {
                    existing?.Shutdown();
                    _capabilities.RemoveAt(i);
                }
            }

            for (var i = 0; i < activeCapabilities.Count; i++)
            {
                var capability = activeCapabilities[i];
                if (_capabilities.Contains(capability))
                {
                    continue;
                }

                _capabilities.Add(capability);
                capability.Initialize(this);
            }

            _actionExecutors.Clear();
            for (var i = 0; i < _capabilities.Count; i++)
            {
                if (_capabilities[i] is INpcActionExecutor executor)
                {
                    _actionExecutors.Add(executor);
                }
            }
        }
    }
}
