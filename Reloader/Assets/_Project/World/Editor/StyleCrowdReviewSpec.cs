using System;
using System.Collections.Generic;
using System.Linq;

namespace Reloader.World.Editor
{
    public enum StyleCrowdReviewBatchKind
    {
        Plausible,
        Stress,
        Recovered
    }

    public enum StyleCrowdReviewGender
    {
        Male,
        Female
    }

    public sealed class StyleCrowdReviewSpec
    {
        public StyleCrowdReviewSpec(
            string name,
            string archetype,
            StyleCrowdReviewBatchKind batchKind,
            StyleCrowdReviewGender gender,
            string hairId,
            string beardId,
            string topId,
            string eyebrowId,
            string bottomId)
        {
            Name = name?.Trim() ?? string.Empty;
            Archetype = archetype?.Trim() ?? string.Empty;
            BatchKind = batchKind;
            Gender = gender;
            HairId = hairId?.Trim() ?? string.Empty;
            BeardId = beardId?.Trim() ?? string.Empty;
            TopId = topId?.Trim() ?? string.Empty;
            EyebrowId = eyebrowId?.Trim() ?? string.Empty;
            BottomId = bottomId?.Trim() ?? string.Empty;
        }

        public string Name { get; }
        public string Archetype { get; }
        public StyleCrowdReviewBatchKind BatchKind { get; }
        public StyleCrowdReviewGender Gender { get; }
        public string HairId { get; }
        public string BeardId { get; }
        public string TopId { get; }
        public string EyebrowId { get; }
        public string BottomId { get; }
    }

    public static class StyleCrowdReviewCatalog
    {
        public const string MaleBodyId = "male.body";
        public const string FemaleBodyId = "female.body";

        public static readonly IReadOnlyList<string> MaleHairIds = new[]
        {
            "hair.short",
            "hair.swept",
            "hair.wavy",
            "hair.parted"
        };

        public static readonly IReadOnlyList<string> FemaleHairIds = new[]
        {
            "hair.bob",
            "hair.long"
        };

        public static readonly IReadOnlyList<string> CommonHairIds = MaleHairIds.Concat(FemaleHairIds).Distinct().ToArray();

        public static readonly IReadOnlyList<string> MaleBeardIds = new[]
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

        public static readonly IReadOnlyList<string> CommonTopIds = new[]
        {
            "coat",
            "hoody",
            "jacket",
            "openJacket",
            "shirt",
            "shirt2",
            "tshirt1",
            "tshirt2",
            "turtleneck"
        };

        public static readonly IReadOnlyList<string> CommonEyebrowIds = new[]
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

        public const string RequiredBottomId = "pants1";

        public static readonly IReadOnlyList<string> CommonBottomIds = new[]
        {
            "pants1"
        };
    }
}
