using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.Weapons.Animations
{
    /// <summary>
    /// Receives legacy animation events embedded in imported FPS arms clips.
    /// Runtime reload/holster flow is already driven by PackWeaponRuntimeDriver timing,
    /// so these handlers intentionally remain no-op compatibility endpoints.
    /// </summary>
    public sealed class PlayerArmsAnimationEventReceiver : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHooks()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureReceiversOnLoadedScene()
        {
            EnsureReceiversInScene();
        }

        public static PlayerArmsAnimationEventReceiver EnsureReceiver(Animator animator)
        {
            if (animator == null)
            {
                return null;
            }

            var target = animator.gameObject;
            if (!target.TryGetComponent<PlayerArmsAnimationEventReceiver>(out var receiver))
            {
                receiver = target.AddComponent<PlayerArmsAnimationEventReceiver>();
            }

            return receiver;
        }

        private static void HandleSceneLoaded(Scene _, LoadSceneMode __)
        {
            EnsureReceiversInScene();
        }

        private static void EnsureReceiversInScene()
        {
            var animators = Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null || animator.gameObject.name != "PlayerArmsVisual")
                {
                    continue;
                }

                EnsureReceiver(animator);
            }
        }

        public void OnAnimationEndedHolster()
        {
        }

        public void OnAmmunitionFill()
        {
        }

        public void OnAnimationEndedReload()
        {
        }

        public void OnEjectCasing()
        {
        }
    }
}
