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

        public Transform CaptureOrAdoptPlayerRootForScene(Scene scene, bool preferSceneRoot = false)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return _playerRootTransform;
            }

            var scenePlayerRoot = FindPlayerRootInScene(scene);
            if (preferSceneRoot && scenePlayerRoot != null && scenePlayerRoot != _playerRootTransform)
            {
                if (_playerRootTransform != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(_playerRootTransform.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(_playerRootTransform.gameObject);
                    }
                }

                _playerRootTransform = scenePlayerRoot;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(_playerRootTransform.gameObject);
                }

                return _playerRootTransform;
            }

            if (_playerRootTransform == null)
            {
                _playerRootTransform = scenePlayerRoot;
                if (_playerRootTransform == null)
                {
                    return null;
                }

                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(_playerRootTransform.gameObject);
                }

                return _playerRootTransform;
            }

            if (_playerRootTransform.gameObject.scene == scene)
            {
                return _playerRootTransform;
            }

            if (scenePlayerRoot != null && scenePlayerRoot != _playerRootTransform)
            {
                if (Application.isPlaying)
                {
                    Destroy(scenePlayerRoot.gameObject);
                }
                else
                {
                    DestroyImmediate(scenePlayerRoot.gameObject);
                }
            }

            SceneManager.MoveGameObjectToScene(_playerRootTransform.gameObject, scene);
            return _playerRootTransform;
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

        private static Transform FindPlayerRootInScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root != null && root.name == "PlayerRoot")
                {
                    return root.transform;
                }
            }

            return null;
        }
    }
}
