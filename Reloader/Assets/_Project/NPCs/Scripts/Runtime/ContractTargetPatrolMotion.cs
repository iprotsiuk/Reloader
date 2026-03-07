using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    public sealed class ContractTargetPatrolMotion : MonoBehaviour
    {
        [SerializeField] private Transform[] _waypoints = new Transform[0];
        [SerializeField] private float _moveSpeedMetersPerSecond = 1.4f;
        [SerializeField] private float _arrivalThresholdMeters = 0.1f;
        [SerializeField] private bool _loop = true;
        [SerializeField] private bool _orientToMotion = true;

        private int _currentWaypointIndex;
        private int _direction = 1;

        private void Update()
        {
            if (_waypoints == null || _waypoints.Length == 0)
            {
                return;
            }

            var target = _waypoints[_currentWaypointIndex];
            if (target == null)
            {
                AdvanceWaypoint();
                return;
            }

            var currentPosition = transform.position;
            var destination = target.position;
            var nextPosition = Vector3.MoveTowards(
                currentPosition,
                destination,
                Mathf.Max(0f, _moveSpeedMetersPerSecond) * Time.deltaTime);
            var delta = nextPosition - currentPosition;
            transform.position = nextPosition;

            if (_orientToMotion && delta.sqrMagnitude > 0.0001f)
            {
                var planar = new Vector3(delta.x, 0f, delta.z);
                if (planar.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(planar.normalized, Vector3.up);
                }
            }

            if (Vector3.Distance(transform.position, destination) <= Mathf.Max(0.01f, _arrivalThresholdMeters))
            {
                AdvanceWaypoint();
            }
        }

        private void AdvanceWaypoint()
        {
            if (_waypoints == null || _waypoints.Length <= 1)
            {
                return;
            }

            if (_loop)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Length;
                return;
            }

            if (_currentWaypointIndex == _waypoints.Length - 1)
            {
                _direction = -1;
            }
            else if (_currentWaypointIndex == 0)
            {
                _direction = 1;
            }

            _currentWaypointIndex = Mathf.Clamp(_currentWaypointIndex + _direction, 0, _waypoints.Length - 1);
        }
    }
}
