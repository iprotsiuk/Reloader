using System;
using System.Collections.Generic;
using System.Linq;
using Reloader.Core.Save.Modules;
using Reloader.NPCs.Generation;
using Reloader.NPCs.World;
using UnityEngine;

namespace Reloader.NPCs.Runtime
{
    [ExecuteAlways]
    public sealed class MainTownNpcAppearanceApplicator : MonoBehaviour
    {
        private enum AppearanceSource
        {
            None,
            ContextualSeed,
            ExplicitRecord
        }

        private static readonly IReadOnlyDictionary<string, string> MaleHairObjectNames = new Dictionary<string, string>
        {
            ["hair.short"] = "Hair_Hair_Hair",
            ["hair.swept"] = "Hair_02_hair_hair",
            ["hair.wavy"] = "hair3_hair3_hair3",
            ["hair.parted"] = "hair4_hair4_hair4"
        };

        private static readonly IReadOnlyDictionary<string, string> FemaleHairObjectNames = new Dictionary<string, string>
        {
            ["hair.bob"] = "hair1",
            ["hair.long"] = "hair3"
        };

        private static readonly IReadOnlyDictionary<string, string> MaleTopObjectNames = new Dictionary<string, string>
        {
            ["tshirt1"] = "T_shirt1__T_shirt1_T_shirt1",
            ["tshirt2"] = "T_shirt2_T_shirt2_T_shirt2",
            ["jacket"] = "shirt3_shirt3_shirt3",
            ["openJacket"] = "jacket_jacket_jacket",
            ["hoody"] = "hoody_hoody_hoody"
        };

        private static readonly IReadOnlyDictionary<string, string> FemaleTopObjectNames = new Dictionary<string, string>
        {
            ["tshirt1"] = "T_shirt1",
            ["tshirt2"] = "T_shirt2",
            ["jacket"] = "shirt3",
            ["openJacket"] = "jacket",
            ["hoody"] = "hoody"
        };

        private static readonly IReadOnlyDictionary<string, string> MaleBottomObjectNames = new Dictionary<string, string>
        {
            ["brous1"] = "brous1_brous1_brous1",
            ["brous7"] = "brous7_brous7_brous7",
            ["pants1"] = "pants1_pants1_pants1"
        };

        private static readonly IReadOnlyDictionary<string, string> FemaleBottomObjectNames = new Dictionary<string, string>
        {
            ["brous1"] = "brous1_001",
            ["brous7"] = "brous7",
            ["pants1"] = "pants1"
        };

        private static readonly IReadOnlyDictionary<string, string> MaleBeardObjectNames = new Dictionary<string, string>
        {
            ["beard1"] = "beard1_beard1_beard1",
            ["beard2"] = "beard2_beard2_beard2",
            ["beard3"] = "beard3_beard3_beard3",
            ["beard4"] = "beard4_beard4_beard4",
            ["beard5"] = "beard5_beard5_beard5",
            ["beard6"] = "beard6_beard6_beard6",
            ["beard7"] = "beard7_beard7_beard7",
            ["beard8"] = "beard8_beard8_beard8",
            ["beard9"] = "beard9_beard9_beard9",
            ["beard10"] = "beard10_beard10_beard10"
        };

        [SerializeField] private string _lastContextualSeedKey = string.Empty;
        [NonSerialized] private AppearanceSource _appearanceSource = AppearanceSource.None;

        private void OnEnable()
        {
            ApplyAuthoringAppearanceFromContext();
        }

        private void OnValidate()
        {
            ApplyAuthoringAppearanceFromContext();
        }

        public void Apply(CivilianPopulationRecord record)
        {
            if (record == null)
            {
                return;
            }

            ApplyRecord(record, AppearanceSource.ExplicitRecord);
        }

        public bool ApplyAuthoringAppearanceFromContext()
        {
            if (!TryResolveAuthoringSeedKey(out var seedKey))
            {
                return false;
            }

            ApplySeededAppearance(seedKey, AppearanceSource.ContextualSeed);
            return true;
        }

        public void ApplySeededAppearance(string seedKey)
        {
            ApplySeededAppearance(seedKey, AppearanceSource.ExplicitRecord);
        }

        private void ApplySeededAppearance(string seedKey, AppearanceSource source)
        {
            if (string.IsNullOrWhiteSpace(seedKey))
            {
                return;
            }

            var record = CreateSeededRecord(seedKey);

            ApplyRecord(record, source);
            _lastContextualSeedKey = source == AppearanceSource.ContextualSeed ? seedKey : string.Empty;
        }

