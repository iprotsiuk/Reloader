using System;
using System.Collections.Generic;
using System.Linq;

namespace Reloader.NPCs.Generation
{
    public enum MainTownAppearanceGender
    {
        Male,
        Female
    }

    public static class MainTownCuratedAppearanceRules
    {
        public const string MaleBodyId = "male.body";
        public const string FemaleBodyId = "female.body";
        public const string MasculinePresentation = "masculine";
        public const string FemininePresentation = "feminine";
        public const string NoOuterwearId = "none";

        private static readonly string[] MaleHairIds =
        {
            "hair.short",
            "hair.swept",
            "hair.wavy",
            "hair.parted"
        };

        private static readonly string[] FemaleHairIds =
        {
            "hair.bob",
            "hair.long"
        };

        private static readonly string[] MaleBeardIds =
        {
            "beard1",
            "beard2",
            "beard3",
            "beard4",
            "beard5",
            "beard6",
            "beard7",
            "beard8",
            "beard9",
            "beard10"
        };

        private static readonly string[] ApprovedEyebrowIds =
        {
            "brous1",
            "brous2",
            "brous3",
            "brous4",
            "brous5",
            "brous6",
            "brous7",
            "brous8",
            "brous9",
            "brous10"
        };

        private static readonly string[] ApprovedBaseTopIds =
        {
            "tshirt1",
            "tshirt2"
        };

        private static readonly string[] ApprovedOuterwearIds =
        {
            NoOuterwearId,
            "jacket",
            "openJacket",
            "hoody"
        };

        private static readonly string[] ApprovedBottomIds =
        {
            "pants1"
        };

        public static CivilianAppearanceLibrary CreateDefaultLibrary()
        {
            return new CivilianAppearanceLibrary
            {
                BaseBodyIds = new[] { MaleBodyId, FemaleBodyId },
                PresentationTypes = new[] { MasculinePresentation, FemininePresentation },
                HairIds = MaleHairIds.Concat(FemaleHairIds).ToArray(),
                HairColorIds = new[] { "hair.variant.a", "hair.variant.b", "hair.variant.c", "hair.variant.d" },
                EyebrowIds = ApprovedEyebrowIds,
                BeardIds = MaleBeardIds,
                OutfitTopIds = ApprovedBaseTopIds,
                OutfitBottomIds = ApprovedBottomIds,
                OuterwearIds = ApprovedOuterwearIds,
                MaterialColorIds = new[] { "style.variant.a", "style.variant.b", "style.variant.c", "style.variant.d" },
                DescriptionTags = new[] { "townsfolk", "resident", "work clothes" }
            };
        }

        public static bool IsCuratedStyleLibrary(CivilianAppearanceLibrary library)
        {
            if (library == null)
            {
                return false;
            }

            if (library.BaseBodyIds == null || library.BaseBodyIds.Length == 0)
            {
                return false;
            }

            return library.BaseBodyIds.Any(id => string.Equals(id?.Trim(), MaleBodyId, StringComparison.Ordinal))
                || library.BaseBodyIds.Any(id => string.Equals(id?.Trim(), FemaleBodyId, StringComparison.Ordinal));
        }

        public static bool TryInferGender(string baseBodyId, string presentationType, out MainTownAppearanceGender gender)
        {
            if (string.Equals(baseBodyId, FemaleBodyId, StringComparison.Ordinal))
            {
                gender = MainTownAppearanceGender.Female;
                return true;
            }

            if (string.Equals(baseBodyId, MaleBodyId, StringComparison.Ordinal))
            {
                gender = MainTownAppearanceGender.Male;
                return true;
            }

            if (string.Equals(presentationType, FemininePresentation, StringComparison.Ordinal))
            {
                gender = MainTownAppearanceGender.Female;
                return true;
            }

            if (string.Equals(presentationType, MasculinePresentation, StringComparison.Ordinal))
            {
                gender = MainTownAppearanceGender.Male;
                return true;
            }

            gender = MainTownAppearanceGender.Male;
            return false;
        }

        public static bool IsCuratedStyleBodyId(string baseBodyId)
        {
            return string.Equals(baseBodyId?.Trim(), MaleBodyId, StringComparison.Ordinal)
                || string.Equals(baseBodyId?.Trim(), FemaleBodyId, StringComparison.Ordinal);
        }

        public static string NormalizePresentationType(string baseBodyId, string presentationType)
        {
            if (TryInferGender(baseBodyId, presentationType, out var gender))
            {
                return gender == MainTownAppearanceGender.Female ? FemininePresentation : MasculinePresentation;
            }

            return string.IsNullOrWhiteSpace(presentationType) ? MasculinePresentation : presentationType.Trim();
        }

        public static string NormalizeBottomId(MainTownAppearanceGender gender, string bottomId)
        {
            if (string.IsNullOrWhiteSpace(bottomId))
            {
                return "pants1";
            }

            var normalized = bottomId.Trim();
            return Contains(ApprovedBottomIds, normalized) ? normalized : "pants1";
        }

