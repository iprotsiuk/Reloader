using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Reloader.NPCs.Combat
{
    [CreateAssetMenu(fileName = "BloodVfxCatalog", menuName = "Reloader/NPCs/Blood VFX Catalog")]
    public sealed class BloodVfxCatalog : ScriptableObject
    {
        public const string DefaultCatalogAssetPath = "Assets/_Project/NPCs/Data/BloodVfxCatalog.asset";
        public const string ProjectOwnedPrefabRoot = "Assets/_Project/NPCs/Prefabs/VFX/";

        [Header("Project-Owned Required Defaults")]
        [SerializeField] private GameObject _lightImpactDefaultPrefab;
        [SerializeField] private GameObject _heavyImpactDefaultPrefab;
        [SerializeField] private GameObject _neckImpactDefaultPrefab;
        [SerializeField] private GameObject _deathPuddleDefaultPrefab;

        [Header("Optional Overrides")]
        [SerializeField] private GameObject _lightImpactOverridePrefab;
        [SerializeField] private GameObject _heavyImpactOverridePrefab;
        [SerializeField] private GameObject _neckImpactOverridePrefab;
        [SerializeField] private GameObject _deathPuddleOverridePrefab;

        public bool TryGetPrefab(BloodEffectKind kind, out GameObject prefab)
        {
            prefab = GetOptionalOverridePrefab(kind);
            if (prefab != null)
            {
                return true;
            }

            prefab = GetRequiredDefaultPrefab(kind);
            return prefab != null;
        }

        public GameObject GetRequiredDefaultPrefab(BloodEffectKind kind)
        {
            switch (kind)
            {
                case BloodEffectKind.LightImpact:
                    return _lightImpactDefaultPrefab;
                case BloodEffectKind.HeavyImpact:
                    return _heavyImpactDefaultPrefab;
                case BloodEffectKind.NeckImpact:
                    return _neckImpactDefaultPrefab;
                case BloodEffectKind.DeathPuddle:
                    return _deathPuddleDefaultPrefab;
                default:
                    return null;
            }
        }

        public bool ValidateRequiredDefaults(out string error)
        {
            var errors = new List<string>();
            ValidateRequiredDefault(BloodEffectKind.LightImpact, _lightImpactDefaultPrefab, errors);
            ValidateRequiredDefault(BloodEffectKind.HeavyImpact, _heavyImpactDefaultPrefab, errors);
            ValidateRequiredDefault(BloodEffectKind.NeckImpact, _neckImpactDefaultPrefab, errors);
            ValidateRequiredDefault(BloodEffectKind.DeathPuddle, _deathPuddleDefaultPrefab, errors);

            if (errors.Count == 0)
            {
                error = string.Empty;
                return true;
            }

            error = string.Join("; ", errors);
            return false;
        }

        public void ConfigureRequiredDefaultsForTests(
            GameObject lightImpactDefaultPrefab,
            GameObject heavyImpactDefaultPrefab,
            GameObject neckImpactDefaultPrefab,
            GameObject deathPuddleDefaultPrefab)
        {
            _lightImpactDefaultPrefab = lightImpactDefaultPrefab;
            _heavyImpactDefaultPrefab = heavyImpactDefaultPrefab;
            _neckImpactDefaultPrefab = neckImpactDefaultPrefab;
            _deathPuddleDefaultPrefab = deathPuddleDefaultPrefab;
        }

        private GameObject GetOptionalOverridePrefab(BloodEffectKind kind)
        {
            switch (kind)
            {
                case BloodEffectKind.LightImpact:
                    return _lightImpactOverridePrefab;
                case BloodEffectKind.HeavyImpact:
                    return _heavyImpactOverridePrefab;
                case BloodEffectKind.NeckImpact:
                    return _neckImpactOverridePrefab;
                case BloodEffectKind.DeathPuddle:
                    return _deathPuddleOverridePrefab;
                default:
                    return null;
            }
        }

        private static void ValidateRequiredDefault(BloodEffectKind kind, GameObject prefab, ICollection<string> errors)
        {
            if (prefab == null)
            {
                errors.Add($"{kind} missing required project-owned default prefab");
                return;
            }

#if UNITY_EDITOR
            var path = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!path.StartsWith(ProjectOwnedPrefabRoot, System.StringComparison.Ordinal))
            {
                errors.Add($"{kind} required default prefab must live under {ProjectOwnedPrefabRoot}");
            }
#endif
        }
    }
}
