using UnityEngine;

namespace Reloader.Player.Viewmodel
{
    public readonly struct AnimationContractValidationResult
    {
        public AnimationContractValidationResult(int errorsCount, int warningsCount, int infosCount)
        {
            ErrorsCount = errorsCount;
            WarningsCount = warningsCount;
            InfosCount = infosCount;
        }

        public int ErrorsCount { get; }
        public int WarningsCount { get; }
        public int InfosCount { get; }
        public bool IsValid => ErrorsCount == 0;
    }

    public static class AnimationContractValidator
    {
        public static AnimationContractValidationResult ValidateBindings(Transform root, int contractMajorVersion, int expectedMajorVersion)
        {
            var binding = ViewmodelBindingResolver.Resolve(root);
            var errors = binding.ErrorsCount;
            var warnings = binding.WarningsCount;
            if (contractMajorVersion != expectedMajorVersion)
            {
                errors++;
            }

            return new AnimationContractValidationResult(errors, warnings, 0);
        }
    }
}
