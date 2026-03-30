using System;
using System.Collections.Generic;
using System.Linq;
using Reloader.NPCs.Generation;
using Reloader.NPCs.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.NPCs.Editor
{
    public static class MainTownPlaygroundMarkerSync
    {
        private const string MenuItemPath = "Reloader/NPCs/Sync MainTown Playground Markers In Active Scene";
        private const string RuntimeRootName = "MainTownPopulationRuntime";
        private const string PlaygroundAreaTagPrefix = "maintown.playground.";
        private const string UndoLabel = "Sync MainTown Playground Markers";

        [MenuItem(MenuItemPath)]
        public static void SyncActiveScene()
        {
            var runtimeRoot = FindRuntimeRootInActiveScene();
            if (runtimeRoot == null)
            {
                Debug.LogError($"MainTownPlaygroundMarkerSync: active scene is missing '{RuntimeRootName}'.");
                return;
            }

            var definition = ResolvePopulationDefinition(runtimeRoot);
            if (definition == null)
            {
                Debug.LogError($"MainTownPlaygroundMarkerSync: '{RuntimeRootName}' is missing an assigned population definition.");
                return;
            }

            var changes = SyncMarkers(definition, runtimeRoot);
            if (changes == 0)
            {
                Debug.Log("MainTownPlaygroundMarkerSync: active scene markers already match the authored playground slots.");
                return;
            }

            Debug.Log($"MainTownPlaygroundMarkerSync: synchronized {changes} playground marker change(s) in '{runtimeRoot.scene.name}'.");
        }

        public static int SyncMarkers(MainTownPopulationDefinition definition, GameObject runtimeRoot)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (runtimeRoot == null)
            {
                throw new ArgumentNullException(nameof(runtimeRoot));
            }

            definition.Validate();

            var authoredSlots = definition.GetSlotsForHabitat(MainTownPopulationHabitat.Any)
                .Where(static slot => slot != null && IsPlaygroundAreaTag(slot.AreaTag))
                .ToDictionary(static slot => slot.AreaTag.Trim(), static slot => slot, StringComparer.Ordinal);

            var markers = runtimeRoot.GetComponentsInChildren<MainTownPlaygroundZoneMarker>(true);
            var markersByAreaTag = new Dictionary<string, MainTownPlaygroundZoneMarker>(StringComparer.Ordinal);
            var staleMarkers = new List<MainTownPlaygroundZoneMarker>();

            for (var i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                if (marker == null)
                {
                    continue;
                }

                var areaTag = (marker.AreaTag ?? string.Empty).Trim();
                if (!authoredSlots.ContainsKey(areaTag))
                {
                    staleMarkers.Add(marker);
                    continue;
                }

                if (!markersByAreaTag.TryAdd(areaTag, marker))
                {
                    staleMarkers.Add(marker);
                }
            }

            var changes = 0;
            for (var i = 0; i < staleMarkers.Count; i++)
            {
                Undo.DestroyObjectImmediate(staleMarkers[i].gameObject);
                changes++;
            }

            foreach (var pair in authoredSlots.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                var areaTag = pair.Key;
                var slot = pair.Value;
                var marker = GetOrCreateMarker(runtimeRoot.transform, areaTag, markersByAreaTag, ref changes);
                if (marker == null)
                {
                    continue;
                }

                changes += ApplySlot(marker, slot, runtimeRoot.transform);
            }

            if (changes > 0)
            {
                EditorSceneManager.MarkSceneDirty(runtimeRoot.scene);
            }

            return changes;
        }

        private static MainTownPopulationDefinition ResolvePopulationDefinition(GameObject runtimeRoot)
        {
            if (runtimeRoot == null)
            {
                return null;
            }

            var bridge = runtimeRoot.GetComponent<CivilianPopulationRuntimeBridge>();
            return bridge?.PopulationDefinition;
        }

        private static MainTownPlaygroundZoneMarker GetOrCreateMarker(
            Transform runtimeRoot,
            string areaTag,
            IDictionary<string, MainTownPlaygroundZoneMarker> markersByAreaTag,
            ref int changes)
        {
            if (markersByAreaTag.TryGetValue(areaTag, out var existing) && existing != null)
            {
                return existing;
            }

            var markerObject = new GameObject(BuildMarkerName(areaTag));
            Undo.RegisterCreatedObjectUndo(markerObject, UndoLabel);
            Undo.SetTransformParent(markerObject.transform, runtimeRoot, UndoLabel);
            markerObject.transform.localPosition = Vector3.zero;
            markerObject.transform.localRotation = Quaternion.identity;

            var marker = Undo.AddComponent<MainTownPlaygroundZoneMarker>(markerObject);
            markersByAreaTag[areaTag] = marker;
            changes++;
            return marker;
        }

        private static int ApplySlot(MainTownPlaygroundZoneMarker marker, MainTownPopulationSlotDefinition slot, Transform runtimeRoot)
        {
            var changes = 0;
            Undo.RecordObject(marker, UndoLabel);
            Undo.RecordObject(marker.gameObject, UndoLabel);
            Undo.RecordObject(marker.transform, UndoLabel);

            if (marker.transform.parent != runtimeRoot)
            {
                Undo.SetTransformParent(marker.transform, runtimeRoot, UndoLabel);
                changes++;
            }

            var expectedName = BuildMarkerName(slot.AreaTag);
            if (!string.Equals(marker.gameObject.name, expectedName, StringComparison.Ordinal))
            {
                marker.gameObject.name = expectedName;
                changes++;
            }

            var serializedObject = new SerializedObject(marker);
            changes += SetString(serializedObject.FindProperty("_areaTag"), slot.AreaTag);
            changes += SetString(serializedObject.FindProperty("_primaryPoolId"), slot.PoolId);
            changes += SetEnum(serializedObject.FindProperty("_habitat"), (int)slot.Habitat);
            changes += SetString(serializedObject.FindProperty("_anchorId"), slot.SpawnAnchorId);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            var anchor = FindChildTransform(runtimeRoot, slot.SpawnAnchorId);
            if (anchor != null)
            {
                if (Vector3.Distance(marker.transform.position, anchor.position) > 0.0001f)
                {
                    marker.transform.position = anchor.position;
                    changes++;
                }

                if (Quaternion.Angle(marker.transform.rotation, anchor.rotation) > 0.0001f)
                {
                    marker.transform.rotation = anchor.rotation;
                    changes++;
                }
            }
            else
            {
                Debug.LogWarning($"MainTownPlaygroundMarkerSync: anchor '{slot.SpawnAnchorId}' not found under '{runtimeRoot.name}' for areaTag '{slot.AreaTag}'.", runtimeRoot);
            }

            return changes;
        }

        private static int SetString(SerializedProperty property, string expectedValue)
        {
            if (property == null)
            {
                return 0;
            }

            var normalized = expectedValue ?? string.Empty;
            if (string.Equals(property.stringValue, normalized, StringComparison.Ordinal))
            {
                return 0;
            }

            property.stringValue = normalized;
            return 1;
        }

        private static int SetEnum(SerializedProperty property, int expectedValue)
        {
            if (property == null || property.intValue == expectedValue)
            {
                return 0;
            }

            property.intValue = expectedValue;
            return 1;
        }

        private static GameObject FindRuntimeRootInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                return null;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, RuntimeRootName, StringComparison.Ordinal))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static Transform FindChildTransform(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            var children = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                var candidate = children[i];
                if (candidate != null && string.Equals(candidate.name, childName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool IsPlaygroundAreaTag(string areaTag)
        {
            return !string.IsNullOrWhiteSpace(areaTag) &&
                   areaTag.Trim().StartsWith(PlaygroundAreaTagPrefix, StringComparison.Ordinal);
        }

        private static string BuildMarkerName(string areaTag)
        {
            var trimmedAreaTag = areaTag?.Trim() ?? string.Empty;
            var suffix = string.IsNullOrWhiteSpace(trimmedAreaTag)
                ? "Marker"
                : trimmedAreaTag[Math.Min(trimmedAreaTag.Length, PlaygroundAreaTagPrefix.Length)..];

            var tokens = suffix
                .Split(new[] { '.', '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(CapitalizeToken);

            var normalizedSuffix = string.Concat(tokens);
            if (string.IsNullOrWhiteSpace(normalizedSuffix))
            {
                normalizedSuffix = "Marker";
            }

            return $"PlaygroundZoneMarker_{normalizedSuffix}";
        }

        private static string CapitalizeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            var trimmed = token.Trim();
            if (trimmed.Length == 1)
            {
                return trimmed.ToUpperInvariant();
            }

            return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
        }
    }
}
