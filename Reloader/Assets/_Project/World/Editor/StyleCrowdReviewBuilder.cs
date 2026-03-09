using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Reloader.World.Editor
{
    public static class StyleCrowdReviewBuilder
    {
        private const string DemoScenePath = "Assets/STYLE - Character Customization Kit/Scene/Demo.unity";
        private const string CrowdReviewRootName = "CrowdReviewRoot";
        private const string PlausibleBatchRootName = "PlausibleBatch";
        private const string StressBatchRootName = "StressBatch";
        private const string RecoveredBatchRootName = "RecoveredTopsBatch";
        private const string AutoRebuildSessionKey = "Reloader.StyleCrowdReviewBuilder.AutoRebuildPending";
        private const string BuildSignatureSessionKey = "Reloader.StyleCrowdReviewBuilder.BuildSignature";
        private const int ColumnsPerBatch = 10;
        private const float ColumnSpacing = 1.85f;
        private const float RowSpacing = 2.75f;
        private const float BatchOffsetX = 12.5f;
        private const float BatchOffsetZ = -6f;

        private static readonly Dictionary<string, string[]> MaterialVariantPathCache = new(StringComparer.Ordinal);
        private static bool generatedSupportMaterialsEnsured;

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
            ["coat"] = "coat_coat_coat",
            ["hoody"] = "hoody_hoody_hoody",
            ["jacket"] = "shirt3_shirt3_shirt3",
            ["openJacket"] = "jacket_jacket_jacket",
            ["shirt"] = "shirt_shirt_shirt",
            ["shirt2"] = "shirt2_shirt2_shirt2",
            ["tshirt1"] = "T_shirt1__T_shirt1_T_shirt1",
            ["tshirt2"] = "T_shirt2_T_shirt2_T_shirt2",
            ["turtleneck"] = "turtleneck_turtleneck_turtleneck"
        };

        private static readonly IReadOnlyDictionary<string, string> FemaleTopObjectNames = new Dictionary<string, string>
        {
            ["coat"] = "coat",
            ["hoody"] = "hoody",
            ["jacket"] = "shirt3",
            ["openJacket"] = "jacket",
            ["shirt"] = "shirt",
            ["shirt2"] = "shirt2",
            ["tshirt1"] = "T_shirt1",
            ["tshirt2"] = "T_shirt2",
            ["turtleneck"] = "turtleneck"
        };

        private static readonly string[] MaleOuterwearBaseLayerObjectNames =
        {
            "T_shirt1__T_shirt1_T_shirt1",
            "T_shirt2_T_shirt2_T_shirt2"
        };

        private static readonly string[] FemaleOuterwearBaseLayerObjectNames =
        {
            "T_shirt1",
            "T_shirt2"
        };

        private static readonly IReadOnlyDictionary<string, string> MaleBottomObjectNames = new Dictionary<string, string>
        {
            ["brous1"] = "brous1_brous1_brous1",
            ["brous2"] = "brous2_brous2_brous2",
            ["brous3"] = "brous3_brous3_brous3",
            ["brous4"] = "brous4_brous4_brous4",
            ["brous5"] = "brous5_brous5_brous5",
            ["brous6"] = "brous6_brous6_brous6",
            ["brous7"] = "brous7_brous7_brous7",
            ["brous8"] = "brous8_brous8_brous8",
            ["brous9"] = "brous9_brous9_brous9",
            ["brous10"] = "brous10_brous10_brous10",
            ["pants1"] = "pants1_pants1_pants1"
        };

        private static readonly IReadOnlyDictionary<string, string> FemaleBottomObjectNames = new Dictionary<string, string>
        {
            ["brous1"] = "brous1_001",
            ["brous2"] = "brous2",
            ["brous3"] = "brous3",
            ["brous4"] = "brous4",
            ["brous5"] = "brous5",
            ["brous6"] = "brous6",
            ["brous7"] = "brous7",
            ["brous8"] = "brous8",
            ["brous9"] = "brous9",
            ["brous10"] = "brous10",
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

        private static readonly IReadOnlyDictionary<string, string> MaleMaterialPaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Man_Man_Man"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Body/Materials/Man_Base Color.mat",
            ["Eyes_Eyes_Eyes"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Eyes/Materials/Eyes_Black.mat",
            ["boots1_boots1_boots1"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Boots1/2048/Materials/boots1_1_baseColor.mat",
            ["Hair_Hair_Hair"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Hair/1/2048/Materials/hair6_baseColor.mat",
            ["Hair_02_hair_hair"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Hair/2/2048/Materials/hair1_baseColor.mat",
            ["hair3_hair3_hair3"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Hair/3/2048/Materials/hair3_1_baseColor.mat",
            ["hair4_hair4_hair4"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Hair/4/2048/Materials/hair4_1_baseColor.mat",
            ["coat_coat_coat"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Coat/2048/Materials/coat1_baseColor.mat",
            ["hoody_hoody_hoody"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Hoody/2048/Materials/hoody_1_baseColor.mat",
            ["jacket_jacket_jacket"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Jacket/2048/Materials/jacket1_baseColor.mat",
            ["shirt_shirt_shirt"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Shirt/2048/Materials/shirt_1_baseColor.mat",
            ["shirt2_shirt2_shirt2"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Shirt2/2048/Materials/shirt2_1_baseColor 1.mat",
            ["shirt3_shirt3_shirt3"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Shirt3/2048/Materials/shirt3_1_baseColor.mat",
            ["T_shirt1__T_shirt1_T_shirt1"] = "Assets/STYLE - Character Customization Kit/Textures/Man/T-shirt1/2048/Materials/T-shirt1_1_baseColor.mat",
            ["T_shirt2_T_shirt2_T_shirt2"] = "Assets/STYLE - Character Customization Kit/Textures/Man/T-shirt2/2048/Materials/T-shirt2_1_baseColor.mat",
            ["turtleneck_turtleneck_turtleneck"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Turtleneck/2048/Materials/turtleneck_2_baseColor.mat",
            ["pants1_pants1_pants1"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Pants1/2048/Materials/pants1_4_baseColor.mat",
            ["brous1_brous1_brous1"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Brous/brous1/2048/Materials/brous1_1_BaseColor.mat",
            ["brous2_brous2_brous2"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Brous/brous2/2048/Materials/brous2_1_BaseColor.mat",
            ["brous3_brous3_brous3"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Pants1/2048/Materials/pants1_4_baseColor.mat",
            ["brous4_brous4_brous4"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Pants1/2048/Materials/pants1_4_baseColor.mat",
            ["brous5_brous5_brous5"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Brous/brous5/2048/Materials/brous5_1_BaseColor.mat",
            ["brous6_brous6_brous6"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Brous/brous6/2048/Materials/brous6_4_BaseColor.mat",
            ["brous7_brous7_brous7"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Pants1/2048/Materials/pants1_4_baseColor.mat",
            ["brous8_brous8_brous8"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Pants1/2048/Materials/pants1_4_baseColor.mat",
            ["brous9_brous9_brous9"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Pants1/2048/Materials/pants1_4_baseColor.mat",
            ["brous10_brous10_brous10"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Pants1/2048/Materials/pants1_4_baseColor.mat",
            ["beard1_beard1_beard1"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/1/2048/Materials/beard1_1_baseColor.mat",
            ["beard2_beard2_beard2"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/1/2048/Materials/beard1_1_baseColor.mat",
            ["beard3_beard3_beard3"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/1/2048/Materials/beard1_1_baseColor.mat",
            ["beard4_beard4_beard4"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/1/2048/Materials/beard1_1_baseColor.mat",
            ["beard5_beard5_beard5"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/1/2048/Materials/beard1_1_baseColor.mat",
            ["beard6_beard6_beard6"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/6/2048/Materials/beard6_4_baseColor.mat",
            ["beard7_beard7_beard7"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/7/2048/Materials/beard7_5_baseColor.mat",
            ["beard8_beard8_beard8"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/8/2048/Materials/beard8_1_baseColor.mat",
            ["beard9_beard9_beard9"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/1/2048/Materials/beard1_1_baseColor.mat",
            ["beard10_beard10_beard10"] = "Assets/STYLE - Character Customization Kit/Textures/Man/Beard/10/2048/Materials/beard1_1_baseColor.mat"
        };

        private static readonly IReadOnlyDictionary<string, string> FemaleMaterialPaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["woman"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/2048/Materials/woman_baseColor.mat",
            ["Eyes"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Eyes/2048/Materials/Eyes_Black.mat",
            ["boots1"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Boots1/2048/Materials/boots1_1_baseColor.mat",
            ["hair1"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Hair/2/2048/Materials/hair2_5_baseColor.mat",
            ["hair3"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Hair/3/2048/Materials/hair3_2_baseColor.mat",
            ["coat"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Jacket/2048/Materials/jacket1_baseColor.mat",
            ["hoody"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Hoody/2048/Materials/hoody_1_baseColor.mat",
            ["jacket"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Jacket/2048/Materials/jacket1_baseColor.mat",
            ["shirt"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Shirt3/2048/Materials/shirt3_1_baseColor.mat",
            ["shirt2"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Shirt3/2048/Materials/shirt3_1_baseColor.mat",
            ["shirt3"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Shirt3/2048/Materials/shirt3_1_baseColor.mat",
            ["T_shirt1"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/T-shirt1/2048/Materials/T-shirt1_2_baseColor.mat",
            ["T_shirt2"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/T-shirt2/2048/Materials/T-shirt2_1_baseColor.mat",
            ["turtleneck"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Shirt3/2048/Materials/shirt3_1_baseColor.mat",
            ["pants1"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Pants1/2048/Materials/pants1_1_baseColor.mat",
            ["brous1_001"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Pants1/2048/Materials/pants1_1_baseColor.mat",
            ["brous2"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Pants1/2048/Materials/pants1_1_baseColor.mat",
            ["brous3"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Brous/brous3/2048/Materials/brous3_1_BaseColor.mat",
            ["brous4"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Pants1/2048/Materials/pants1_1_baseColor.mat",
            ["brous5"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Pants1/2048/Materials/pants1_1_baseColor.mat",
            ["brous6"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Pants1/2048/Materials/pants1_1_baseColor.mat",
            ["brous7"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Brous/brous7/2048/Materials/brous7_5_BaseColor.mat",
            ["brous8"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Pants1/2048/Materials/pants1_1_baseColor.mat",
            ["brous9"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Pants1/2048/Materials/pants1_1_baseColor.mat",
            ["brous10"] = "Assets/STYLE - Character Customization Kit/Textures/Woman/Pants1/2048/Materials/pants1_1_baseColor.mat"
        };

        [InitializeOnLoadMethod]
        private static void ScheduleAutoRebuildWhenNeeded()
        {
            SessionState.SetBool(AutoRebuildSessionKey, true);
            EditorApplication.delayCall += TryAutoRebuildActiveDemoScene;
        }

        [MenuItem("Reloader/World/Rebuild STYLE Demo Crowd Review")]
        public static void RebuildActiveDemoScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != DemoScenePath)
            {
                Debug.LogError($"Open '{DemoScenePath}' before rebuilding the STYLE crowd review scene.");
                return;
            }

            var maleTemplate = ResolveSourceTemplate(scene, StyleCrowdReviewGender.Male);
            var femaleTemplate = ResolveSourceTemplate(scene, StyleCrowdReviewGender.Female);

            var maleSource = InstantiateClone(maleTemplate);
            var femaleSource = InstantiateClone(femaleTemplate);
            if (maleSource == null || femaleSource == null)
            {
                Debug.LogError("Could not instantiate the imported STYLE male/female rig assets.");
                return;
            }
            maleSource.hideFlags = HideFlags.HideAndDontSave;
            femaleSource.hideFlags = HideFlags.HideAndDontSave;
            maleSource.SetActive(false);
            femaleSource.SetActive(false);

            try
            {
                ClearPriorCrowd(scene);

                var crowdReviewRoot = new GameObject(CrowdReviewRootName);
                var plausibleRoot = new GameObject(PlausibleBatchRootName);
                plausibleRoot.transform.SetParent(crowdReviewRoot.transform, false);
                var stressRoot = new GameObject(StressBatchRootName);
                stressRoot.transform.SetParent(crowdReviewRoot.transform, false);
                var recoveredRoot = new GameObject(RecoveredBatchRootName);
                recoveredRoot.transform.SetParent(crowdReviewRoot.transform, false);

                var specs = StyleCrowdReviewSpecLibrary.BuildAll();
                var plausibleIndex = 0;
                var stressIndex = 0;
                var recoveredIndex = 0;

                foreach (var spec in specs)
                {
                    var parent = spec.BatchKind switch
                    {
                        StyleCrowdReviewBatchKind.Plausible => plausibleRoot.transform,
                        StyleCrowdReviewBatchKind.Stress => stressRoot.transform,
                        _ => recoveredRoot.transform
                    };
                    var source = spec.Gender == StyleCrowdReviewGender.Male ? maleSource : femaleSource;
                    var instance = InstantiateClone(source, parent);
                    if (instance == null)
                    {
                        throw new InvalidOperationException($"Could not instantiate crowd spec '{spec.Name}'.");
                    }
                    instance.hideFlags = HideFlags.None;
                    instance.SetActive(true);
                    instance.name = spec.Name;
                    var batchIndex = spec.BatchKind switch
                    {
                        StyleCrowdReviewBatchKind.Plausible => plausibleIndex++,
                        StyleCrowdReviewBatchKind.Stress => stressIndex++,
                        _ => recoveredIndex++
                    };
                    instance.transform.localPosition = GetLocalPosition(
                        batchIndex,
                        spec.BatchKind);
                    ApplyDirectChildState(instance, spec);
                    ApplyExternalMaterials(instance, spec.Gender);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                SessionState.SetString(BuildSignatureSessionKey, ComputeBuildSignature());
                SessionState.SetBool(AutoRebuildSessionKey, false);
                Debug.Log($"Rebuilt STYLE demo crowd review with {plausibleIndex} plausible NPCs, {stressIndex} stress NPCs, and {recoveredIndex} recovered-top NPCs.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(maleSource);
                UnityEngine.Object.DestroyImmediate(femaleSource);
            }
        }

        public static string GetModelAssetPath(StyleCrowdReviewGender gender)
        {
            return gender == StyleCrowdReviewGender.Male
                ? "Assets/STYLE - Character Customization Kit/Models/Man_Rig_Correct 1.fbx"
                : "Assets/STYLE - Character Customization Kit/Models/Woman_Rig_Correct.fbx";
        }

        public static GameObject ResolveSourceTemplate(Scene scene, StyleCrowdReviewGender gender)
        {
            var prefix = gender == StyleCrowdReviewGender.Male ? "Man_Rig_Correct" : "Woman_Rig_Correct";
            var sceneRoot = FindRootByPrefix(scene, prefix);
            if (sceneRoot != null)
            {
                return sceneRoot;
            }

            var modelAssetPath = GetModelAssetPath(gender);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
            if (modelAsset != null)
            {
                return modelAsset;
            }

            throw new InvalidOperationException($"Could not resolve STYLE source template for {gender} at '{modelAssetPath}'.");
        }

        public static GameObject InstantiateClone(GameObject source, Transform parent = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var prefabClone = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            if (prefabClone != null)
            {
                return prefabClone;
            }

            return parent == null
                ? UnityEngine.Object.Instantiate(source)
                : UnityEngine.Object.Instantiate(source, parent);
        }

        public static Vector3 GetLocalPosition(int index, StyleCrowdReviewBatchKind batchKind)
        {
            var column = index % ColumnsPerBatch;
            var row = index / ColumnsPerBatch;
            var xBase = batchKind switch
            {
                StyleCrowdReviewBatchKind.Plausible => 0f,
                StyleCrowdReviewBatchKind.Stress => BatchOffsetX * 2f,
                _ => BatchOffsetX * 4f
            };
            return new Vector3(
                xBase + (column * ColumnSpacing),
                0f,
                row * RowSpacing);
        }

        public static IReadOnlyCollection<string> BuildActiveChildNames(StyleCrowdReviewSpec spec)
        {
            if (spec == null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            var activeNames = new HashSet<string>(StringComparer.Ordinal)
            {
                "root"
            };

            if (spec.Gender == StyleCrowdReviewGender.Male)
            {
                activeNames.Add("Man_Man_Man");
                activeNames.Add("Eyes_Eyes_Eyes");
                activeNames.Add("boots1_boots1_boots1");
                activeNames.Add(GetMappedName(MaleHairObjectNames, spec.HairId, nameof(spec.HairId), spec.Name));
                AddTopLayerNames(activeNames, spec);
                activeNames.Add(NormalizeChildName(spec.Gender, GetMappedName(MaleBottomObjectNames, spec.BottomId, nameof(spec.BottomId), spec.Name)));
                if (!string.IsNullOrWhiteSpace(spec.BeardId))
                {
                    activeNames.Add(GetMappedName(MaleBeardObjectNames, spec.BeardId, nameof(spec.BeardId), spec.Name));
                }
            }
            else
            {
                activeNames.Add("woman");
                activeNames.Add("Eyes");
                activeNames.Add("boots1");
                activeNames.Add(GetMappedName(FemaleHairObjectNames, spec.HairId, nameof(spec.HairId), spec.Name));
                AddTopLayerNames(activeNames, spec);
                activeNames.Add(NormalizeChildName(spec.Gender, GetMappedName(FemaleBottomObjectNames, spec.BottomId, nameof(spec.BottomId), spec.Name)));
            }

            return activeNames;
        }

        public static IReadOnlyCollection<string> GetDirectChildNamesToActivate(StyleCrowdReviewSpec spec)
        {
            return BuildActiveChildNames(spec);
        }

        public static string GetExternalMaterialPath(StyleCrowdReviewGender gender, string childName)
        {
            return GetExternalMaterialPath(gender, childName, string.Empty);
        }

        public static string GetExternalMaterialPath(StyleCrowdReviewGender gender, string childName, string variationKey)
        {
            var materialPaths = GetExternalMaterialPaths(gender, childName);
            if (materialPaths.Count == 0)
            {
                return null;
            }

            if (materialPaths.Count == 1 || string.IsNullOrWhiteSpace(variationKey))
            {
                return materialPaths[0];
            }

            return materialPaths[GetStableIndex(variationKey, materialPaths.Count)];
        }

        public static IReadOnlyList<string> GetExternalMaterialPaths(StyleCrowdReviewGender gender, string childName)
        {
            if (string.IsNullOrWhiteSpace(childName) || childName == "root")
            {
                return Array.Empty<string>();
            }

            EnsureGeneratedSupportMaterials();

            var normalizedName = NormalizeChildName(gender, childName);
            var map = gender == StyleCrowdReviewGender.Male ? MaleMaterialPaths : FemaleMaterialPaths;
            if (!map.TryGetValue(normalizedName, out var canonicalPath) || string.IsNullOrWhiteSpace(canonicalPath))
            {
                return Array.Empty<string>();
            }

            if (MaterialVariantPathCache.TryGetValue(canonicalPath, out var cachedPaths))
            {
                return cachedPaths;
            }

            var directory = Path.GetDirectoryName(canonicalPath)?.Replace('\\', '/');
            var stem = Path.GetFileNameWithoutExtension(canonicalPath);
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(stem))
            {
                var fallbackPaths = new[] { canonicalPath };
                MaterialVariantPathCache[canonicalPath] = fallbackPaths;
                return fallbackPaths;
            }

            var variantFamilyStem = GetMaterialVariantFamilyStem(stem);
            var siblingPaths = AssetDatabase.FindAssets("t:Material", new[] { directory })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(Path.GetDirectoryName(path)?.Replace('\\', '/'), directory, StringComparison.Ordinal))
                .Where(path =>
                {
                    var candidateStem = Path.GetFileNameWithoutExtension(path);
                    return string.Equals(candidateStem, stem, StringComparison.Ordinal)
                        || candidateStem.StartsWith(stem + " ", StringComparison.Ordinal)
                        || string.Equals(GetMaterialVariantFamilyStem(candidateStem), variantFamilyStem, StringComparison.Ordinal);
                })
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            var resolvedPaths = siblingPaths.Length > 0 ? siblingPaths : new[] { canonicalPath };
            MaterialVariantPathCache[canonicalPath] = resolvedPaths;
            return resolvedPaths;
        }

        private static void EnsureGeneratedSupportMaterials()
        {
            if (generatedSupportMaterialsEnsured)
            {
                return;
            }

            var changed = false;
            changed |= EnsureGeneratedMaterialVariants(
                "Assets/STYLE - Character Customization Kit/Textures/Man/Hoody/2048/Materials",
                "Assets/STYLE - Character Customization Kit/Textures/Man/Hoody/2048",
                new[] { "hoody_1_baseColor", "hoody_2_baseColor", "hoody_3_baseColor", "hoody_4_baseColor" },
                "hoody_normal",
                "hoody_metallic",
                "hoody_ao");
            changed |= EnsureGeneratedMaterialVariants(
                "Assets/STYLE - Character Customization Kit/Textures/Man/Jacket/2048/Materials",
                "Assets/STYLE - Character Customization Kit/Textures/Man/Jacket/2048",
                new[] { "jacket1_baseColor", "jacket2_baseColor", "jacket3_baseColor", "jacket4_baseColor" },
                "jacket_normal",
                "jacket_metallic",
                "jacket_ao");
            changed |= EnsureGeneratedMaterialVariants(
                "Assets/STYLE - Character Customization Kit/Textures/Woman/Hair/3/2048/Materials",
                "Assets/STYLE - Character Customization Kit/Textures/Woman/Hair/3/2048",
                new[]
                {
                    "hair3_1_baseColor",
                    "hair3_2_baseColor",
                    "hair3_3_baseColor",
                    "hair3_4_baseColor",
                    "hair3_5_baseColor",
                    "hair3_6_baseColor"
                },
                "hair3_normal.png",
                string.Empty,
                "hair3_occlusion.jpg");

            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                MaterialVariantPathCache.Clear();
            }

            generatedSupportMaterialsEnsured = true;
        }

        private static string GetMaterialVariantFamilyStem(string stem)
        {
            if (string.IsNullOrWhiteSpace(stem))
            {
                return string.Empty;
            }

            const string suffix = "_baseColor";
            if (!stem.EndsWith(suffix, StringComparison.Ordinal))
            {
                return stem;
            }

            var prefix = stem.Substring(0, stem.Length - suffix.Length);
            var trimmedPrefix = TrimTrailingVariantOrdinal(prefix);
            return trimmedPrefix + suffix;
        }

        private static string TrimTrailingVariantOrdinal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var end = value.Length;
            while (end > 0 && char.IsDigit(value[end - 1]))
            {
                end--;
            }

            if (end > 0 && end < value.Length && (value[end - 1] == '_' || value[end - 1] == ' '))
            {
                end--;
            }

            return value.Substring(0, end);
        }

        private static bool EnsureGeneratedMaterialVariants(
            string materialDirectory,
            string textureDirectory,
            IReadOnlyList<string> baseColorStems,
            string normalStem,
            string metallicStem,
            string occlusionStem)
        {
            var changed = false;
            foreach (var baseColorStem in baseColorStems)
            {
                changed |= EnsureGeneratedMaterial(
                    $"{materialDirectory}/{baseColorStem}.mat",
                    $"{textureDirectory}/{baseColorStem}.jpg",
                    BuildTextureAssetPath(textureDirectory, normalStem, ".jpg"),
                    BuildTextureAssetPath(textureDirectory, metallicStem, ".jpg"),
                    BuildTextureAssetPath(textureDirectory, occlusionStem, ".jpg"));
            }

            return changed;
        }

        private static string BuildTextureAssetPath(string textureDirectory, string textureStem, string defaultExtension)
        {
            if (string.IsNullOrWhiteSpace(textureStem))
            {
                return string.Empty;
            }

            if (textureStem.Contains(".", StringComparison.Ordinal))
            {
                return $"{textureDirectory}/{textureStem}";
            }

            return $"{textureDirectory}/{textureStem}{defaultExtension}";
        }

        private static bool EnsureGeneratedMaterial(
            string materialAssetPath,
            string baseColorTexturePath,
            string normalTexturePath,
            string metallicTexturePath,
            string occlusionTexturePath)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath) != null)
            {
                return false;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                return false;
            }

            var baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorTexturePath);
            if (baseColor == null)
            {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(GetAbsoluteAssetPath(materialAssetPath)) ?? string.Empty);

            var material = new Material(shader);
            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", Color.white);

            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalTexturePath);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicTexturePath);
            if (metallic != null)
            {
                material.SetTexture("_MetallicGlossMap", metallic);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            var occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(occlusionTexturePath);
            if (occlusion != null)
            {
                material.SetTexture("_OcclusionMap", occlusion);
                material.SetFloat("_OcclusionStrength", 1f);
            }

            AssetDatabase.CreateAsset(material, materialAssetPath);
            return true;
        }

        private static string GetAbsoluteAssetPath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        public static string NormalizeChildName(StyleCrowdReviewGender gender, string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
            {
                return childName;
            }

            if (gender == StyleCrowdReviewGender.Male)
            {
                return childName switch
                {
                    "brous2_brous2_brous2" => "pants1_pants1_pants1",
                    "brous3_brous3_brous3" => "pants1_pants1_pants1",
                    "brous4_brous4_brous4" => "pants1_pants1_pants1",
                    "brous5_brous5_brous5" => "pants1_pants1_pants1",
                    "brous7_brous7_brous7" => "pants1_pants1_pants1",
                    "brous8_brous8_brous8" => "pants1_pants1_pants1",
                    "brous9_brous9_brous9" => "pants1_pants1_pants1",
                    "brous10_brous10_brous10" => "pants1_pants1_pants1",
                    _ => childName
                };
            }

            return childName switch
            {
                "brous1_001" => "pants1",
                "brous2" => "pants1",
                "brous4" => "pants1",
                "brous5" => "pants1",
                "brous6" => "pants1",
                "brous8" => "pants1",
                "brous9" => "pants1",
                "brous10" => "pants1",
                _ => childName
            };
        }

        private static void ApplyDirectChildState(GameObject root, StyleCrowdReviewSpec spec)
        {
            var activeNames = BuildActiveChildNames(spec);
            foreach (Transform child in root.transform)
            {
                child.gameObject.SetActive(activeNames.Contains(child.name));
            }
        }

        private static void ApplyExternalMaterials(GameObject root, StyleCrowdReviewGender gender)
        {
            NormalizeUnsupportedChildren(root, gender);
            EnsureBaseLayerCoverage(root, gender);

            foreach (Transform child in root.transform)
            {
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                var materialPath = GetExternalMaterialPath(gender, child.name, $"{root.name}|{child.name}");
                if (string.IsNullOrWhiteSpace(materialPath))
                {
                    continue;
                }

                var renderer = child.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    continue;
                }

                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void NormalizeUnsupportedChildren(GameObject root, StyleCrowdReviewGender gender)
        {
            foreach (Transform child in root.transform)
            {
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                var normalizedName = NormalizeChildName(gender, child.name);
                if (normalizedName == child.name)
                {
                    continue;
                }

                var replacement = root.transform.Find(normalizedName);
                if (replacement == null)
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                replacement.gameObject.SetActive(true);
            }
        }

        private static void AddTopLayerNames(HashSet<string> activeNames, StyleCrowdReviewSpec spec)
        {
            if (spec.Gender == StyleCrowdReviewGender.Male)
            {
                var outerName = GetMappedName(MaleTopObjectNames, spec.TopId, nameof(spec.TopId), spec.Name);
                activeNames.Add(outerName);

                if (TopRequiresBaseLayer(spec.TopId))
                {
                    activeNames.Add(GetOuterwearBaseLayerName(spec));
                }

                return;
            }

            var femaleTopName = GetMappedName(FemaleTopObjectNames, spec.TopId, nameof(spec.TopId), spec.Name);
            activeNames.Add(femaleTopName);

            if (TopRequiresBaseLayer(spec.TopId))
            {
                activeNames.Add(GetOuterwearBaseLayerName(spec));
            }
        }

        private static void EnsureBaseLayerCoverage(GameObject root, StyleCrowdReviewGender gender)
        {
            if (gender == StyleCrowdReviewGender.Male)
            {
                EnsureAnyChildActiveWhenOuterwearPresent(root.transform, MaleOuterwearBaseLayerObjectNames, "jacket_jacket_jacket", "coat_coat_coat", "shirt3_shirt3_shirt3");
                return;
            }

            EnsureAnyChildActiveWhenOuterwearPresent(root.transform, FemaleOuterwearBaseLayerObjectNames, "jacket", "coat", "shirt3");
        }

        private static bool TopRequiresBaseLayer(string topId)
        {
            return topId is "jacket" or "openJacket" or "coat";
        }

        private static string GetOuterwearBaseLayerName(StyleCrowdReviewSpec spec)
        {
            var candidates = spec.Gender == StyleCrowdReviewGender.Male
                ? MaleOuterwearBaseLayerObjectNames
                : FemaleOuterwearBaseLayerObjectNames;

            var seed = 17;
            seed = (seed * 31) + GetDeterministicStringHash(spec.Name);
            seed = (seed * 31) + GetDeterministicStringHash(spec.Archetype);
            seed = (seed * 31) + GetDeterministicStringHash(spec.HairId);
            seed = (seed * 31) + GetDeterministicStringHash(spec.BottomId);

            var index = Math.Abs(seed % candidates.Length);
            return candidates[index];
        }

        private static int GetDeterministicStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                var hash = 23;
                foreach (var character in value)
                {
                    hash = (hash * 31) + character;
                }

                return hash;
            }
        }

        private static void EnsureAnyChildActiveWhenOuterwearPresent(Transform root, IReadOnlyList<string> baseLayerNames, params string[] outerwearNames)
        {
            var hasOuterwear = outerwearNames.Any(name =>
            {
                var child = root.Find(name);
                return child != null && child.gameObject.activeSelf;
            });
            if (!hasOuterwear)
            {
                return;
            }

            var activeBaseLayer = baseLayerNames
                .Select(root.Find)
                .FirstOrDefault(child => child != null && child.gameObject.activeSelf);
            if (activeBaseLayer != null)
            {
                return;
            }

            foreach (var baseLayerName in baseLayerNames)
            {
                var baseLayer = root.Find(baseLayerName);
                if (baseLayer == null)
                {
                    continue;
                }

                baseLayer.gameObject.SetActive(true);
                return;
            }
        }

        private static int GetStableIndex(string key, int count)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in key)
                {
                    hash ^= character;
                    hash *= 16777619;
                }

                return (int)(hash % count);
            }
        }

        private static string ComputeBuildSignature()
        {
            return string.Join(
                "\n",
                StyleCrowdReviewSpecLibrary.BuildAll()
                    .Select(spec =>
                        $"{spec.Name}|{spec.Archetype}|{spec.BatchKind}|{spec.Gender}|{spec.HairId}|{spec.BeardId}|{spec.TopId}|{spec.BottomId}"));
        }

        private static GameObject FindRootByPrefix(Scene scene, string prefix)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static void TryAutoRebuildActiveDemoScene()
        {
            if (!SessionState.GetBool(AutoRebuildSessionKey, true))
            {
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != DemoScenePath)
            {
                return;
            }

            var roots = scene.GetRootGameObjects();
            var crowdRoot = roots.FirstOrDefault(root => root != null && root.name == CrowdReviewRootName);
            var hasVendorLineup = roots.Any(root =>
                root != null
                && (root.name.StartsWith("Man_Rig_Correct", StringComparison.Ordinal)
                    || root.name.StartsWith("Woman_Rig_Correct", StringComparison.Ordinal)));
            var crowdNeedsPopulation = crowdRoot == null || crowdRoot.transform.Cast<Transform>().All(batchRoot => batchRoot.childCount == 0);
            var signatureChanged = crowdRoot != null
                && !string.Equals(
                    SessionState.GetString(BuildSignatureSessionKey, string.Empty),
                    ComputeBuildSignature(),
                    StringComparison.Ordinal);

            if (!hasVendorLineup && !crowdNeedsPopulation && !signatureChanged)
            {
                ApplyExternalMaterialsToCrowd(crowdRoot);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                SessionState.SetBool(AutoRebuildSessionKey, false);
                return;
            }

            RebuildActiveDemoScene();
        }

        private static void ClearPriorCrowd(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                if (root.name == CrowdReviewRootName
                    || root.name.StartsWith("Man_Rig_Correct", StringComparison.Ordinal)
                    || root.name.StartsWith("Woman_Rig_Correct", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static string GetMappedName(
            IReadOnlyDictionary<string, string> map,
            string id,
            string fieldName,
            string specName)
        {
            if (!map.TryGetValue(id, out var mapped))
            {
                throw new ArgumentException($"Spec '{specName}' uses unsupported {fieldName} '{id}'.");
            }

            return mapped;
        }

        private static void ApplyExternalMaterialsToCrowd(GameObject crowdRoot)
        {
            if (crowdRoot == null)
            {
                return;
            }

            foreach (Transform batchRoot in crowdRoot.transform)
            {
                foreach (Transform npcRoot in batchRoot)
                {
                    ApplyExternalMaterials(npcRoot.gameObject, InferGender(npcRoot.gameObject));
                }
            }
        }

        private static StyleCrowdReviewGender InferGender(GameObject root)
        {
            foreach (Transform child in root.transform)
            {
                if (child.name == "Man_Man_Man")
                {
                    return StyleCrowdReviewGender.Male;
                }

                if (child.name == "woman")
                {
                    return StyleCrowdReviewGender.Female;
                }
            }

            return root.name.IndexOf("Woman", StringComparison.OrdinalIgnoreCase) >= 0
                ? StyleCrowdReviewGender.Female
                : StyleCrowdReviewGender.Male;
        }
    }
}
