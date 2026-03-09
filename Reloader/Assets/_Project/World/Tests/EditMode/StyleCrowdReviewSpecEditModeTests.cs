using System.Linq;
using NUnit.Framework;
using Reloader.World.Editor;

namespace Reloader.World.Tests.EditMode
{
    public class StyleCrowdReviewSpecEditModeTests
    {
        private static readonly string[] RuntimeSafeTopIds =
        {
            "jacket",
            "tshirt1",
            "tshirt2"
        };

        private static readonly string[] ExpectedPlausibleArchetypes =
        {
            "Police",
            "EMS",
            "BlueCollar",
            "Jogger",
            "Hunter",
            "ParkRanger",
            "Hiker",
            "WhiteCollar",
            "Student",
            "RoughLiving"
        };

        [Test]
        public void BuildAll_ReturnsOneHundredSixteenNamedEntriesAcrossReviewAndRecoveryBatches()
        {
            var specs = StyleCrowdReviewSpecLibrary.BuildAll();

            Assert.That(specs.Count, Is.EqualTo(116));
            Assert.That(specs.Count(spec => spec.BatchKind == StyleCrowdReviewBatchKind.Plausible), Is.EqualTo(50));
            Assert.That(specs.Count(spec => spec.BatchKind == StyleCrowdReviewBatchKind.Stress), Is.EqualTo(50));
            Assert.That(specs.Count(spec => spec.BatchKind == StyleCrowdReviewBatchKind.Recovered), Is.EqualTo(16));
            Assert.That(specs.Select(spec => spec.Name).Distinct().Count(), Is.EqualTo(specs.Count));
        }

        [Test]
        public void PlausibleBatch_CoversEveryRequiredArchetype()
        {
            var plausible = StyleCrowdReviewSpecLibrary.BuildAll()
                .Where(spec => spec.BatchKind == StyleCrowdReviewBatchKind.Plausible)
                .ToArray();

            foreach (var archetype in ExpectedPlausibleArchetypes)
            {
                Assert.That(plausible.Any(spec => spec.Archetype == archetype), Is.True, $"Expected plausible batch to include archetype '{archetype}'.");
            }
        }

        [Test]
        public void AllSpecs_UseOnlyKnownPartIdentifiersForTheirGender()
        {
            var specs = StyleCrowdReviewSpecLibrary.BuildAll();

            foreach (var spec in specs)
            {
                Assert.That(StyleCrowdReviewCatalog.CommonBottomIds, Does.Contain(spec.BottomId), $"{spec.Name} uses unknown bottom '{spec.BottomId}'.");
                Assert.That(StyleCrowdReviewCatalog.CommonTopIds, Does.Contain(spec.TopId), $"{spec.Name} uses unknown top '{spec.TopId}'.");
                Assert.That(StyleCrowdReviewCatalog.CommonHairIds, Does.Contain(spec.HairId), $"{spec.Name} uses unknown hair '{spec.HairId}'.");

                if (spec.Gender == StyleCrowdReviewGender.Male)
                {
                    Assert.That(StyleCrowdReviewCatalog.MaleBodyId, Is.Not.Empty);
                    Assert.That(StyleCrowdReviewCatalog.MaleHairIds, Does.Contain(spec.HairId), $"{spec.Name} uses unsupported male hair '{spec.HairId}'.");
                    Assert.That(StyleCrowdReviewCatalog.MaleBeardIds.Append(string.Empty), Does.Contain(spec.BeardId), $"{spec.Name} uses unsupported male beard '{spec.BeardId}'.");
                }
                else
                {
                    Assert.That(StyleCrowdReviewCatalog.FemaleBodyId, Is.Not.Empty);
                    Assert.That(StyleCrowdReviewCatalog.FemaleHairIds, Does.Contain(spec.HairId), $"{spec.Name} uses unsupported female hair '{spec.HairId}'.");
                    Assert.That(spec.BeardId, Is.EqualTo(string.Empty), $"{spec.Name} should not assign a beard to a female rig.");
                }
            }
        }

        [Test]
        public void Catalog_UsesJacketAndOpenJacket_NotLegacyShirt3()
        {
            CollectionAssert.Contains(StyleCrowdReviewCatalog.CommonTopIds, "jacket");
            CollectionAssert.Contains(StyleCrowdReviewCatalog.CommonTopIds, "openJacket");
            CollectionAssert.DoesNotContain(StyleCrowdReviewCatalog.CommonTopIds, "shirt3");

            var specs = StyleCrowdReviewSpecLibrary.BuildAll();
            Assert.That(specs.Any(spec => spec.TopId == "shirt3"), Is.False);
        }

        [Test]
        public void AllSpecs_CoverEveryRuntimeSafeTopAndReferenceEveryBottomAtLeastOnce()
        {
            var specs = StyleCrowdReviewSpecLibrary.BuildAll()
                .Where(spec => spec.BatchKind != StyleCrowdReviewBatchKind.Recovered)
                .ToArray();

            foreach (var topId in RuntimeSafeTopIds)
            {
                Assert.That(specs.Any(spec => spec.TopId == topId), Is.True, $"Expected at least one spec to use top '{topId}'.");
            }

            foreach (var bottomId in StyleCrowdReviewCatalog.CommonBottomIds)
            {
                Assert.That(specs.Any(spec => spec.BottomId == bottomId), Is.True, $"Expected at least one source spec to reference bottom '{bottomId}'.");
            }
        }

        [Test]
        public void AllSpecs_UseOnlyRuntimeSafeTopIdsForReviewScene()
        {
            var specs = StyleCrowdReviewSpecLibrary.BuildAll()
                .Where(spec => spec.BatchKind != StyleCrowdReviewBatchKind.Recovered)
                .ToArray();

            foreach (var spec in specs)
            {
                Assert.That(RuntimeSafeTopIds, Does.Contain(spec.TopId), $"{spec.Name} uses disabled review-scene top '{spec.TopId}'.");
            }
        }

        [Test]
        public void RecoveredBatch_UsesOnlyRecoveredTopIds()
        {
            var recoveredTopIds = new[]
            {
                "hoody",
                "jacket",
                "openJacket"
            };

            var specs = StyleCrowdReviewSpecLibrary.BuildAll()
                .Where(spec => spec.BatchKind == StyleCrowdReviewBatchKind.Recovered)
                .ToArray();

            Assert.That(specs, Is.Not.Empty);

            foreach (var spec in specs)
            {
                Assert.That(recoveredTopIds, Does.Contain(spec.TopId), $"{spec.Name} should only use recovered top IDs.");
            }
        }
    }
}
