using BrunoMikoski.Core.DevelopmentTools.Layers;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Layers
{
    [CustomEditor(typeof(SingleUnityLayer), true)]
    public class SingleLayerCustomEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = false;
            base.OnInspectorGUI();
            GUI.enabled = true;
        }
    }
}
