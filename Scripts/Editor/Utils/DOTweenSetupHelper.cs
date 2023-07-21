using System;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.Core.Utils
{
    [InitializeOnLoad]
    public static class DOTweenSetupHelper
    {
        private static string SCRIPTING_DEFINE_SYMBOL = "DOTWEEN_ENABLED";
        private static string DOTWEEN_ASSEMBLY_NAME = "DOTween.Modules";
        static DOTweenSetupHelper()
        {
            Assembly[] availableAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);

            bool foundDOTween = false;
            for (int i = availableAssemblies.Length - 1; i >= 0; i--)
            {
                if (availableAssemblies[i].name.IndexOf(DOTWEEN_ASSEMBLY_NAME, StringComparison.Ordinal) > -1)
                {
                    foundDOTween = true;
                    break;
                }
            }

            if (foundDOTween)
            {
                AddScriptingDefineSymbol();
            }
            else
            {
                RemoveScriptingDefineSymbol();
            }
        }

        private static void AddScriptingDefineSymbol()
        {
            string scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (scriptingDefineSymbols.Contains(SCRIPTING_DEFINE_SYMBOL))
                return;

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                $"{scriptingDefineSymbols};{SCRIPTING_DEFINE_SYMBOL}");
            
            Debug.Log($"Adding {SCRIPTING_DEFINE_SYMBOL} for {EditorUserBuildSettings.selectedBuildTargetGroup}");
        }

        private static void RemoveScriptingDefineSymbol()
        {
            string scriptingDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            if (!scriptingDefineSymbols.Contains(SCRIPTING_DEFINE_SYMBOL))
                return;

            scriptingDefineSymbols = scriptingDefineSymbols.Replace(SCRIPTING_DEFINE_SYMBOL, string.Empty);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                scriptingDefineSymbols);
        }
    }
}
