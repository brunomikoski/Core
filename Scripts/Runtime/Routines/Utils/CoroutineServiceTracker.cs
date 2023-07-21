using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace BrunoMikoski.Core.Routines
{
    public static class CoroutineServiceTracker
    {
        public struct RoutineData
        {
            public readonly DateTime UtcNow;
            public readonly string Name;
            public readonly string StackTrace;
            public readonly Routine Routine;

            public RoutineData(DateTime utcNow, string name, string stackTrace, Routine targetRoutine)
            {
                UtcNow = utcNow;
                Name = name;
                StackTrace = stackTrace;
                Routine = targetRoutine;
            }
        }

        public static readonly List<RoutineData> RUNNING_ROUTINES = new(1000);
        private static readonly Regex REMOVE_HREF = new Regex("<a href.+>(.+)</a>", RegexOptions.Compiled);

        public static void TrackRoutine(Routine targetRoutine)
        {
#if UNITY_EDITOR
            string stackTrace = new StackTrace(3, true).CleanupAsyncStackTrace() ;
            string typeName;
            StringBuilder sb = new StringBuilder();
            DiagnosticsExtensions.TypeBeautify(targetRoutine.GetType(), sb);
            typeName = sb.ToString();

            
            RUNNING_ROUTINES.Add(new RoutineData(DateTime.UtcNow, GetFirstLine(stackTrace), stackTrace, targetRoutine));
#endif
        }
        
        static string GetFirstLine(string str)
        {
            StringBuilder sb = new();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\r' || str[i] == '\n')
                {
                    break;
                }
                sb.Append(str[i]);
            }

            return REMOVE_HREF.Replace(sb.ToString(), "$1");
        }
    }
}
