using System;
using UnityEngine;

namespace Reloader.World.Travel
{
    [Serializable]
    public sealed class TravelContext
    {
        [SerializeField] private string _destinationSceneName;
        [SerializeField] private string _destinationEntryPointId;
        [SerializeField] private string _returnEntryPointId;
        [SerializeField] private TravelActivityType _activityType;
        [SerializeField] private TravelTimeAdvancePolicy _timeAdvancePolicy;

        public TravelContext(
            string destinationSceneName,
            string destinationEntryPointId,
            string returnEntryPointId,
            TravelActivityType activityType,
            TravelTimeAdvancePolicy timeAdvancePolicy)
        {
            _destinationSceneName = ValidateRequired(destinationSceneName, nameof(destinationSceneName));
            _destinationEntryPointId = ValidateRequired(destinationEntryPointId, nameof(destinationEntryPointId));
            _returnEntryPointId = ValidateRequired(returnEntryPointId, nameof(returnEntryPointId));
            _activityType = activityType;
            _timeAdvancePolicy = timeAdvancePolicy;
        }

        public string DestinationSceneName => _destinationSceneName;
        public string DestinationEntryPointId => _destinationEntryPointId;
        public string ReturnEntryPointId => _returnEntryPointId;
        public TravelActivityType ActivityType => _activityType;
        public TravelTimeAdvancePolicy TimeAdvancePolicy => _timeAdvancePolicy;

        public void Validate()
        {
            _destinationSceneName = ValidateRequired(_destinationSceneName, nameof(DestinationSceneName));
            _destinationEntryPointId = ValidateRequired(_destinationEntryPointId, nameof(DestinationEntryPointId));
            _returnEntryPointId = ValidateRequired(_returnEntryPointId, nameof(ReturnEntryPointId));
        }

        private static string ValidateRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{fieldName} is required.", fieldName);
            }

            return value.Trim();
        }
    }
}
