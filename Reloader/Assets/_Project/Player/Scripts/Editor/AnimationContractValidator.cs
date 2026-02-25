using Reloader.Player.Viewmodel;
using UnityEditor;
using UnityEngine;

namespace Reloader.Player.Editor
{
    public static class AnimationContractValidatorMenu
    {
        [MenuItem("Reloader/Player/Validate Weapon Animation Contract In Active Scene")]
        public static void ValidateInActiveScene()
        {
            var selected = Selection.activeTransform;
            if (selected == null)
            {
                Debug.LogWarning("Select a weapon/viewmodel root transform before validation.");
                return;
            }

            var result = AnimationContractValidator.ValidateBindings(selected, contractMajorVersion: 1, expectedMajorVersion: 1);
            var level = result.IsValid ? "PASS" : "FAIL";
            Debug.Log($"Animation contract validation {level}: errors={result.ErrorsCount}, warnings={result.WarningsCount}, infos={result.InfosCount}", selected);
        }
    }
}
