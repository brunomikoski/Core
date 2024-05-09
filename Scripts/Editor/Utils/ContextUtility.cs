using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Utils
{
    public static class ContextUtility
    {
        [MenuItem("Assets/Copy GUID to Clipboard")]
        public static void CopyGuidToClipboard()
        {
            Object context = Selection.activeObject;
            if (context == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(context);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            Debug.Log($"[{assetPath}] -> GUID: {guid} copied to clipboard");
            EditorGUIUtility.systemCopyBuffer = guid;
        }
        
    }
}