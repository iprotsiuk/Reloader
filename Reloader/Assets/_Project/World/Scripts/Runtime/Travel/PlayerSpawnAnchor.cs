using System;
using UnityEngine;

namespace Reloader.World.Travel
{
    [DisallowMultipleComponent]
    public sealed class PlayerSpawnAnchor : MonoBehaviour
    {
        [SerializeField] private string _anchorId = string.Empty;
        [SerializeField] private PlayerSpawnAnchorKind _anchorKind = PlayerSpawnAnchorKind.Spawn;

        public string AnchorId => _anchorId;
        public PlayerSpawnAnchorKind AnchorKind => _anchorKind;

        public void Configure(string anchorId, PlayerSpawnAnchorKind anchorKind)
        {
            _anchorId = string.IsNullOrWhiteSpace(anchorId) ? Guid.NewGuid().ToString("N") : anchorId.Trim();
            _anchorKind = anchorKind;
        }

        private void Reset()
        {
            if (string.IsNullOrWhiteSpace(_anchorId))
            {
                _anchorId = Guid.NewGuid().ToString("N");
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_anchorId))
            {
                _anchorId = Guid.NewGuid().ToString("N");
                return;
            }

            _anchorId = _anchorId.Trim();
        }
    }
}