        public static string NormalizeEyebrowId(string eyebrowId, string legacyCarrierId = null)
        {
            var normalized = eyebrowId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized)
                && IsApprovedEyebrowId(legacyCarrierId))
            {
                normalized = legacyCarrierId.Trim();
            }

            if (IsApprovedEyebrowId(normalized))
            {
                return normalized;
            }

            return ApprovedEyebrowIds[0];
        }

        public static string NormalizeOuterwearId(string outerwearId)
        {
            if (string.IsNullOrWhiteSpace(outerwearId))
            {
                return string.Empty;
            }

            var trimmed = outerwearId.Trim();
            return string.Equals(trimmed, NoOuterwearId, StringComparison.Ordinal) ? string.Empty : trimmed;
        }

        public static bool IsMaleHairId(string hairId) => Contains(MaleHairIds, hairId);
        public static bool IsFemaleHairId(string hairId) => Contains(FemaleHairIds, hairId);
        public static bool IsApprovedEyebrowId(string eyebrowId) => Contains(ApprovedEyebrowIds, eyebrowId);
        public static bool IsMaleBeardId(string beardId) => Contains(MaleBeardIds, beardId);
        public static bool IsApprovedBaseTopId(string topId) => Contains(ApprovedBaseTopIds, topId);
        public static bool IsApprovedOuterwearId(string outerwearId) => string.IsNullOrWhiteSpace(outerwearId) || Contains(ApprovedOuterwearIds, outerwearId);
        public static bool OuterwearRequiresBaseTop(string outerwearId) => NormalizeOuterwearId(outerwearId) is "jacket" or "openJacket";

        public static IReadOnlyList<string> GetCompatibleHairIds(CivilianAppearanceLibrary library, MainTownAppearanceGender gender)
        {
            var fallback = gender == MainTownAppearanceGender.Male ? MaleHairIds : FemaleHairIds;
            var filtered = Filter(library?.HairIds, candidate => gender == MainTownAppearanceGender.Male ? IsMaleHairId(candidate) : IsFemaleHairId(candidate));
            return filtered.Count > 0 ? filtered : fallback;
        }

        public static IReadOnlyList<string> GetCompatibleBeardIds(CivilianAppearanceLibrary library, MainTownAppearanceGender gender)
        {
            if (gender == MainTownAppearanceGender.Female)
            {
                return Array.Empty<string>();
            }

            var filtered = Filter(library?.BeardIds, IsMaleBeardId);
            return filtered.Count > 0 ? filtered : MaleBeardIds;
        }

        public static IReadOnlyList<string> GetCompatibleEyebrowIds(CivilianAppearanceLibrary library)
        {
            var filtered = Filter(library?.EyebrowIds, IsApprovedEyebrowId);
            return filtered.Count > 0 ? filtered : ApprovedEyebrowIds;
        }

        public static IReadOnlyList<string> GetCompatibleBaseTopIds(CivilianAppearanceLibrary library)
        {
            var filtered = Filter(library?.OutfitTopIds, IsApprovedBaseTopId);
            return filtered.Count > 0 ? filtered : ApprovedBaseTopIds;
        }

        public static IReadOnlyList<string> GetCompatibleOuterwearIds(CivilianAppearanceLibrary library)
        {
            if (library?.OuterwearIds == null || library.OuterwearIds.Length == 0)
            {
                return ApprovedOuterwearIds;
            }

            var filtered = new List<string>();
            var explicitNone = false;

            foreach (var value in library.OuterwearIds)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    explicitNone = true;
                    continue;
                }

                var trimmed = value.Trim();
                if (string.Equals(trimmed, NoOuterwearId, StringComparison.Ordinal))
                {
                    explicitNone = true;
                    continue;
                }

                if (Contains(ApprovedOuterwearIds, trimmed))
                {
                    filtered.Add(trimmed);
                }
            }

            if (filtered.Count > 0)
            {
                if (explicitNone)
                {
                    filtered.Insert(0, NoOuterwearId);
                }

                return filtered
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
            }

            return explicitNone ? new[] { NoOuterwearId } : ApprovedOuterwearIds;
        }

        public static IReadOnlyList<string> GetCompatibleBottomIds(CivilianAppearanceLibrary library, MainTownAppearanceGender gender)
        {
            var filtered = Filter(library?.OutfitBottomIds, candidate => Contains(ApprovedBottomIds, NormalizeBottomId(gender, candidate)));
            if (filtered.Count == 0)
            {
                return new[] { "pants1" };
            }

            return filtered
                .Select(candidate => NormalizeBottomId(gender, candidate))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        private static bool Contains(IEnumerable<string> values, string candidate)
        {
            return values.Any(value => string.Equals(value, candidate?.Trim(), StringComparison.Ordinal));
        }

        private static List<string> Filter(IEnumerable<string> values, Func<string, bool> predicate)
        {
            var filtered = new List<string>();
            if (values == null)
            {
                return filtered;
            }

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                var trimmed = value.Trim();
                if (predicate(trimmed))
                {
                    filtered.Add(trimmed);
                }
            }

            return filtered;
        }
    }
}
