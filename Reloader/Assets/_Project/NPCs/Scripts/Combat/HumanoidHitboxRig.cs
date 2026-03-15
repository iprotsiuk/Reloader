using System.Collections.Generic;
using UnityEngine;

namespace Reloader.NPCs.Combat
{
    [DisallowMultipleComponent]
    public sealed class HumanoidHitboxRig : MonoBehaviour
    {
        [SerializeField] private Transform _head;
        [SerializeField] private Transform _neck;
        [SerializeField] private Transform _torso;
        [SerializeField] private Transform _pelvis;
        [SerializeField] private Transform _armL;
        [SerializeField] private Transform _armR;
        [SerializeField] private Transform _legL;
        [SerializeField] private Transform _legR;
        [SerializeField] private bool _autoResolveOnAwake = true;

        private readonly Dictionary<HumanoidBodyZone, Transform> _boneByZone = new Dictionary<HumanoidBodyZone, Transform>();
        private readonly Dictionary<Transform, HumanoidBodyZone> _zoneByBone = new Dictionary<Transform, HumanoidBodyZone>();
        private readonly Dictionary<HumanoidBodyZone, BodyZoneHitbox> _hitboxByZone = new Dictionary<HumanoidBodyZone, BodyZoneHitbox>();

        public bool IsRigResolved { get; private set; }
        public bool HasAllStandardBones => ResolveBoneCount() == 8;

        private void Reset()
        {
            ResolveBones();
        }

        private void Awake()
        {
            if (_autoResolveOnAwake)
            {
                ResolveBones();
                return;
            }

            RebuildLookups();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (_autoResolveOnAwake)
            {
                ResolveBones();
                return;
            }

            RebuildLookups();
        }

        public bool ResolveBones()
        {
            AutoResolveMissingBones();
            RebuildLookups();
            IsRigResolved = HasAllStandardBones;
            return IsRigResolved;
        }

        public bool ValidateRig()
        {
            RebuildLookups();
            IsRigResolved = HasAllStandardBones;
            return IsRigResolved;
        }

        public bool TryResolveBone(HumanoidBodyZone zone, out Transform bone)
        {
            return _boneByZone.TryGetValue(zone, out bone) && bone != null;
        }

        public Transform GetBoneOrNull(HumanoidBodyZone zone)
        {
            return _boneByZone.TryGetValue(zone, out var bone) ? bone : null;
        }

        public bool TryResolveZone(Transform candidate, out HumanoidBodyZone zone)
        {
            zone = HumanoidBodyZone.Torso;
            if (candidate == null)
            {
                return false;
            }

            var current = candidate;
            while (current != null)
            {
                if (_zoneByBone.TryGetValue(current, out zone))
                {
                    return true;
                }

                if (ReferenceEquals(current, transform))
                {
                    break;
                }

                current = current.parent;
            }

            return false;
        }

        public HumanoidBodyZone ResolveZoneOrDefault(GameObject hitObject, HumanoidBodyZone fallbackZone = HumanoidBodyZone.Torso)
        {
            if (hitObject == null)
            {
                return fallbackZone;
            }

            if (hitObject.TryGetComponent<BodyZoneHitbox>(out var directHitbox))
            {
                return directHitbox.BodyZone;
            }

            if (TryResolveZone(hitObject.transform, out var zone))
            {
                return zone;
            }

            return fallbackZone;
        }

        public bool TryGetHitbox(HumanoidBodyZone zone, out BodyZoneHitbox hitbox)
        {
            return _hitboxByZone.TryGetValue(zone, out hitbox) && hitbox != null;
        }

        internal bool TryGetDamageReceiver(out HumanoidDamageReceiver receiver)
        {
            receiver = GetComponent<HumanoidDamageReceiver>();
            return receiver != null;
        }

        internal void RegisterHitbox(BodyZoneHitbox hitbox)
        {
            if (hitbox == null)
            {
                return;
            }

            _hitboxByZone[hitbox.BodyZone] = hitbox;
        }

        internal void UnregisterHitbox(BodyZoneHitbox hitbox)
        {
            if (hitbox == null)
            {
                return;
            }

            if (_hitboxByZone.TryGetValue(hitbox.BodyZone, out var registered) && ReferenceEquals(registered, hitbox))
            {
                _hitboxByZone.Remove(hitbox.BodyZone);
            }
        }

        internal void UnregisterHitbox(BodyZoneHitbox hitbox, HumanoidBodyZone zone)
        {
            if (hitbox == null)
            {
                return;
            }

            if (_hitboxByZone.TryGetValue(zone, out var registered) && ReferenceEquals(registered, hitbox))
            {
                _hitboxByZone.Remove(zone);
            }
        }

