using UnityEngine;

namespace Reloader.World.Travel
{
    [DisallowMultipleComponent]
    public sealed class TravelSceneTrigger : MonoBehaviour
    {
        [SerializeField] private TravelContext _travelContext;

        public bool TryTravel()
        {
            if (_travelContext == null)
            {
                return false;
            }

            return WorldTravelCoordinator.TryTravel(_travelContext);
        }

        public void Configure(TravelContext context)
        {
            _travelContext = context;
        }
    }
}
