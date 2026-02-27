using UnityEngine;

namespace Reloader.World.Runtime
{
    public sealed class BootstrapWorldRoot : MonoBehaviour
    {
        private void Awake()
        {
            Initialize();
        }

        public static PersistentPlayerRoot Initialize()
        {
            return PersistentPlayerRoot.EnsureInstance();
        }
    }
}