        private int ResolveBoneCount()
        {
            var count = 0;
            if (_boneByZone.TryGetValue(HumanoidBodyZone.Head, out var head) && head != null) count++;
            if (_boneByZone.TryGetValue(HumanoidBodyZone.Neck, out var neck) && neck != null) count++;
            if (_boneByZone.TryGetValue(HumanoidBodyZone.Torso, out var torso) && torso != null) count++;
            if (_boneByZone.TryGetValue(HumanoidBodyZone.Pelvis, out var pelvis) && pelvis != null) count++;
            if (_boneByZone.TryGetValue(HumanoidBodyZone.ArmL, out var armL) && armL != null) count++;
            if (_boneByZone.TryGetValue(HumanoidBodyZone.ArmR, out var armR) && armR != null) count++;
            if (_boneByZone.TryGetValue(HumanoidBodyZone.LegL, out var legL) && legL != null) count++;
            if (_boneByZone.TryGetValue(HumanoidBodyZone.LegR, out var legR) && legR != null) count++;
            return count;
        }

        private void RebuildLookups()
        {
            _boneByZone.Clear();
            _zoneByBone.Clear();

            RegisterZone(HumanoidBodyZone.Head, _head);
            RegisterZone(HumanoidBodyZone.Neck, _neck);
            RegisterZone(HumanoidBodyZone.Torso, _torso);
            RegisterZone(HumanoidBodyZone.Pelvis, _pelvis);
            RegisterZone(HumanoidBodyZone.ArmL, _armL);
            RegisterZone(HumanoidBodyZone.ArmR, _armR);
            RegisterZone(HumanoidBodyZone.LegL, _legL);
            RegisterZone(HumanoidBodyZone.LegR, _legR);
        }

        private void RegisterZone(HumanoidBodyZone zone, Transform bone)
        {
            if (bone == null)
            {
                return;
            }

            _boneByZone[zone] = bone;
            if (!_zoneByBone.ContainsKey(bone))
            {
                _zoneByBone.Add(bone, zone);
            }
        }

        private void AutoResolveMissingBones()
        {
            var animator = GetComponentInChildren<Animator>(includeInactive: true);
            if (animator != null)
            {
                _head ??= ResolveAnimatorBone(animator, HumanBodyBones.Head);
                _neck ??= ResolveAnimatorBone(animator, HumanBodyBones.Neck);
                _torso ??= ResolveAnimatorBone(animator, HumanBodyBones.Chest, HumanBodyBones.UpperChest, HumanBodyBones.Spine);
                _pelvis ??= ResolveAnimatorBone(animator, HumanBodyBones.Hips);
                _armL ??= ResolveAnimatorBone(animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
                _armR ??= ResolveAnimatorBone(animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
                _legL ??= ResolveAnimatorBone(animator, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
                _legR ??= ResolveAnimatorBone(animator, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
            }

            var allTransforms = GetComponentsInChildren<Transform>(includeInactive: true);
            _head ??= FindByNameTokens(allTransforms, "head");
            _neck ??= FindByNameTokens(allTransforms, "neck");
            _torso ??= FindByNameTokens(allTransforms, "chest", "spine", "torso");
            _pelvis ??= FindByNameTokens(allTransforms, "hips", "pelvis");
            _armL ??= FindByNameTokens(allTransforms, "leftarm", "leftupperarm", "l_upperarm", "upperarm_l", "arm_l");
            _armR ??= FindByNameTokens(allTransforms, "rightarm", "rightupperarm", "r_upperarm", "upperarm_r", "arm_r");
            _legL ??= FindByNameTokens(allTransforms, "leftleg", "leftupperleg", "l_thigh", "upleg_l", "leg_l");
            _legR ??= FindByNameTokens(allTransforms, "rightleg", "rightupperleg", "r_thigh", "upleg_r", "leg_r");

            if (_neck == null && _head != null)
            {
                _neck = _head.parent;
            }

            if (_torso == null && _neck != null)
            {
                _torso = _neck.parent;
            }

            if (_pelvis == null && _torso != null)
            {
                _pelvis = _torso.parent;
            }
        }

        private static Transform ResolveAnimatorBone(Animator animator, params HumanBodyBones[] candidates)
        {
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman || candidates == null)
            {
                return null;
            }

            for (var i = 0; i < candidates.Length; i++)
            {
                var bone = candidates[i];
                if (bone == HumanBodyBones.LastBone)
                {
                    continue;
                }

                var candidate = animator.GetBoneTransform(bone);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform FindByNameTokens(Transform[] transforms, params string[] tokens)
        {
            if (transforms == null || tokens == null || tokens.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < transforms.Length; i++)
            {
                var candidate = transforms[i];
                if (candidate == null)
                {
                    continue;
                }

                var name = candidate.name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var normalizedName = name.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
                for (var tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
                {
                    var token = tokens[tokenIndex];
                    if (string.IsNullOrWhiteSpace(token))
                    {
                        continue;
                    }

                    if (normalizedName.Contains(NormalizeLookupToken(token)))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static string NormalizeLookupToken(string token)
        {
            return string.IsNullOrWhiteSpace(token)
                ? string.Empty
                : token.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
