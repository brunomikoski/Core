using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Playground.ScenesReferences
{
    [CustomEditor(typeof(SceneIDsCustomEditor), true)]
    public sealed class SceneIDsCustomEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Sync with Build Settings"))
            {
                
            }
        }
    }
}