        private void ApplyRecord(CivilianPopulationRecord record, AppearanceSource source)
        {
            var visualRoot = ResolveVisualRoot();
            if (visualRoot == null)
            {
                return;
            }

            _appearanceSource = source;

            MainTownCuratedAppearanceRules.TryInferGender(record.BaseBodyId, record.PresentationType, out var gender);
            var maleRoot = visualRoot.Find("StyleMaleRoot");
            var femaleRoot = visualRoot.Find("StyleFemaleRoot");

            if (maleRoot != null)
            {
                maleRoot.gameObject.SetActive(gender == MainTownAppearanceGender.Male);
            }

            if (femaleRoot != null)
            {
                femaleRoot.gameObject.SetActive(gender == MainTownAppearanceGender.Female);
            }

            var activeRoot = gender == MainTownAppearanceGender.Male ? maleRoot : femaleRoot;
            if (activeRoot == null)
            {
                return;
            }

            SetAllChildrenInactive(activeRoot);
            ActivateChild(activeRoot, "root");

            if (gender == MainTownAppearanceGender.Male)
            {
                ActivateChild(activeRoot, "Man_Man_Man");
                ActivateChild(activeRoot, "Eyes_Eyes_Eyes");
                ActivateChild(activeRoot, "boots1_boots1_boots1");
                ActivateMappedChild(activeRoot, MaleHairObjectNames, record.HairId);
                if (MainTownCuratedAppearanceRules.IsMaleBeardId(record.BeardId))
                {
                    ActivateMappedChild(activeRoot, MaleBeardObjectNames, record.BeardId);
                }
            }
            else
            {
                ActivateChild(activeRoot, "woman");
                ActivateChild(activeRoot, "Eyes");
                ActivateChild(activeRoot, "boots1");
                ActivateMappedChild(activeRoot, FemaleHairObjectNames, record.HairId);
            }

            var baseTopId = MainTownCuratedAppearanceRules.IsApprovedBaseTopId(record.OutfitTopId)
                ? record.OutfitTopId
                : "tshirt1";

            var outerwearId = MainTownCuratedAppearanceRules.NormalizeOuterwearId(record.OuterwearId);
            if (string.IsNullOrWhiteSpace(outerwearId) || MainTownCuratedAppearanceRules.OuterwearRequiresBaseTop(outerwearId))
            {
                ActivateMappedChild(activeRoot, gender == MainTownAppearanceGender.Male ? MaleTopObjectNames : FemaleTopObjectNames, baseTopId);
            }

            if (MainTownCuratedAppearanceRules.IsApprovedOuterwearId(outerwearId) && !string.IsNullOrWhiteSpace(outerwearId))
            {
                ActivateMappedChild(activeRoot, gender == MainTownAppearanceGender.Male ? MaleTopObjectNames : FemaleTopObjectNames, outerwearId);
            }

            var bottomId = MainTownCuratedAppearanceRules.NormalizeBottomId(gender, record.OutfitBottomId);
            ActivateMappedChild(activeRoot, gender == MainTownAppearanceGender.Male ? MaleBottomObjectNames : FemaleBottomObjectNames, bottomId);
            ApplyMaterialVariants(activeRoot, record, gender);
        }

        private Transform ResolveVisualRoot()
        {
            var visualRoot = transform.Find("VisualRoot");
            if (visualRoot == null)
            {
                visualRoot = transform.Find("Body/VisualRoot");
            }

            return visualRoot;
        }

        private bool TryResolveAuthoringSeedKey(out string seedKey)
        {
            seedKey = string.Empty;

            if (_appearanceSource == AppearanceSource.ExplicitRecord || GetComponent<MainTownPopulationSpawnedCivilian>() != null)
            {
                return false;
            }

            var vendorTarget = GetComponent<ShopVendorTarget>();
            if (vendorTarget == null || string.IsNullOrWhiteSpace(vendorTarget.VendorId))
            {
                var agent = GetComponent<NpcAgent>();
                if (agent != null && agent.Definition != null && !string.IsNullOrWhiteSpace(agent.Definition.NpcId))
                {
                    seedKey = $"npc.{agent.Definition.NpcId.Trim()}";
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(gameObject.name))
                {
                    seedKey = $"authoring.{gameObject.name.Trim()}";
                    return true;
                }

                return false;
            }

            seedKey = $"vendor.{vendorTarget.VendorId.Trim()}";
            return true;
        }

        private static int GetDeterministicSeed(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 17;
            }

            unchecked
            {
                var hash = 17;
                foreach (var character in value)
                {
                    hash = (hash * 31) + character;
                }

                return Math.Abs(hash);
            }
        }

