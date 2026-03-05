using System.Collections.Generic;
using UnityEngine;

namespace Reloader.Audio
{
    [CreateAssetMenu(fileName = "CombatAudioCatalog", menuName = "Reloader/Audio/Combat Audio Catalog")]
    public sealed class CombatAudioCatalog : ScriptableObject
    {
        [System.Serializable]
        public sealed class WeaponShotGroup
        {
            [SerializeField] private string _weaponId = string.Empty;
            [SerializeField] private AudioClip[] _fireClips = System.Array.Empty<AudioClip>();
            [SerializeField] private AudioClip[] _reloadStartClips = System.Array.Empty<AudioClip>();
            [SerializeField] private AudioClip[] _reloadCompleteClips = System.Array.Empty<AudioClip>();

            public string WeaponId => _weaponId;
            public AudioClip[] FireClips => _fireClips;
            public AudioClip[] ReloadStartClips => _reloadStartClips;
            public AudioClip[] ReloadCompleteClips => _reloadCompleteClips;
        }

        [System.Serializable]
        public sealed class SurfaceAudioGroup
        {
            [SerializeField] private string _surfaceId = "Default";
            [SerializeField] private AudioClip[] _clips = System.Array.Empty<AudioClip>();

            public string SurfaceId => _surfaceId;
            public AudioClip[] Clips => _clips;
        }

        [Header("Weapons")]
        [SerializeField] private AudioClip[] _defaultGunshotClips = System.Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _defaultReloadStartClips = System.Array.Empty<AudioClip>();
        [SerializeField] private AudioClip[] _defaultReloadCompleteClips = System.Array.Empty<AudioClip>();
        [SerializeField] private WeaponShotGroup[] _weaponShotGroups = System.Array.Empty<WeaponShotGroup>();

        [Header("Impacts")]
        [SerializeField] private SurfaceAudioGroup[] _impactGroups = System.Array.Empty<SurfaceAudioGroup>();

        [Header("Footsteps")]
        [SerializeField] private SurfaceAudioGroup[] _footstepGroups = System.Array.Empty<SurfaceAudioGroup>();

        public AudioClip GetRandomFireClip(string weaponId)
        {
            return GetStableClip($"fire::{weaponId}", ResolveWeaponGroup(weaponId)?.FireClips, _defaultGunshotClips);
        }

        public AudioClip GetStableFireClip(string weaponId)
        {
            return GetStableClip($"fire::{weaponId}", ResolveWeaponGroup(weaponId)?.FireClips, _defaultGunshotClips);
        }

        public AudioClip GetRandomReloadStartClip(string weaponId)
        {
            return GetStableClip($"reload_start::{weaponId}", ResolveWeaponGroup(weaponId)?.ReloadStartClips, _defaultReloadStartClips);
        }

        public AudioClip GetRandomReloadCompleteClip(string weaponId)
        {
            return GetStableClip($"reload_end::{weaponId}", ResolveWeaponGroup(weaponId)?.ReloadCompleteClips, _defaultReloadCompleteClips);
        }

        public AudioClip GetRandomImpactClip(string surfaceId)
        {
            return GetStableClip($"impact::{surfaceId}", ResolveSurfaceGroup(_impactGroups, surfaceId)?.Clips, null);
        }

        public AudioClip GetRandomFootstepClip(string surfaceId)
        {
            return GetStableClip($"footstep::{surfaceId}", ResolveSurfaceGroup(_footstepGroups, surfaceId)?.Clips, null);
        }

        private WeaponShotGroup ResolveWeaponGroup(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId) || _weaponShotGroups == null)
            {
                return null;
            }

            for (var i = 0; i < _weaponShotGroups.Length; i++)
            {
                var group = _weaponShotGroups[i];
                if (group != null && string.Equals(group.WeaponId, weaponId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return group;
                }
            }

            return null;
        }

        private static SurfaceAudioGroup ResolveSurfaceGroup(IReadOnlyList<SurfaceAudioGroup> groups, string surfaceId)
        {
            if (groups == null || groups.Count == 0)
            {
                return null;
            }

            var resolvedSurfaceId = string.IsNullOrWhiteSpace(surfaceId) ? "Default" : surfaceId;
            SurfaceAudioGroup fallback = null;
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group == null)
                {
                    continue;
                }

                if (string.Equals(group.SurfaceId, resolvedSurfaceId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return group;
                }

                if (fallback == null && string.Equals(group.SurfaceId, "Default", System.StringComparison.OrdinalIgnoreCase))
                {
                    fallback = group;
                }
            }

            return fallback;
        }

        private static AudioClip GetStableClip(string channelKey, AudioClip[] primary, AudioClip[] fallback)
        {
            var source = BuildValidClipList(primary, fallback);
            if (source.Count == 0)
            {
                return null;
            }

            var resolvedKey = string.IsNullOrWhiteSpace(channelKey) ? "default" : channelKey;
            var index = Mathf.Abs(resolvedKey.GetHashCode()) % source.Count;
            return source[index];
        }

        private static List<AudioClip> BuildValidClipList(AudioClip[] primary, AudioClip[] fallback)
        {
            var source = HasEntries(primary) ? primary : fallback;
            var valid = new List<AudioClip>();
            if (!HasEntries(source))
            {
                return valid;
            }

            for (var i = 0; i < source.Length; i++)
            {
                if (source[i] != null)
                {
                    valid.Add(source[i]);
                }
            }

            return valid;
        }

        private static bool HasEntries(AudioClip[] clips)
        {
            return clips != null && clips.Length > 0;
        }
    }
}
