using Reloader.DevTools.Data;
using UnityEngine;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevNpcSpawnService
    {
        private readonly DevNpcSpawnCatalog _catalog;
        private readonly float _fallbackDistanceMeters;
        private readonly float _maxSpawnDistanceMeters;
        private Camera _spawnCamera;

        public DevNpcSpawnService(
            DevNpcSpawnCatalog catalog,
            Camera spawnCamera = null,
            float fallbackDistanceMeters = 3f,
            float maxSpawnDistanceMeters = 128f)
        {
            _catalog = catalog;
            _spawnCamera = spawnCamera;
            _fallbackDistanceMeters = Mathf.Max(0.1f, fallbackDistanceMeters);
            _maxSpawnDistanceMeters = Mathf.Max(0.1f, maxSpawnDistanceMeters);
        }

        public bool TrySpawn(string spawnId, out GameObject instance, out string resultMessage)
        {
            if (_catalog == null)
            {
                instance = null;
                resultMessage = "NPC spawn catalog is unavailable.";
                return false;
            }

            if (!_catalog.TryResolve(spawnId, out var entry))
            {
                instance = null;
                resultMessage = $"Unknown npc spawn id '{spawnId}'.";
                return false;
            }

            return TrySpawn(entry, out instance, out resultMessage);
        }

        public bool TrySpawn(DevNpcSpawnCatalog.Entry entry, out GameObject instance, out string resultMessage)
        {
            if (entry == null || entry.Prefab == null)
            {
                instance = null;
                resultMessage = "Spawn entry is missing a prefab.";
                return false;
            }

            if (!TryResolveSpawnPose(out var spawnPosition, out var spawnRotation))
            {
                instance = null;
                resultMessage = "Unable to resolve a camera for npc spawning.";
                return false;
            }

            instance = Object.Instantiate(entry.Prefab, spawnPosition, spawnRotation);
            instance.name = $"{entry.Prefab.name}(Clone)";
            instance.SetActive(true);
            resultMessage = $"Spawned npc '{entry.SpawnId}'.";
            return true;
        }

        public void SetCameraForTests(Camera camera)
        {
            _spawnCamera = camera;
        }

        private bool TryResolveSpawnPose(out Vector3 spawnPosition, out Quaternion spawnRotation)
        {
            var camera = ResolveSpawnCamera();
            if (camera == null)
            {
                spawnPosition = default;
                spawnRotation = Quaternion.identity;
                return false;
            }

            var forward = camera.transform.forward;
            var planarForward = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                planarForward = forward;
            }

            spawnRotation = Quaternion.LookRotation(planarForward.normalized, Vector3.up);

            Physics.SyncTransforms();
            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out var hit, _maxSpawnDistanceMeters, ~0, QueryTriggerInteraction.Ignore))
            {
                spawnPosition = hit.point;
                return true;
            }

            spawnPosition = camera.transform.position + camera.transform.forward * _fallbackDistanceMeters;
            return true;
        }

        private Camera ResolveSpawnCamera()
        {
            if (_spawnCamera != null)
            {
                return _spawnCamera;
            }

            var taggedMain = Camera.main;
            if (taggedMain != null)
            {
                _spawnCamera = taggedMain;
                return _spawnCamera;
            }

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                var candidate = cameras[i];
                if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                _spawnCamera = candidate;
                return _spawnCamera;
            }

            return null;
        }
    }
}
