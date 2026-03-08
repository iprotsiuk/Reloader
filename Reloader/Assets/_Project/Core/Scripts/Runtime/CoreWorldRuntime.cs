using System;
using System.Globalization;

namespace Reloader.Core.Runtime
{
    public sealed class CoreWorldRuntime
    {
        public readonly struct Snapshot
        {
            public Snapshot(int dayCount, float timeOfDay)
            {
                DayCount = Math.Max(0, dayCount);
                TimeOfDay = NormalizeTimeOfDay(timeOfDay);
            }

            public int DayCount { get; }
            public float TimeOfDay { get; }

            public string GetDayOfWeekName()
            {
                return DayNames[DayCount % DayNames.Length];
            }

            public string GetFormattedTimeOfDay()
            {
                var totalMinutes = (int)Math.Round(TimeOfDay * 60f, MidpointRounding.AwayFromZero);
                var normalizedMinutes = ((totalMinutes % MinutesPerDay) + MinutesPerDay) % MinutesPerDay;
                var hours = normalizedMinutes / 60;
                var minutes = normalizedMinutes % 60;
                return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", hours, minutes);
            }
        }

        private static readonly string[] DayNames =
        {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"
        };

        private const int MinutesPerDay = 24 * 60;

        public CoreWorldRuntime(int dayCount = 0, float timeOfDay = 8f)
        {
            DayCount = Math.Max(0, dayCount);
            TimeOfDay = NormalizeTimeOfDay(timeOfDay);
        }

        public int DayCount { get; private set; }
        public float TimeOfDay { get; private set; }

        public Snapshot CaptureSnapshot()
        {
            return new Snapshot(DayCount, TimeOfDay);
        }

        public void SetWorldState(int dayCount, float timeOfDay)
        {
            DayCount = Math.Max(0, dayCount);
            TimeOfDay = NormalizeTimeOfDay(timeOfDay);
        }

        public bool AdvanceRealtimeSeconds(float realtimeSeconds, float worldMinutesPerRealtimeSecond = 1f)
        {
            if (float.IsNaN(realtimeSeconds) || float.IsInfinity(realtimeSeconds) || realtimeSeconds <= 0f)
            {
                return false;
            }

            if (float.IsNaN(worldMinutesPerRealtimeSecond)
                || float.IsInfinity(worldMinutesPerRealtimeSecond)
                || worldMinutesPerRealtimeSecond <= 0f)
            {
                return false;
            }

            var deltaMinutes = realtimeSeconds * worldMinutesPerRealtimeSecond;
            if (deltaMinutes <= 0f)
            {
                return false;
            }

            var totalMinutes = (TimeOfDay * 60d) + deltaMinutes;
            var wrappedDayDelta = (int)Math.Floor(totalMinutes / MinutesPerDay);
            DayCount = Math.Max(0, DayCount + wrappedDayDelta);
            TimeOfDay = NormalizeTimeOfDay((float)(totalMinutes / 60d));
            return true;
        }

        private static float NormalizeTimeOfDay(float timeOfDay)
        {
            if (float.IsNaN(timeOfDay) || float.IsInfinity(timeOfDay))
            {
                return 0f;
            }

            var normalized = timeOfDay % 24f;
            if (normalized < 0f)
            {
                normalized += 24f;
            }

            return normalized;
        }
    }
}
