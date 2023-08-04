using System;
using System.Linq;
using BrunoMikoski.Core.Layers;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Editor.Layers
{
    [CustomEditor(typeof(UnityLayerCollection), true)]
    public class UnityLayerCollectionCustomEditor : CollectionCustomEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Sync"))
            {
                SyncLayers();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SyncLayers();
        }

        private void SyncLayers()
        {
            string[] layers = Enumerable.Range(0, 31).Select(LayerMask.LayerToName).Where(l => !string.IsNullOrEmpty(l)).ToArray();

            bool changed = false;

            for (int i = collection.Count - 1; i >= 0; i--)
            {
                ScriptableObject scriptableObject = collection[i];

                if (scriptableObject is not SingleUnityLayer singleUnityLayer)
                    continue;

                bool shouldBeRemoved = true;
                for (int j = 0; j < layers.Length; j++)
                {
                    string layer = layers[j];

                    if (string.Equals(singleUnityLayer.name, layer, StringComparison.Ordinal))
                    {
                        shouldBeRemoved = false;
                        break;
                    }
                }

                if (!shouldBeRemoved) 
                    continue;
                
                collection.RemoveAt(i);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(singleUnityLayer));
                changed = true;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                string layer = layers[i];
                if (string.IsNullOrEmpty(layer))
                    continue;
                
                bool layerAlreadyExists = false;

                for (int j = 0; j < collection.Count; j++)
                {
                    ScriptableObject scriptableObject = collection[j];
                    if (scriptableObject is not SingleUnityLayer singleUnityLayer)
                        continue;

                    if (!singleUnityLayer.name.Equals(layer, StringComparison.Ordinal))
                        continue;
                    
                    layerAlreadyExists = true;
                    if (singleUnityLayer.LayerIndex != i)
                    {
                        SerializedObject serializedObj = new SerializedObject(singleUnityLayer);
                        serializedObj.FindProperty("layerIndex").intValue = i;
                        serializedObj.ApplyModifiedProperties();
                    }
                    break;
                }

                if (layerAlreadyExists)
                    continue;

                SingleUnityLayer unityLayerSocItem = (SingleUnityLayer) collection.AddNew(typeof(SingleUnityLayer), layer);

                SerializedObject so = new SerializedObject(unityLayerSocItem);
                so.FindProperty("layerIndex").intValue = LayerMask.NameToLayer(layer);
                so.ApplyModifiedProperties();
                changed = true;
            }
            
            if (changed)
            {
                AssetDatabase.SaveAssets();
                CodeGenerationUtility.GenerateStaticCollectionScript(collection);
                AssetDatabase.Refresh();
            }
        }
    }
}