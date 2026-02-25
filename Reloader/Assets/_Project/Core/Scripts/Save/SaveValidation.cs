using System;

namespace Reloader.Core.Save
{
    public static class SaveValidation
    {
        public static void EnsureRequiredString(string value, string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(errorMessage);
            }
        }

        public static void EnsureNonNegative(int value, string errorMessage)
        {
            if (value < 0)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }

        public static void EnsureCountMatch(int expectedCount, int actualCount, string errorMessage)
        {
            if (expectedCount != actualCount)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }

        public static void Ensure(bool condition, string errorMessage)
        {
            if (!condition)
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
    }
}
