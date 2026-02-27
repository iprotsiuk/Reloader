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
            var root = gameObject.AddComponent<PersistentPlayerRoot>();
            root.InitializeSingleton();
            return root;
        }

        private void Awake()
        {
            RegisterOrDestroyDuplicate();
        }

        private void OnEnable()
        {
            RegisterOrDestroyDuplicate();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void InitializeSingleton()
        {
            if (Instance == this)
            {
                return;
            }

            Instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void RegisterOrDestroyDuplicate()
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

            InitializeSingleton();
        }
    }
}
