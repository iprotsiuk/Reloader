using UnityEngine;

namespace Reloader.Player.Viewmodel
{
    public readonly struct ViewmodelBindingResolution
    {
        public ViewmodelBindingResolution(bool isValid, int errorsCount, int warningsCount)
        {
            IsValid = isValid;
            ErrorsCount = errorsCount;
            WarningsCount = warningsCount;
        }

        public bool IsValid { get; }
        public int ErrorsCount { get; }
        public int WarningsCount { get; }
    }

    public static class ViewmodelBindingResolver
    {
        private static readonly string[] RequiredBindPoints =
        {
            "Muzzle",
            "RightHandGrip",
            "LeftHandIKTarget",
            "AimReference"
        };

        private static readonly string[] OptionalBindPoints =
        {
            "EjectPort",
            "MagazineAttach"
        };

        public static ViewmodelBindingResolution Resolve(Transform root)
        {
            if (root == null)
            {
                return new ViewmodelBindingResolution(false, RequiredBindPoints.Length, OptionalBindPoints.Length);
            }

            var errors = 0;
            var warnings = 0;
            for (var i = 0; i < RequiredBindPoints.Length; i++)
            {
                if (FindByName(root, RequiredBindPoints[i]) == null)
                {
                    errors++;
                }
            }

            for (var i = 0; i < OptionalBindPoints.Length; i++)
            {
                if (FindByName(root, OptionalBindPoints[i]) == null)
                {
                    warnings++;
                }
            }

            return new ViewmodelBindingResolution(errors == 0, errors, warnings);
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindByName(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
