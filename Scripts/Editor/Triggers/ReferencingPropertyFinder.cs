using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Triggers
{
    public static class ReferencingPropertyFinder
    {
        public static void Find(Component target, Transform root, List<ReferencingProperty> results)
        {
            results.Clear();
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                AddReferences(behaviour, target, results);
            }
        }

        private static void AddReferences(MonoBehaviour behaviour, Component target,
            List<ReferencingProperty> results)
        {
            using SerializedObject serializedObject = new SerializedObject(behaviour);
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;

            while (property.Next(enterChildren))
            {
                enterChildren = CanHoldReferences(property.propertyType);

                if (property.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                if (property.objectReferenceValue != target)
                    continue;

                string path = property.propertyPath.Replace(".Array.data[", "[");
                results.Add(new ReferencingProperty(behaviour, path));
            }
        }

        private static bool CanHoldReferences(SerializedPropertyType propertyType)
        {
            switch (propertyType)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ManagedReference:
                    return true;
                default:
                    return false;
            }
        }
    }
}
