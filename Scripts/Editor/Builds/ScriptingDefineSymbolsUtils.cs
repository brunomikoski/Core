using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace BrunoMikoski.Core.Builds
{
    public static class ScriptingDefineSymbolsUtils
    {
        public static List<string> GetScriptingDefineSymbolsListForBuildTarget(BuildTargetGroup buildTargetGroup)
        {
            NamedBuildTarget buildTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
            List<string> definesList = new List<string>(defines.Split(';'));
            return definesList;
        }
    }
}
