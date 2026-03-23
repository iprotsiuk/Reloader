using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Runtime
{
    public sealed class PersistentPlayerRoot : MonoBehaviour
    {
        public static PersistentPlayerRoot Instance { get; private set; }
        public Transform PlayerRootTransform => _playerRootTransform;

        [SerializeField] private Transform _playerRootTransform;

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

        public Transform RegisterRuntimePlayerRoot(Transform playerRootTransform)
        {
            if (playerRootTransform == null)
            {
                return _playerRootTransform;
            }

            if (_playerRootTransform != null && _playerRootTransform != playerRootTransform)
            {
                DestroyGameObject(playerRootTransform.gameObject);
                return _playerRootTransform;
            }

            _playerRootTransform = playerRootTransform;

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(_playerRootTransform.gameObject);
            }

            return _playerRootTransform;
        }

        public Transform MoveRuntimePlayerRootToScene(Scene scene)
        {
            if (_playerRootTransform == null)
            {
                return null;
            }

            MoveRuntimeOwnerToScene(scene);

            if (!scene.IsValid() || !scene.isLoaded || _playerRootTransform.gameObject.scene == scene)
            {
                return _playerRootTransform;
            }

            SceneManager.MoveGameObjectToScene(_playerRootTransform.gameObject, scene);
            return _playerRootTransform;
        }

        private void MoveRuntimeOwnerToScene(Scene scene)
        {
            if (Application.isPlaying || !scene.IsValid() || !scene.isLoaded || gameObject.scene == scene)
            {
                return;
            }

            SceneManager.MoveGameObjectToScene(gameObject, scene);
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
                DestroyGameObject(gameObject);
                return;
            }

            InitializeSingleton();
        }

        private static void DestroyGameObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }
    }
}
