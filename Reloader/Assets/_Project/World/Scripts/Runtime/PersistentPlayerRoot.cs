using UnityEngine;

namespace Reloader.World.Runtime
{
    public sealed class PersistentPlayerRoot : MonoBehaviour
    {
        public static PersistentPlayerRoot Instance { get; private set; }

        public static PersistentPlayerRoot EnsureInstance()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var gameObject = new GameObject(nameof(PersistentPlayerRoot));
            return gameObject.AddComponent<PersistentPlayerRoot>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }

                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
