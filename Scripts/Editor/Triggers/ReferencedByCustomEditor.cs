using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    public abstract class ReferencedByCustomEditor : UnityEditor.Editor
    {
        private readonly List<ReferencingProperty> references = new List<ReferencingProperty>();

        private Transform searchRoot;
        private bool isDirty = true;

        private void OnEnable()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        private void OnDisable()
        {
            ObjectChangeEvents.changesPublished -= OnChangesPublished;
        }

        private void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            isDirty = true;
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (targets.Length > 1)
                return;

            if (isDirty)
                Refresh();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Referenced by", EditorStyles.boldLabel);

            if (references.Count == 0)
                EditorGUILayout.LabelField($"Nothing under '{searchRoot.name}' references this.", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < references.Count; i++)
                    DrawReference(references[i]);
            }

            DrawWarnings(searchRoot, references);
        }

        protected abstract void DrawWarnings(Transform root, IReadOnlyList<ReferencingProperty> found);

        private void Refresh()
        {
            Component component = (Component)target;
            searchRoot = component.transform.root;
            ReferencingPropertyFinder.Find(component, searchRoot, references);
            isDirty = false;
        }

        private void DrawReference(ReferencingProperty reference)
        {
            string hierarchyPath = reference.Owner.transform.GetPath(searchRoot.parent);
            GUIContent label = new GUIContent($"{GetLocationTag(reference.Owner)} {reference.Path}", hierarchyPath);
            EditorGUILayout.ObjectField(label, reference.Owner, typeof(Component), true);
        }

        private string GetLocationTag(Component owner)
        {
            Component component = (Component)target;
            if (owner.gameObject == component.gameObject)
                return "[SELF]";

            return "[OTHER]";
        }
    }
}
