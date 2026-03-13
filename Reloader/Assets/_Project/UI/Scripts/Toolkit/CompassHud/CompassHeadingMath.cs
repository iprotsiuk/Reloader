using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloader.UI.Toolkit.CompassHud
{
    public static class CompassHeadingMath
    {
        private static readonly (string Label, float HeadingDegrees)[] CardinalHeadings =
        {
            ("N", 0f),
            ("E", 90f),
            ("S", 180f),
            ("W", 270f)
        };

        public static float ResolveHeadingDegrees(Vector3 forward)
        {
            var planar = new Vector2(forward.x, forward.z);
            if (planar.sqrMagnitude <= 0.0001f)
            {
                return 0f;
            }

            var heading = Mathf.Atan2(planar.x, planar.y) * Mathf.Rad2Deg;
            return NormalizeDegrees(heading);
        }

        public static float ResolveBearingDegrees(Vector3 viewerPosition, Vector3 targetPosition)
        {
            var delta = targetPosition - viewerPosition;
            delta.y = 0f;
            return ResolveHeadingDegrees(delta);
        }

        public static float ResolveSignedDeltaDegrees(float fromHeadingDegrees, float toHeadingDegrees)
        {
            var delta = NormalizeDegrees(toHeadingDegrees) - NormalizeDegrees(fromHeadingDegrees);
            while (delta > 180f)
            {
                delta -= 360f;
            }

            while (delta < -180f)
            {
                delta += 360f;
            }

            return delta;
        }

        public static CompassHudUiState.EntryState[] CreateCardinalEntries(float currentHeadingDegrees, float visibleHalfAngleDegrees)
        {
            var entries = new List<CompassHudUiState.EntryState>(CardinalHeadings.Length);
            for (var i = 0; i < CardinalHeadings.Length; i++)
            {
                var cardinal = CardinalHeadings[i];
                var delta = ResolveSignedDeltaDegrees(currentHeadingDegrees, cardinal.HeadingDegrees);
                if (Mathf.Abs(delta) > visibleHalfAngleDegrees)
                {
                    continue;
                }

                entries.Add(new CompassHudUiState.EntryState(
                    CompassHudUiState.EntryKind.Cardinal,
                    cardinal.Label,
                    delta));
            }

            entries.Sort(static (left, right) => left.SignedAngleDeltaDegrees.CompareTo(right.SignedAngleDeltaDegrees));
            return entries.ToArray();
        }

        private static float NormalizeDegrees(float degrees)
        {
            var normalized = degrees % 360f;
            if (normalized < 0f)
            {
                normalized += 360f;
            }

            return normalized;
        }
    }
}
