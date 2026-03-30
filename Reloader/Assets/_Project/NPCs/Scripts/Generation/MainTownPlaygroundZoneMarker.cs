using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.NPCs.Generation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MainTownPlaygroundZoneMarker : MonoBehaviour
    {
        [SerializeField] private string _areaTag = string.Empty;
        [SerializeField] private string _primaryPoolId = string.Empty;
        [SerializeField] private MainTownPopulationHabitat _habitat = MainTownPopulationHabitat.Town;
        [SerializeField] private string _anchorId = string.Empty;
        [SerializeField] private string _notes = string.Empty;
        [SerializeField] private Vector3 _gizmoSize = new(6f, 2f, 6f);

        public string AreaTag => _areaTag;
        public string PrimaryPoolId => _primaryPoolId;
        public MainTownPopulationHabitat Habitat => _habitat;
        public string AnchorId => _anchorId;
        public string Notes => _notes;
        public Vector3 GizmoSize => _gizmoSize;

        private void OnValidate()
        {
            _gizmoSize = new Vector3(
                Mathf.Max(0.25f, _gizmoSize.x),
                Mathf.Max(0.25f, _gizmoSize.y),
                Mathf.Max(0.25f, _gizmoSize.z));

            if (string.IsNullOrWhiteSpace(_anchorId))
            {
                return;
            }

            var root = FindMainTownPopulationRuntimeRoot();
            if (root == null)
            {
                return;
            }

            var anchor = FindChildTransform(root, _anchorId);
            if (anchor == null || anchor == transform)
            {
                return;
            }

            transform.SetPositionAndRotation(anchor.position, anchor.rotation);
        }

        private Transform FindMainTownPopulationRuntimeRoot()
        {
            var current = transform;
            while (current != null)
            {
                if (string.Equals(current.name, "MainTownPopulationRuntime", System.StringComparison.Ordinal))
                {
                    return current;
                }

                current = current.parent;
            }

            return null;
        }

        private static Transform FindChildTransform(Transform root, string anchorId)
        {
            if (root == null || string.IsNullOrWhiteSpace(anchorId))
            {
                return null;
            }

            var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
            for (var i = 0; i < children.Length; i++)
            {
                var candidate = children[i];
                if (candidate != null && string.Equals(candidate.name, anchorId, System.StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void OnDrawGizmos()
        {
            var label = BuildLabel();
            var position = transform.position;

            Gizmos.color = ResolveFillColor(_habitat);
            Gizmos.DrawCube(position, _gizmoSize);
            Gizmos.color = ResolveOutlineColor(_habitat);
            Gizmos.DrawWireCube(position, _gizmoSize);

#if UNITY_EDITOR
            Handles.Label(position + Vector3.up * (_gizmoSize.y * 0.5f + 0.35f), label);
#endif
        }

        private string BuildLabel()
        {
            var areaTag = string.IsNullOrWhiteSpace(_areaTag) ? "area: -" : $"area: {_areaTag.Trim()}";
            var poolId = string.IsNullOrWhiteSpace(_primaryPoolId) ? "pool: -" : $"pool: {_primaryPoolId.Trim()}";
            var anchorId = string.IsNullOrWhiteSpace(_anchorId) ? "anchor: -" : $"anchor: {_anchorId.Trim()}";
            var notes = string.IsNullOrWhiteSpace(_notes) ? string.Empty : $"\nnotes: {_notes.Trim()}";
            return $"{gameObject.name}\n{areaTag} | {poolId} | {anchorId} | {_habitat}{notes}";
        }

        private static Color ResolveFillColor(MainTownPopulationHabitat habitat)
        {
            return habitat switch
            {
                MainTownPopulationHabitat.Town => new Color(0.25f, 0.85f, 1f, 0.18f),
                MainTownPopulationHabitat.Quarry => new Color(1f, 0.74f, 0.28f, 0.18f),
                MainTownPopulationHabitat.Forest => new Color(0.35f, 0.85f, 0.45f, 0.18f),
                _ => new Color(0.8f, 0.8f, 0.8f, 0.18f)
            };
        }

        private static Color ResolveOutlineColor(MainTownPopulationHabitat habitat)
        {
            var fill = ResolveFillColor(habitat);
            return new Color(fill.r, fill.g, fill.b, 1f);
        }
    }
}
