namespace BrunoMikoski.Core.DevelopmentTools
{
    // Platform families used for matchmaking pools and platform-network features. GDK on Windows
    // belongs to the Xbox family because those users live on Xbox Live.
    public static class RuntimePlatformFamilies
    {
        public const RuntimePlatformFlag Pc = RuntimePlatformFlag.Windows | RuntimePlatformFlag.OSX | RuntimePlatformFlag.Linux
                                            | RuntimePlatformFlag.Steam | RuntimePlatformFlag.Epic_Games_Store
                                            | RuntimePlatformFlag.Steam_Deck;
        public const RuntimePlatformFlag PlayStation = RuntimePlatformFlag.PS4 | RuntimePlatformFlag.PS5;
        public const RuntimePlatformFlag Xbox = RuntimePlatformFlag.XboxOne | RuntimePlatformFlag.XboxSeriesXS | RuntimePlatformFlag.XboxGDK;
        //public const RuntimePlatformFlag Switch = RuntimePlatformFlag.Switch2;

        // Fixed order so every client derives an identical family list.
        public static readonly (string Token, RuntimePlatformFlag Mask)[] Families =
        {
            ("PC", Pc),
            ("Playstation", PlayStation),
            ("Xbox", Xbox)
            // , ("Switch", Switch),
        };

        public static bool TryGetFamily(RuntimePlatformFlag platform, out RuntimePlatformFlag familyMask)
        {
            foreach ((string _, RuntimePlatformFlag mask) in Families)
            {
                if ((platform & mask) != 0)
                {
                    familyMask = mask;
                    return true;
                }
            }

            familyMask = RuntimePlatformFlag.None;
            return false;
        }

        public static bool AreSameFamily(RuntimePlatformFlag first, RuntimePlatformFlag second)
        {
            return TryGetFamily(first, out RuntimePlatformFlag familyMask) && (second & familyMask) != 0;
        }
    }
}
