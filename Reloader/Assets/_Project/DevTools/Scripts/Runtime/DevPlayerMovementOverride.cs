using Reloader.Player;
using UnityEngine;

namespace Reloader.DevTools.Runtime
{
    public sealed class DevPlayerMovementOverride
    {
        public bool IsNoclipEnabled { get; private set; }
        public float NoclipSpeed { get; private set; }
        public PlayerMover PlayerMover { get; set; }

        public void SetNoclipEnabled(bool isEnabled)
        {
            IsNoclipEnabled = isEnabled;
            Apply();
        }

        public void ToggleNoclip()
        {
            IsNoclipEnabled = !IsNoclipEnabled;
            Apply();
        }

        public void SetNoclipSpeed(float speed)
        {
            NoclipSpeed = Mathf.Max(0.1f, speed);
            Apply();
        }

        public PlayerMover ResolvePlayerMover()
        {
            if (PlayerMover != null)
            {
                return PlayerMover;
            }

            PlayerMover = Object.FindFirstObjectByType<PlayerMover>(FindObjectsInactive.Include);
            return PlayerMover;
        }

        private void Apply()
        {
            var mover = ResolvePlayerMover();
            if (mover == null)
            {
                return;
            }

            mover.SetDevNoclip(IsNoclipEnabled, NoclipSpeed);
        }
    }
}
