using System.Collections.Generic;
using UnityEngine;

namespace Reloader.World.Contracts
{
    [CreateAssetMenu(fileName = "WorldSceneContract", menuName = "Reloader/World/Scene Contract")]
    public sealed class WorldSceneContract : ScriptableObject
    {
        [SerializeField] private string _scenePath = string.Empty;
        [SerializeField] private WorldSceneRole _sceneRole = WorldSceneRole.TownHub;
        [SerializeField] private List<string> _requiredObjectPaths = new();
        [SerializeField] private List<WorldRequiredComponentContract> _requiredComponentContracts = new();
        [SerializeField] private bool _validateRequiredSceneEntryPointIds = true;
        [SerializeField] private List<string> _requiredSceneEntryPointIds = new();

        public string ScenePath
        {
            get => _scenePath;
            set => _scenePath = value ?? string.Empty;
        }

        public WorldSceneRole SceneRole
        {
            get => _sceneRole;
            set => _sceneRole = value;
        }

        public List<string> RequiredObjectPaths => _requiredObjectPaths;
        public List<WorldRequiredComponentContract> RequiredComponentContracts => _requiredComponentContracts;

        public bool ValidateRequiredSceneEntryPointIds
        {
            get => _validateRequiredSceneEntryPointIds;
            set => _validateRequiredSceneEntryPointIds = value;
        }

        public List<string> RequiredSceneEntryPointIds => _requiredSceneEntryPointIds;
    }
}