        private static CivilianPopulationRecord CreateSeededRecord(string seedKey)
        {
            var library = MainTownCuratedAppearanceRules.CreateDefaultLibrary();
            var gender = PickStable(new[] { MainTownAppearanceGender.Male, MainTownAppearanceGender.Female }, seedKey, "gender");
            var baseBodyId = gender == MainTownAppearanceGender.Female
                ? MainTownCuratedAppearanceRules.FemaleBodyId
                : MainTownCuratedAppearanceRules.MaleBodyId;
            var presentationType = gender == MainTownAppearanceGender.Female
                ? MainTownCuratedAppearanceRules.FemininePresentation
                : MainTownCuratedAppearanceRules.MasculinePresentation;

            return new CivilianPopulationRecord
            {
                CivilianId = $"npc.{seedKey}",
                IsAlive = true,
                IsContractEligible = false,
                IsProtectedFromContracts = true,
                BaseBodyId = baseBodyId,
                PresentationType = presentationType,
                HairId = PickStable(MainTownCuratedAppearanceRules.GetCompatibleHairIds(library, gender), seedKey, "hair"),
                HairColorId = PickStable(library.HairColorIds, seedKey, "hairColor"),
                BeardId = gender == MainTownAppearanceGender.Male
                    ? PickStable(MainTownCuratedAppearanceRules.GetCompatibleBeardIds(library, gender), seedKey, "beard")
                    : string.Empty,
                OutfitTopId = PickStable(MainTownCuratedAppearanceRules.GetCompatibleBaseTopIds(library), seedKey, "top"),
                OutfitBottomId = MainTownCuratedAppearanceRules.NormalizeBottomId(
                    gender,
                    PickStable(MainTownCuratedAppearanceRules.GetCompatibleBottomIds(library, gender), seedKey, "bottom")),
                OuterwearId = MainTownCuratedAppearanceRules.NormalizeOuterwearId(
                    PickStable(MainTownCuratedAppearanceRules.GetCompatibleOuterwearIds(library), seedKey, "outerwear")),
                MaterialColorIds = new List<string> { PickStable(library.MaterialColorIds, seedKey, "materialColor") },
                GeneratedDescriptionTags = new List<string> { seedKey },
                SpawnAnchorId = "authoring",
                AreaTag = "maintown.authoring",
                CreatedAtDay = 0,
                RetiredAtDay = -1
            };
        }

        private static T PickStable<T>(IReadOnlyList<T> values, string seedKey, string salt)
        {
            if (values == null || values.Count == 0)
            {
                return default;
            }

            var index = GetDeterministicSeed($"{seedKey}|{salt}") % values.Count;
            return values[index];
        }

        private static void SetAllChildrenInactive(Transform parent)
        {
            foreach (Transform child in parent)
            {
                child.gameObject.SetActive(false);
            }
        }

        private static void ApplyMaterialVariants(Transform activeRoot, CivilianPopulationRecord record, MainTownAppearanceGender gender)
        {
            if (activeRoot == null || record == null)
            {
                return;
            }

            var catalog = activeRoot.GetComponent<StyleMaterialVariantCatalog>();
            if (catalog == null)
            {
                return;
            }

            foreach (Transform child in activeRoot)
            {
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                var renderer = child.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                var variants = catalog.GetVariants(child.name);
                if (variants == null || variants.Count == 0)
                {
                    continue;
                }

                var seedKey = BuildMaterialSeed(record, gender, child.name);
                var selectedMaterial = variants[GetDeterministicSeed(seedKey) % variants.Count];
                if (selectedMaterial != null)
                {
                    renderer.sharedMaterial = selectedMaterial;
                }
            }
        }

        private static void ActivateMappedChild(Transform root, IReadOnlyDictionary<string, string> map, string key)
        {
            if (string.IsNullOrWhiteSpace(key) || !map.TryGetValue(key, out var childName))
            {
                return;
            }

            ActivateChild(root, childName);
        }

        private static void ActivateChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return;
            }

            var child = root.Find(childName);
            if (child != null)
            {
                child.gameObject.SetActive(true);
            }
        }

        private static string BuildMaterialSeed(CivilianPopulationRecord record, MainTownAppearanceGender gender, string childName)
        {
            var styleVariant = record.MaterialColorIds != null && record.MaterialColorIds.Count > 0
                ? record.MaterialColorIds[0] ?? string.Empty
                : string.Empty;
            var salt = UsesHairPalette(gender, childName)
                ? record.HairColorId ?? string.Empty
                : styleVariant;

            return string.Join(
                "|",
                record.CivilianId ?? string.Empty,
                gender.ToString(),
                childName ?? string.Empty,
                salt,
                record.OutfitTopId ?? string.Empty,
                record.OutfitBottomId ?? string.Empty,
                record.OuterwearId ?? string.Empty);
        }

        private static bool UsesHairPalette(MainTownAppearanceGender gender, string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
            {
                return false;
            }

            return gender == MainTownAppearanceGender.Male
                ? MaleHairObjectNames.Values.Contains(childName) || MaleBeardObjectNames.Values.Contains(childName)
                : FemaleHairObjectNames.Values.Contains(childName);
        }
    }
}
