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
                if (!IsPlayerArmsAnimatorHost(animator))
                {
                    continue;
                }

                EnsureReceiver(animator);
            }
        }

        private static bool IsPlayerArmsAnimatorHost(Animator animator)
        {
            if (animator == null || animator.gameObject == null)
            {
                return false;
            }

            var name = animator.gameObject.name;
            if (string.Equals(name, "PlayerArmsVisual")
                || string.Equals(name, "PlayerArms"))
            {
                return true;
            }

            return name.IndexOf("PlayerArms", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void OnAnimationEndedHolster()
        {
        }

        public void OnAmmunitionFill()
        {
            SendMessageUpwards("OnAmmunitionFillForwarded", SendMessageOptions.DontRequireReceiver);
        }

        public void OnAnimationEndedReload()
        {
            SendMessageUpwards("OnAnimationEndedReloadForwarded", SendMessageOptions.DontRequireReceiver);
        }

        public void OnEjectCasing()
        {
        }
    }
}
