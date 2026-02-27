using UnityEngine;

namespace Reloader.World.Travel
{
    [DisallowMultipleComponent]
    public sealed class TravelSceneTrigger : MonoBehaviour
    {
        [SerializeField] private TravelContext _travelContext;
        [SerializeField] private string _requiredInteractorTag = "";

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

        public bool TryHandleInteractor(GameObject interactor)
        {
            if (!IsInteractorAllowed(interactor))
            {
                return false;
            }

            return TryTravel();
        }

        public void SetInteractorTag(string requiredTag)
        {
            _requiredInteractorTag = requiredTag == null ? "" : requiredTag.Trim();
        }

        public bool IsInteractorAllowed(GameObject interactor)
        {
            if (interactor == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_requiredInteractorTag))
            {
                return true;
            }

            return interactor.CompareTag(_requiredInteractorTag);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHandleInteractor(other != null ? other.gameObject : null);
        }
    }
}
