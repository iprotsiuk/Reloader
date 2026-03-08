using System.Collections.Generic;
using Reloader.Core.Save;
using Reloader.Core.Save.Modules;
using UnityEngine;

namespace Reloader.Core.Runtime
{
    public sealed class CoreWorldController : MonoBehaviour, ISaveRuntimeBridge
    {
        [SerializeField] private int _startingDayCount;
        [SerializeField] private float _startingTimeOfDay = 8f;
        [SerializeField] private float _worldMinutesPerRealSecond = 1f;

        private CoreWorldRuntime _runtime;

        public CoreWorldRuntime Runtime => _runtime;
        public event System.Action WorldStateChanged;

        private void Awake()
        {
            _runtime = new CoreWorldRuntime(_startingDayCount, _startingTimeOfDay);
            WorldStateChanged?.Invoke();
        }

        private void OnEnable()
        {
            SaveRuntimeBridgeRegistry.Register(this);
        }

        private void OnDisable()
        {
            SaveRuntimeBridgeRegistry.Unregister(this);
        }

        private void Update()
        {
            AdvanceRealtimeSeconds(Time.deltaTime);
        }

        public CoreWorldRuntime.Snapshot CaptureSnapshot()
        {
            return _runtime?.CaptureSnapshot() ?? new CoreWorldRuntime.Snapshot(_startingDayCount, _startingTimeOfDay);
        }

        public void SetWorldState(int dayCount, float timeOfDay)
        {
            ApplyWorldState(dayCount, timeOfDay, raiseEvent: true);
        }

        public void AdvanceRealtimeSeconds(float realtimeSeconds)
        {
            EnsureRuntimeInitialized();
            if (_runtime.AdvanceRealtimeSeconds(realtimeSeconds, _worldMinutesPerRealSecond))
            {
                WorldStateChanged?.Invoke();
            }
        }

        public void PrepareForSave(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            var module = ResolveModule(moduleRegistrations);
            if (module == null)
            {
                return;
            }

            var snapshot = CaptureSnapshot();
            module.DayCount = snapshot.DayCount;
            module.TimeOfDay = snapshot.TimeOfDay;
        }

        public void FinalizeAfterLoad(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            var module = ResolveModule(moduleRegistrations);
            if (module == null)
            {
                return;
            }

            ApplyWorldState(module.DayCount, module.TimeOfDay, raiseEvent: false);
        }

        private void ApplyWorldState(int dayCount, float timeOfDay, bool raiseEvent)
        {
            EnsureRuntimeInitialized();
            _runtime.SetWorldState(dayCount, timeOfDay);

            if (raiseEvent)
            {
                WorldStateChanged?.Invoke();
            }
        }

        private void EnsureRuntimeInitialized()
        {
            _runtime ??= new CoreWorldRuntime(_startingDayCount, _startingTimeOfDay);
        }

        private static CoreWorldModule ResolveModule(IReadOnlyList<SaveModuleRegistration> moduleRegistrations)
        {
            if (moduleRegistrations == null)
            {
                return null;
            }

            for (var i = 0; i < moduleRegistrations.Count; i++)
            {
                if (moduleRegistrations[i].Module is CoreWorldModule module)
                {
                    return module;
                }
            }

            return null;
        }
    }
}
