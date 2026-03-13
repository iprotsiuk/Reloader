using System;
using System.Collections.Generic;
using Reloader.Core.Save.Modules;
using SystemRandom = System.Random;

namespace Reloader.NPCs.Generation
{
    public sealed class CivilianAppearanceGenerator
    {
        private const string NoBeardId = "beard.none";

        public CivilianPopulationRecord GenerateRecord(
            CivilianAppearanceLibrary library,
            string civilianId,
            int createdAtDay,
            string spawnAnchorId,
            int seed,
            bool isContractEligible)
        {
            return GenerateRecord(
                library,
                civilianId,
                createdAtDay,
                spawnAnchorId,
                seed,
                isContractEligible,
                populationSlotId: string.Empty,
                poolId: string.Empty,
                areaTag: string.Empty,
                isProtectedFromContracts: false,
                reservedPublicDisplayNames: null);
        }

        public CivilianPopulationRecord GenerateRecord(
            CivilianAppearanceLibrary library,
            string civilianId,
            int createdAtDay,
            string spawnAnchorId,
            int seed,
            bool isContractEligible,
            string populationSlotId,
            string poolId,
            string areaTag,
            bool isProtectedFromContracts,
            ISet<string> reservedPublicDisplayNames = null)
        {
            if (library == null)
            {
                throw new ArgumentNullException(nameof(library));
            }

            if (string.IsNullOrWhiteSpace(civilianId))
            {
                throw new ArgumentException("Civilian id is required.", nameof(civilianId));
            }

            if (string.IsNullOrWhiteSpace(spawnAnchorId))
            {
                throw new ArgumentException("Spawn anchor id is required.", nameof(spawnAnchorId));
            }

            var random = new SystemRandom(seed);

            if (MainTownCuratedAppearanceRules.IsCuratedStyleLibrary(library))
            {
                return GenerateCuratedStyleRecord(
                    library,
                    civilianId,
                    createdAtDay,
                    spawnAnchorId,
                    random,
                    isContractEligible,
                    populationSlotId,
                    poolId,
                    areaTag,
                    isProtectedFromContracts,
                    reservedPublicDisplayNames);
            }

            var baseBodyId = PickRequired(library.BaseBodyIds, nameof(library.BaseBodyIds), random);
            var presentationType = PickRequired(library.PresentationTypes, nameof(library.PresentationTypes), random);
            CivilianPublicIdentityGenerator.Generate(
                random,
                baseBodyId,
                presentationType,
                reservedPublicDisplayNames,
                out var firstName,
                out var lastName,
                out var nickname);

            var hairId = PickRequired(library.HairIds, nameof(library.HairIds), random);
            var hairColorId = PickRequired(library.HairColorIds, nameof(library.HairColorIds), random);
            var eyebrowId = PickOptionalValue(library.EyebrowIds, random);
            var beardId = PickRequired(library.BeardIds, nameof(library.BeardIds), random);
            var outfitTopId = PickRequired(library.OutfitTopIds, nameof(library.OutfitTopIds), random);
            var outfitBottomId = PickRequired(library.OutfitBottomIds, nameof(library.OutfitBottomIds), random);
            var outerwearId = PickRequired(library.OuterwearIds, nameof(library.OuterwearIds), random);
            var materialColorIds = PickOptionalList(library.MaterialColorIds, random);
            var generatedDescriptionTags = PickOptionalList(library.DescriptionTags, random);

            if (IsStyleCompatibleAppearance(baseBodyId, presentationType, outfitTopId, outfitBottomId, outerwearId)
                && MainTownCuratedAppearanceRules.TryInferGender(baseBodyId, presentationType, out var gender))
            {
                eyebrowId = MainTownCuratedAppearanceRules.NormalizeEyebrowId(eyebrowId);
                outfitBottomId = MainTownCuratedAppearanceRules.NormalizeBottomId(gender, outfitBottomId);
            }

            return new CivilianPopulationRecord
            {
                PopulationSlotId = populationSlotId?.Trim() ?? string.Empty,
                PoolId = poolId?.Trim() ?? string.Empty,
                CivilianId = civilianId.Trim(),
                FirstName = firstName,
                LastName = lastName,
                Nickname = nickname,
                IsAlive = true,
                IsContractEligible = isContractEligible,
                IsProtectedFromContracts = isProtectedFromContracts,
                BaseBodyId = baseBodyId,
                PresentationType = presentationType,
                HairId = hairId,
                HairColorId = hairColorId,
                EyebrowId = eyebrowId,
                BeardId = beardId,
                OutfitTopId = outfitTopId,
                OutfitBottomId = outfitBottomId,
                OuterwearId = outerwearId,
                MaterialColorIds = materialColorIds,
                GeneratedDescriptionTags = generatedDescriptionTags,
                SpawnAnchorId = spawnAnchorId.Trim(),
                AreaTag = areaTag?.Trim() ?? string.Empty,
                CreatedAtDay = Math.Max(0, createdAtDay),
                RetiredAtDay = -1
            };
        }

