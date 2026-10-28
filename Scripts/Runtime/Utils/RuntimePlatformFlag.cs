using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrunoMikoski.Core.DevelopmentTools
{
    [System.Flags]
    public enum RuntimePlatformFlag
    {
        None = 0,
        Windows = 1 << 0,
        OSX = 1 << 1,
        Linux = 1 << 2,
        iOS = 1 << 3,
        Android = 1 << 4,
        WebGL = 1 << 5,
        PS4 = 1 << 6,
        PS5 = 1 << 7,
        XboxOne = 1 << 8,
        XboxSeriesXS = 1 << 9,
        Switch2 = 1 << 10,
        Steam_Deck = 1 << 11,
        Epic_Games_Store = 1 << 12,
        Steam = 1 << 13,
        XboxGDK = 1 << 14,
        
        All = ~0,
        Consoles = PS4 | PS5 | XboxOne | XboxSeriesXS | Switch2,
        Xbox = XboxOne | XboxSeriesXS,
        PlayStation = PS4 | PS5,
    }
    
    
    
    
    public static class RuntimePlatformUtils
    {
        public static bool HasFlagFast(this RuntimePlatformFlag platformType, RuntimePlatformFlag flag)
        {
            return (platformType & flag) == flag;
        }
        
        public static bool HasAnyFlagFast(this RuntimePlatformFlag platformType, RuntimePlatformFlag flags)
        {
            return (platformType & flags) != 0;
        }
        
        
        public static bool IsPlatformIncluded(RuntimePlatformFlag includedPlatforms)
        {
            #if UNITY_EDITOR
            // In the editor use the current build target to simulate the runtime platform
            RuntimePlatformFlag currentPlatformFlag = ToPlatformFlag(EditorUserBuildSettings.activeBuildTarget);
            #else
            // Convert current runtime platform to your flag
            RuntimePlatformFlag currentPlatformFlag = ToPlatformFlag(Application.platform);
            #endif

            return (includedPlatforms & currentPlatformFlag) != 0;
        }

        public static RuntimePlatformFlag ToPlatformFlag(RuntimePlatform platform)
        {
            return platform switch
            {
                RuntimePlatform.WindowsEditor       => RuntimePlatformFlag.Windows,
                RuntimePlatform.WindowsPlayer       => RuntimePlatformFlag.Windows,
                RuntimePlatform.WSAPlayerX86        => RuntimePlatformFlag.Windows,
                RuntimePlatform.WSAPlayerX64        => RuntimePlatformFlag.Windows,
                RuntimePlatform.WSAPlayerARM        => RuntimePlatformFlag.Windows,

                RuntimePlatform.OSXPlayer           => RuntimePlatformFlag.OSX,
                RuntimePlatform.OSXEditor           => RuntimePlatformFlag.OSX,

                RuntimePlatform.LinuxPlayer         => RuntimePlatformFlag.Linux,
                RuntimePlatform.LinuxEditor         => RuntimePlatformFlag.Linux,
                
                RuntimePlatform.IPhonePlayer        => RuntimePlatformFlag.iOS,
                RuntimePlatform.tvOS                => RuntimePlatformFlag.iOS,
                
                RuntimePlatform.Android             => RuntimePlatformFlag.Android,
                RuntimePlatform.WebGLPlayer         => RuntimePlatformFlag.WebGL,
                RuntimePlatform.PS4                 => RuntimePlatformFlag.PS4,
                RuntimePlatform.PS5                 => RuntimePlatformFlag.PS5,
                RuntimePlatform.XboxOne             => RuntimePlatformFlag.XboxOne,
                RuntimePlatform.GameCoreXboxOne     => RuntimePlatformFlag.XboxOne,
                RuntimePlatform.GameCoreXboxSeries  => RuntimePlatformFlag.XboxSeriesXS,
                RuntimePlatform.Switch              => RuntimePlatformFlag.Switch2,

                // Fallback to unknown if we can't match the current runtime platform to one of our flags
                _ => RuntimePlatformFlag.None,
            };
        }
        
        #if UNITY_EDITOR
        public static RuntimePlatformFlag ToPlatformFlag(BuildTarget platform)
        {
            return platform switch
            {
                BuildTarget.StandaloneWindows       => RuntimePlatformFlag.Windows,
                BuildTarget.StandaloneWindows64     => RuntimePlatformFlag.Windows,
                BuildTarget.WSAPlayer               => RuntimePlatformFlag.Windows,

                BuildTarget.StandaloneOSX           => RuntimePlatformFlag.OSX,

                BuildTarget.StandaloneLinux64       => RuntimePlatformFlag.Linux,
                BuildTarget.LinuxHeadlessSimulation => RuntimePlatformFlag.Linux,
                
                BuildTarget.VisionOS                => RuntimePlatformFlag.iOS,
                BuildTarget.tvOS                    => RuntimePlatformFlag.iOS,
                
                BuildTarget.Android                 => RuntimePlatformFlag.Android,
                BuildTarget.WebGL                   => RuntimePlatformFlag.WebGL,
                BuildTarget.PS4                     => RuntimePlatformFlag.PS4,
                BuildTarget.PS5                     => RuntimePlatformFlag.PS5,
                BuildTarget.XboxOne                 => RuntimePlatformFlag.XboxOne,
                BuildTarget.GameCoreXboxOne         => RuntimePlatformFlag.XboxOne,
                BuildTarget.GameCoreXboxSeries      => RuntimePlatformFlag.XboxSeriesXS,
                BuildTarget.Switch                  => RuntimePlatformFlag.Switch2,

                // Fallback to unknown if we can't match the current build target to one of our flags
                _ => RuntimePlatformFlag.None,
            };
        }
        #endif
    }
}
