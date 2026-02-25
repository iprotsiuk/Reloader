using System.Collections.Generic;

namespace Reloader.Reloading.Runtime
{
    public sealed class MockAmmoResult
    {
        public bool IsRoundComplete;
        public bool IsFunctional;
        public float MisfireChance;
        public float PressureRisk;
        public float AccuracyPenalty;
        public readonly List<string> ConsequenceFlags = new List<string>();
    }
}
