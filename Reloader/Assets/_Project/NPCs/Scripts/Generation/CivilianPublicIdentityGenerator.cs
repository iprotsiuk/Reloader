using SystemRandom = System.Random;

namespace Reloader.NPCs.Generation
{
    internal static class CivilianPublicIdentityGenerator
    {
        private static readonly string[] MasculineFirstNames =
        {
            "Derek", "Tomas", "Maksim", "Viktor", "Leon", "Pavel", "Martin", "Adrian", "Stefan", "Oleg", "Niko", "Ivan"
        };

        private static readonly string[] FeminineFirstNames =
        {
            "Ilona", "Marta", "Petra", "Alina", "Nadia", "Vera", "Sonya", "Elena", "Daria", "Klara", "Milena", "Irina"
        };

        private static readonly string[] NeutralFirstNames =
        {
            "Sasha", "Alex", "Robin", "Luka", "Nikita", "Mika"
        };

        private static readonly string[] LastNames =
        {
            "Mullen", "Varga", "Volkov", "Sidorov", "Novak", "Kozak", "Markovic", "Dobrev", "Petrov", "Kolar", "Dragan", "Hale"
        };

        private static readonly string[] Nicknames =
        {
            "Socks", "Patch", "Brick", "Lucky", "Mole", "Birdie", "Dusty", "Skipper", "Ox", "Cricket"
        };

        private const int NicknameChanceDivisor = 6;

        public static void Generate(int seed, string baseBodyId, string presentationType, out string firstName, out string lastName, out string nickname)
        {
            Generate(new SystemRandom(seed), baseBodyId, presentationType, out firstName, out lastName, out nickname);
        }

        public static void Generate(SystemRandom random, string baseBodyId, string presentationType, out string firstName, out string lastName, out string nickname)
        {
            Generate(random, baseBodyId, presentationType, reservedDisplayNames: null, out firstName, out lastName, out nickname);
        }

        public static void Generate(
            SystemRandom random,
            string baseBodyId,
            string presentationType,
            System.Collections.Generic.ISet<string> reservedDisplayNames,
            out string firstName,
            out string lastName,
            out string nickname)
        {
            if (random == null)
            {
                throw new System.ArgumentNullException(nameof(random));
            }

            var normalizedBody = baseBodyId?.Trim().ToLowerInvariant() ?? string.Empty;
            var normalizedPresentation = presentationType?.Trim().ToLowerInvariant() ?? string.Empty;

            var firstNamePool = NeutralFirstNames;
            if (normalizedBody.Contains("female") || normalizedPresentation.Contains("femin"))
            {
                firstNamePool = FeminineFirstNames;
            }
            else if (normalizedBody.Contains("male") || normalizedPresentation.Contains("mascul"))
            {
                firstNamePool = MasculineFirstNames;
            }

            AssignUniqueDisplayName(random, firstNamePool, reservedDisplayNames, out firstName, out lastName);
            nickname = random.Next(NicknameChanceDivisor) == 0
                ? Nicknames[random.Next(Nicknames.Length)]
                : string.Empty;
        }

        private static void AssignUniqueDisplayName(
            SystemRandom random,
            string[] firstNamePool,
            System.Collections.Generic.ISet<string> reservedDisplayNames,
            out string firstName,
            out string lastName)
        {
            var firstIndex = random.Next(firstNamePool.Length);
            var lastIndex = random.Next(LastNames.Length);
            if (reservedDisplayNames == null || reservedDisplayNames.Count == 0)
            {
                firstName = firstNamePool[firstIndex];
                lastName = LastNames[lastIndex];
                return;
            }

            var totalCombinations = firstNamePool.Length * LastNames.Length;
            var startingIndex = (firstIndex * LastNames.Length) + lastIndex;
            for (var offset = 0; offset < totalCombinations; offset++)
            {
                var combinationIndex = (startingIndex + offset) % totalCombinations;
                var candidateFirstName = firstNamePool[combinationIndex / LastNames.Length];
                var candidateLastName = LastNames[combinationIndex % LastNames.Length];
                if (reservedDisplayNames.Contains(BuildDisplayName(candidateFirstName, candidateLastName)))
                {
                    continue;
                }

                firstName = candidateFirstName;
                lastName = candidateLastName;
                return;
            }

            firstName = firstNamePool[firstIndex];
            lastName = LastNames[lastIndex];
        }

        private static string BuildDisplayName(string firstName, string lastName)
        {
            var normalizedFirstName = firstName?.Trim() ?? string.Empty;
            var normalizedLastName = lastName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedFirstName))
            {
                return normalizedLastName;
            }

            if (string.IsNullOrWhiteSpace(normalizedLastName))
            {
                return normalizedFirstName;
            }

            return string.Concat(normalizedFirstName, " ", normalizedLastName);
        }
    }
}
