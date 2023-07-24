using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BrunoMikoski.Core.Routines
{
    public static class CoroutineServiceTracker
    {
        public struct RoutineData
        {
            public readonly DateTime Created;
            public readonly string Name;
            public readonly string StackTrace;
            public readonly Routine Routine;

            public RoutineData(DateTime created, string name, string stackTrace, Routine targetRoutine)
            {
                Created = created;
                Name = name;
                StackTrace = stackTrace;
                Routine = targetRoutine;
            }
        }

        public static readonly List<RoutineData> RUNNING_ROUTINES = new(1000);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void SupportDomainReload()
        {
            RUNNING_ROUTINES.Clear();
        }
        
        public static void TrackRoutine(Routine targetRoutine)
        {
#if UNITY_EDITOR
            string stackTrace = new StackTrace(3, true).CleanupAsyncStackTrace() ;
            
            RUNNING_ROUTINES.Add(new RoutineData(DateTime.UtcNow, GetFirstLine(stackTrace), stackTrace, targetRoutine));
#endif
        }
        
        static string GetFirstLine(string str)
        {
            Match match = Regex.Match(str, @"^(.*?\))");
        
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return str;
        }
    }
}
