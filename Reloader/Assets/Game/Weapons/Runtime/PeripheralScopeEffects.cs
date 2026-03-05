using UnityEngine;

namespace Reloader.Game.Weapons
{
    public sealed class PeripheralScopeEffects : MonoBehaviour
    {
        [SerializeField] private Behaviour[] _scopedBehaviours;

        public bool IsActive { get; private set; }
        public float CurrentAlpha { get; private set; }

        public void SetState(bool isActive, float alpha)
        {
            IsActive = isActive;
            CurrentAlpha = Mathf.Clamp01(alpha);

            if (_scopedBehaviours == null)
            {
                return;
            }

            for (var i = 0; i < _scopedBehaviours.Length; i++)
            {
                if (_scopedBehaviours[i] != null)
                {
                    _scopedBehaviours[i].enabled = isActive;
                }
            }
        }
    }
}
