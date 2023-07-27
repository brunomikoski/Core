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
        protected override bool DisplayAddButton => false;
        protected override bool DisplayRemoveButton => false;
        protected override bool AllowCustomTypeCreation => false;

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
            for (int i = 0; i < layers.Length; i++)
            {
                string layer = layers[i];
                if (collection.Items.Any(item => string.Equals(item.name, layer, StringComparison.Ordinal)))
                    continue;

                UnityLayer unityLayerSocItem = (UnityLayer) collection.AddNew(collection.GetItemType(), layer);

                SerializedObject so = new SerializedObject(unityLayerSocItem);
                so.FindProperty("layerIndex").intValue = LayerMask.NameToLayer(layer);
                so.ApplyModifiedProperties();
                changed = true;
            }

            for (int i = collection.Count - 1; i >= 0; i--)
            {
                ScriptableObject scriptableObject = collection[i];

                if (!layers.Any(l => string.Equals(l, scriptableObject.name, StringComparison.Ordinal)))
                {
                    RemoveItemAtIndex(i);
                    changed = true;
                }
            }

            if (changed)
            {
                lastCheckedForValidItemsArraySize = -1;
                AssetDatabase.SaveAssets();
                CodeGenerationUtility.GenerateStaticCollectionScript(collection);
                AssetDatabase.Refresh();
            }
        }
    }
}