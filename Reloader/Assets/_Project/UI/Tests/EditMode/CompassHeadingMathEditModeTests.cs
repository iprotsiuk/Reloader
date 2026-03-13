using NUnit.Framework;
using Reloader.UI.Toolkit.CompassHud;
using UnityEngine;

namespace Reloader.UI.Tests.EditMode
{
    public class CompassHeadingMathEditModeTests
    {
        [TestCase(0f, 0f, 1f, 0f)]
        [TestCase(1f, 0f, 0f, 90f)]
        [TestCase(0f, 0f, -1f, 180f)]
        [TestCase(-1f, 0f, 0f, 270f)]
        public void ResolveHeadingDegrees_UsesTrueNorthAndTrueEast(float x, float y, float z, float expectedHeading)
        {
            var heading = CompassHeadingMath.ResolveHeadingDegrees(new Vector3(x, y, z));

            Assert.That(heading, Is.EqualTo(expectedHeading).Within(0.001f));
        }

        [TestCase(350f, 10f, 20f)]
        [TestCase(10f, 350f, -20f)]
        [TestCase(90f, 270f, 180f)]
        [TestCase(270f, 90f, -180f)]
        public void ResolveSignedDeltaDegrees_ReturnsShortestAngle(float fromHeading, float toHeading, float expectedDelta)
        {
            var delta = CompassHeadingMath.ResolveSignedDeltaDegrees(fromHeading, toHeading);

            Assert.That(delta, Is.EqualTo(expectedDelta).Within(0.001f));
        }

        [Test]
        public void ResolveBearingDegrees_IgnoresVerticalOffsetAndUsesXZPlane()
        {
            var viewer = new Vector3(3f, 10f, 4f);
            var target = new Vector3(13f, -25f, 4f);

            var bearing = CompassHeadingMath.ResolveBearingDegrees(viewer, target);

            Assert.That(bearing, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void CreateCardinalEntries_BuildsRepeatedMarkersAroundHeadingWindow()
        {
            var entries = CompassHeadingMath.CreateCardinalEntries(currentHeadingDegrees: 0f, visibleHalfAngleDegrees: 100f);

            Assert.That(entries, Has.Length.EqualTo(3));
            Assert.That(entries[0].Key, Is.EqualTo("W:-90"));
            Assert.That(entries[0].Label, Is.EqualTo("W"));
            Assert.That(entries[0].SignedAngleDeltaDegrees, Is.EqualTo(-90f).Within(0.001f));
            Assert.That(entries[1].Key, Is.EqualTo("N:0"));
            Assert.That(entries[1].Label, Is.EqualTo("N"));
            Assert.That(entries[1].SignedAngleDeltaDegrees, Is.EqualTo(0f).Within(0.001f));
            Assert.That(entries[2].Key, Is.EqualTo("E:90"));
            Assert.That(entries[2].Label, Is.EqualTo("E"));
            Assert.That(entries[2].SignedAngleDeltaDegrees, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void CreateCardinalEntries_WhenWindowSpansPastOneRotation_ReturnsRepeatedCardinalsWithUniqueKeys()
        {
            var entries = CompassHeadingMath.CreateCardinalEntries(currentHeadingDegrees: 0f, visibleHalfAngleDegrees: 400f);

            Assert.That(entries, Has.Length.GreaterThan(4));
            Assert.That(entries, Has.Some.Matches<CompassHudUiState.EntryState>(entry => entry.Key == "N:-360" && entry.Label == "N"));
            Assert.That(entries, Has.Some.Matches<CompassHudUiState.EntryState>(entry => entry.Key == "N:0" && entry.Label == "N"));
            Assert.That(entries, Has.Some.Matches<CompassHudUiState.EntryState>(entry => entry.Key == "N:360" && entry.Label == "N"));
        }
    }
}