        private static CivilianPopulationRecord GenerateCuratedStyleRecord(
            CivilianAppearanceLibrary library,
            string civilianId,
            int createdAtDay,
            string spawnAnchorId,
            Random random,
            bool isContractEligible,
            string populationSlotId,
            string poolId,
            string areaTag,
            bool isProtectedFromContracts,
            ISet<string> reservedPublicDisplayNames)
        {
            var baseBodyId = PickRequired(library.BaseBodyIds, nameof(library.BaseBodyIds), random);
            var presentationType = MainTownCuratedAppearanceRules.NormalizePresentationType(
                baseBodyId,
                PickRequired(library.PresentationTypes, nameof(library.PresentationTypes), random));
            CivilianPublicIdentityGenerator.Generate(
                random,
                baseBodyId,
                presentationType,
                reservedPublicDisplayNames,
                out var firstName,
                out var lastName,
                out var nickname);

            MainTownCuratedAppearanceRules.TryInferGender(baseBodyId, presentationType, out var gender);

            var hairId = PickRequired(MainTownCuratedAppearanceRules.GetCompatibleHairIds(library, gender), nameof(library.HairIds), random);
            var eyebrowId = MainTownCuratedAppearanceRules.NormalizeEyebrowId(
                PickRequired(MainTownCuratedAppearanceRules.GetCompatibleEyebrowIds(library), nameof(library.EyebrowIds), random));
            var beardChoices = MainTownCuratedAppearanceRules.GetCompatibleBeardIds(library, gender);
            var beardId = beardChoices.Count > 0
                ? PickRequired(beardChoices, nameof(library.BeardIds), random)
                : NoBeardId;

            var outfitTopId = PickRequired(MainTownCuratedAppearanceRules.GetCompatibleBaseTopIds(library), nameof(library.OutfitTopIds), random);
            var outfitBottomId = MainTownCuratedAppearanceRules.NormalizeBottomId(
                gender,
                PickRequired(MainTownCuratedAppearanceRules.GetCompatibleBottomIds(library, gender), nameof(library.OutfitBottomIds), random));
            var outerwearRaw = PickRequired(MainTownCuratedAppearanceRules.GetCompatibleOuterwearIds(library), nameof(library.OuterwearIds), random);
            var outerwearId = MainTownCuratedAppearanceRules.NormalizeOuterwearId(outerwearRaw);
            if (string.IsNullOrWhiteSpace(outerwearId))
            {
                outerwearId = MainTownCuratedAppearanceRules.NoOuterwearId;
            }

            return new CivilianPopulationRecord
            {
                PopulationSlotId = populationSlotId?.Trim() ?? string.Empty,
                PoolId = poolId?.Trim() ?? string.Empty,
                CivilianId = civilianId.Trim(),
                FirstName = firstName,
                LastName = lastName,
                Nickname = nickname,
                IsAlive = true,
                IsContractEligible = isContractEligible,
                IsProtectedFromContracts = isProtectedFromContracts,
                BaseBodyId = baseBodyId,
                PresentationType = presentationType,
                HairId = hairId,
                HairColorId = PickRequired(library.HairColorIds, nameof(library.HairColorIds), random),
                EyebrowId = eyebrowId,
                BeardId = beardId,
                OutfitTopId = outfitTopId,
                OutfitBottomId = outfitBottomId,
                OuterwearId = outerwearId,
                MaterialColorIds = PickOptionalList(library.MaterialColorIds, random),
                GeneratedDescriptionTags = PickOptionalList(library.DescriptionTags, random),
                SpawnAnchorId = spawnAnchorId.Trim(),
                AreaTag = areaTag?.Trim() ?? string.Empty,
                CreatedAtDay = Math.Max(0, createdAtDay),
                RetiredAtDay = -1
            };
        }

        private static string PickRequired(IReadOnlyList<string> values, string fieldName, Random random)
        {
            if (values == null || values.Count == 0)
            {
                throw new InvalidOperationException($"Civilian appearance library requires at least one value for {fieldName}.");
            }

            var candidates = new List<string>(values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    candidates.Add(values[i].Trim());
                }
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException($"Civilian appearance library requires non-empty values for {fieldName}.");
            }

            return candidates[random.Next(candidates.Count)];
        }

        private static string PickOptionalValue(IReadOnlyList<string> values, Random random)
        {
            if (values == null || values.Count == 0)
            {
                return string.Empty;
            }

            var candidates = new List<string>(values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    candidates.Add(values[i].Trim());
                }
            }

            return candidates.Count > 0 ? candidates[random.Next(candidates.Count)] : string.Empty;
        }

        private static bool IsStyleCompatibleAppearance(
            string baseBodyId,
            string presentationType,
            string outfitTopId,
            string outfitBottomId,
            string outerwearId)
        {
            if (MainTownCuratedAppearanceRules.IsCuratedStyleBodyId(baseBodyId))
            {
                return true;
            }

            if (!MainTownCuratedAppearanceRules.TryInferGender(baseBodyId, presentationType, out _))
            {
                return false;
            }

            if (MainTownCuratedAppearanceRules.IsApprovedBaseTopId(outfitTopId))
            {
                return true;
            }

            if (string.Equals(outfitBottomId?.Trim(), "pants1", StringComparison.Ordinal))
            {
                return true;
            }

            var normalizedOuterwearId = outerwearId?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(normalizedOuterwearId)
                   && !string.Equals(normalizedOuterwearId, MainTownCuratedAppearanceRules.NoOuterwearId, StringComparison.Ordinal)
                   && MainTownCuratedAppearanceRules.IsApprovedOuterwearId(normalizedOuterwearId);
        }

        private static List<string> PickOptionalList(IReadOnlyList<string> values, Random random)
        {
            var selected = new List<string>();
            if (values == null || values.Count == 0)
            {
                return selected;
            }

            var candidates = new List<string>(values.Count);
            for (var i = 0; i < values.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    candidates.Add(values[i].Trim());
                }
            }

            if (candidates.Count == 0)
            {
                return selected;
            }

            selected.Add(candidates[random.Next(candidates.Count)]);
            return selected;
        }
    }
}
