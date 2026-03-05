using Reloader.Audio;
using UnityEngine;

namespace Reloader.Weapons.Runtime
{
    public class WeaponCombatAudioEmitter : MonoBehaviour
    {
        private const string OneShotSourceNodeName = "WeaponAudioOneShotSource";

        [SerializeField] private CombatAudioCatalog _catalog;
        [SerializeField] private AudioSource _oneShotSource;
        [SerializeField, Range(0f, 1f)] private float _fireVolume = 0.95f;
        [SerializeField, Range(0f, 1f)] private float _reloadVolume = 0.7f;

        public System.Action<string, AudioClip, Vector3> ClipPlayed;

        public virtual void EmitWeaponFire(string weaponId, Vector3 muzzlePosition, AudioClip overrideClip = null)
        {
            var catalog = CombatAudioCatalogResolver.Resolve(_catalog);
            _catalog = catalog;
            var clip = overrideClip != null
                ? overrideClip
                : catalog != null ? catalog.GetStableFireClip(weaponId) : null;
            TryPlay(weaponId, clip, muzzlePosition, _fireVolume);
        }

        public virtual void EmitReloadStarted(string weaponId, Vector3 position)
        {
            var catalog = CombatAudioCatalogResolver.Resolve(_catalog);
            _catalog = catalog;
            var clip = catalog != null ? catalog.GetRandomReloadStartClip(weaponId) : null;
            TryPlay(weaponId, clip, position, _reloadVolume);
        }

        public virtual void EmitReloadCompleted(string weaponId, Vector3 position)
        {
            var catalog = CombatAudioCatalogResolver.Resolve(_catalog);
            _catalog = catalog;
            var clip = catalog != null ? catalog.GetRandomReloadCompleteClip(weaponId) : null;
            TryPlay(weaponId, clip, position, _reloadVolume);
        }

        public void SetCatalog(CombatAudioCatalog catalog)
        {
            _catalog = catalog;
        }

        public void EnsureCatalog(CombatAudioCatalog fallbackCatalog)
        {
            if (_catalog == null)
            {
                _catalog = CombatAudioCatalogResolver.Resolve(fallbackCatalog);
            }
        }

        private void Awake()
        {
            EnsureOneShotSource();
        }

        private void TryPlay(string weaponId, AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null)
            {
                return;
            }

            ClipPlayed?.Invoke(weaponId, clip, position);
            var source = EnsureOneShotSource();
            if (source == null)
            {
                return;
            }

            source.transform.position = position;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        private AudioSource EnsureOneShotSource()
        {
            _oneShotSource = ResolveSafeOneShotSource(_oneShotSource);

            _oneShotSource.playOnAwake = false;
            _oneShotSource.spatialBlend = 1f;
            _oneShotSource.rolloffMode = AudioRolloffMode.Logarithmic;
            return _oneShotSource;
        }

        private AudioSource ResolveSafeOneShotSource(AudioSource current)
        {
            if (current != null && current.transform != transform)
            {
                return current;
            }

            if (current == null && TryGetComponent<AudioSource>(out var attachedOnHost) && attachedOnHost != null && attachedOnHost.transform != transform)
            {
                return attachedOnHost;
            }

            return GetOrCreateChildSource();
        }

        private AudioSource GetOrCreateChildSource()
        {
            var child = transform.Find(OneShotSourceNodeName);
            if (child == null)
            {
                var childGo = new GameObject(OneShotSourceNodeName);
                child = childGo.transform;
                child.SetParent(transform, false);
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
            }

            var source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = child.gameObject.AddComponent<AudioSource>();
            }

            return source;
        }
    }
}
