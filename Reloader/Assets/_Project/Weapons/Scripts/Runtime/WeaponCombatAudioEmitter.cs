using Reloader.Audio;
using UnityEngine;

namespace Reloader.Weapons.Runtime
{
    public class WeaponCombatAudioEmitter : MonoBehaviour
    {
        [SerializeField] private CombatAudioCatalog _catalog;
        [SerializeField, Range(0f, 1f)] private float _fireVolume = 0.95f;
        [SerializeField, Range(0f, 1f)] private float _reloadVolume = 0.7f;

        public System.Action<string, AudioClip, Vector3> ClipPlayed;

        public virtual void EmitWeaponFire(string weaponId, Vector3 muzzlePosition, AudioClip overrideClip = null)
        {
            var clip = overrideClip != null ? overrideClip : _catalog != null ? _catalog.GetRandomFireClip(weaponId) : null;
            TryPlay(weaponId, clip, muzzlePosition, _fireVolume);
        }

        public virtual void EmitReloadStarted(string weaponId, Vector3 position)
        {
            var clip = _catalog != null ? _catalog.GetRandomReloadStartClip(weaponId) : null;
            TryPlay(weaponId, clip, position, _reloadVolume);
        }

        public virtual void EmitReloadCompleted(string weaponId, Vector3 position)
        {
            var clip = _catalog != null ? _catalog.GetRandomReloadCompleteClip(weaponId) : null;
            TryPlay(weaponId, clip, position, _reloadVolume);
        }

        private void TryPlay(string weaponId, AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null)
            {
                return;
            }

            ClipPlayed?.Invoke(weaponId, clip, position);
            AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
        }
    }
}
