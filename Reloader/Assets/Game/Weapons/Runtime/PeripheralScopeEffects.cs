using UnityEngine;

namespace Reloader.Game.Weapons
{
    internal interface IPeripheralScopeBlurReceiver
    {
        void SetPeripheralBlur(float blurPercent);
    }

    public sealed class PeripheralScopeEffects : MonoBehaviour
    {
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

            if (_scopedBehaviours == null)
            {
                return;
            }

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

                behaviour.enabled = isActive;
            }
        }
    }
}
