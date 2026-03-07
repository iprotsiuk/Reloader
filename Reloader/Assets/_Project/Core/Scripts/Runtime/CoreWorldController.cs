using UnityEngine;

namespace Reloader.Core.Runtime
{
    public sealed class CoreWorldController : MonoBehaviour
    {
        [SerializeField] private int _startingDayCount;
        [SerializeField] private float _startingTimeOfDay = 8f;

        private CoreWorldRuntime _runtime;

        public CoreWorldRuntime Runtime => _runtime;
        public event System.Action WorldStateChanged;

        private void Awake()
        {
            _runtime = new CoreWorldRuntime(_startingDayCount, _startingTimeOfDay);
            WorldStateChanged?.Invoke();
        }

        public CoreWorldRuntime.Snapshot CaptureSnapshot()
        {
            return _runtime?.CaptureSnapshot() ?? new CoreWorldRuntime.Snapshot(_startingDayCount, _startingTimeOfDay);
        }

        public void SetWorldState(int dayCount, float timeOfDay)
        {
            if (_runtime == null)
            {
                _runtime = new CoreWorldRuntime(dayCount, timeOfDay);
            }
            else
            {
                _runtime.SetWorldState(dayCount, timeOfDay);
            }

            WorldStateChanged?.Invoke();
        }
    }
}
