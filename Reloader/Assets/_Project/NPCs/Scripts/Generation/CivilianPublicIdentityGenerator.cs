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

            firstName = firstNamePool[random.Next(firstNamePool.Length)];
            lastName = LastNames[random.Next(LastNames.Length)];
            nickname = random.Next(NicknameChanceDivisor) == 0
                ? Nicknames[random.Next(Nicknames.Length)]
                : string.Empty;
        }
    }
}
