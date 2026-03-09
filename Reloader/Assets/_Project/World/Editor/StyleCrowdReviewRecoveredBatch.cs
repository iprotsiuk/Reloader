using System.Collections.Generic;

namespace Reloader.World.Editor
{
    public static class StyleCrowdReviewRecoveredBatch
    {
        public static IReadOnlyList<StyleCrowdReviewSpec> Build()
        {
            return new[]
            {
                new StyleCrowdReviewSpec("OpenJacket_Recovered_01", "RecoveredOpenJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.short", "beard4", "openJacket", "brous1"),
                new StyleCrowdReviewSpec("OpenJacket_Recovered_02", "RecoveredOpenJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.parted", string.Empty, "openJacket", "brous2"),
                new StyleCrowdReviewSpec("OpenJacket_Recovered_03", "RecoveredOpenJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.wavy", "beard6", "openJacket", "brous5"),
                new StyleCrowdReviewSpec("OpenJacket_Recovered_04", "RecoveredOpenJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.swept", string.Empty, "openJacket", "pants1"),
                new StyleCrowdReviewSpec("Hoody_Recovered_01", "RecoveredHoody", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.short", string.Empty, "hoody", "brous6"),
                new StyleCrowdReviewSpec("Hoody_Recovered_02", "RecoveredHoody", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.parted", "beard2", "hoody", "brous1"),
                new StyleCrowdReviewSpec("Hoody_Recovered_03", "RecoveredHoody", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.wavy", string.Empty, "hoody", "pants1"),
                new StyleCrowdReviewSpec("Hoody_Recovered_04", "RecoveredHoody", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.swept", "beard5", "hoody", "brous5"),
                new StyleCrowdReviewSpec("JacketLayered_Recovered_01", "RecoveredJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.short", string.Empty, "jacket", "pants1"),
                new StyleCrowdReviewSpec("JacketLayered_Recovered_02", "RecoveredJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Male, "hair.parted", "beard3", "jacket", "brous2"),
                new StyleCrowdReviewSpec("JacketLayered_Recovered_03", "RecoveredJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "jacket", "pants1"),
                new StyleCrowdReviewSpec("JacketLayered_Recovered_04", "RecoveredJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Female, "hair.long", string.Empty, "jacket", "brous3"),
                new StyleCrowdReviewSpec("OpenJacket_Recovered_05", "RecoveredOpenJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "openJacket", "pants1"),
                new StyleCrowdReviewSpec("OpenJacket_Recovered_06", "RecoveredOpenJacket", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Female, "hair.long", string.Empty, "openJacket", "brous7"),
                new StyleCrowdReviewSpec("Hoody_Recovered_05", "RecoveredHoody", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Female, "hair.bob", string.Empty, "hoody", "pants1"),
                new StyleCrowdReviewSpec("Hoody_Recovered_06", "RecoveredHoody", StyleCrowdReviewBatchKind.Recovered, StyleCrowdReviewGender.Female, "hair.long", string.Empty, "hoody", "brous3")
            };
        }
    }
}
