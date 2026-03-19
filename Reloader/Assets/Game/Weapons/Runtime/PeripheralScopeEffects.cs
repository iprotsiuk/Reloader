using UnityEngine;
using Reloader.Game.Weapons.Rendering;

namespace Reloader.Game.Weapons
{
    internal interface IPeripheralScopeBlurReceiver
    {
        void SetPeripheralBlur(float blurPercent);
    }

    internal interface IPeripheralScopeBlurRuntimeStateSource
    {
        void UpdateBlurRuntimeState(bool isActive, float alpha, float blurPercent);
    }

    public sealed class PeripheralScopeEffects : MonoBehaviour
    {
        private const float DefaultCenterWidthNormalized = 0.3f;
        private const float DefaultCenterHeightNormalized = 0.3f;
        private const float DefaultMinCenterNormalizedScale = 0.55f;
        private const float DefaultSoftEdgeNormalized = 0.04f;

        [SerializeField] private Behaviour[] _scopedBehaviours;

        public bool IsActive { get; private set; }
        public float CurrentAlpha { get; private set; }
        public float CurrentPeripheralBlur { get; private set; }

        public void SetState(bool isActive, float alpha)
        {
            SetState(isActive, alpha, 0f);
        }

        public void SetState(bool isActive, float alpha, float peripheralBlurPercent)
        {
            IsActive = isActive;
            CurrentAlpha = Mathf.Clamp01(alpha);
            CurrentPeripheralBlur = Mathf.Clamp01(peripheralBlurPercent);

            var updatedBlurRuntimeState = false;
            if (_scopedBehaviours != null)
            {
                for (var i = 0; i < _scopedBehaviours.Length; i++)
                {
                    var behaviour = _scopedBehaviours[i];
                    if (behaviour == null)
                    {
                        continue;
                    }

                    if (behaviour is IPeripheralScopeEffectReceiver receiver)
                    {
                        receiver.SetScopedState(isActive, CurrentAlpha);
                    }

                    if (behaviour is IPeripheralScopeBlurReceiver blurReceiver)
                    {
                        blurReceiver.SetPeripheralBlur(CurrentPeripheralBlur);
                    }

                    if (behaviour is IPeripheralScopeBlurRuntimeStateSource blurRuntimeStateSource)
                    {
                        blurRuntimeStateSource.UpdateBlurRuntimeState(isActive, CurrentAlpha, CurrentPeripheralBlur);
                        updatedBlurRuntimeState = true;
                    }

                    behaviour.enabled = isActive;
                }
            }

            if (!updatedBlurRuntimeState)
            {
                if (TryUpdateSiblingMaskRuntimeState(isActive))
                {
                    return;
                }

                UpdateDefaultBlurRuntimeState(isActive);
            }
        }

        private bool TryUpdateSiblingMaskRuntimeState(bool isActive)
        {
            var mask = GetComponent<PeripheralScopeScreenMask>();
            if (mask == null)
            {
                return false;
            }

            mask.SetScopedState(isActive, CurrentAlpha);
            mask.SetPeripheralBlur(CurrentPeripheralBlur);
            mask.UpdateBlurRuntimeState(isActive, CurrentAlpha, CurrentPeripheralBlur);
            return true;
        }

        private void UpdateDefaultBlurRuntimeState(bool isActive)
        {
            var effectiveActive = isActive && CurrentAlpha > 0.001f && CurrentPeripheralBlur > 0.001f;
            if (!effectiveActive)
            {
                PeripheralScopeBlurRuntimeState.Reset();
                return;
            }

            var blurScale = Mathf.Lerp(1f, DefaultMinCenterNormalizedScale, CurrentPeripheralBlur);
            var centerWidthNormalized = Mathf.Clamp01(DefaultCenterWidthNormalized * blurScale);
            var centerHeightNormalized = Mathf.Clamp01(DefaultCenterHeightNormalized * blurScale);

            PeripheralScopeBlurRuntimeState.Update(
                true,
                CurrentAlpha,
                CurrentPeripheralBlur,
                centerWidthNormalized,
                centerHeightNormalized,
                DefaultSoftEdgeNormalized);
        }
    }
}
