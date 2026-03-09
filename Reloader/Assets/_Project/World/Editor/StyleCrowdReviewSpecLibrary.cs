using System.Collections.Generic;
using System.Linq;

namespace Reloader.World.Editor
{
    public static class StyleCrowdReviewSpecLibrary
    {
        public static IReadOnlyList<StyleCrowdReviewSpec> BuildAll()
        {
            return StyleCrowdReviewPlausibleBatch.Build()
                .Concat(StyleCrowdReviewStressBatch.Build())
                .Concat(StyleCrowdReviewRecoveredBatch.Build())
                .ToArray();
        }
    }
}
