using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace BrunoMikoski.Core.Attributes
{
    public abstract class ArrayElementPropertyDrawer : PropertyDrawer
    {
        protected static int GetPropertyIndexByPath(string path)
        {
            // Array element path looks like this: listName.Array.data[0], so get the index, we
            // search the path for the last "[" and grab the string until the end of the string - 2
            Assert.IsTrue(path.EndsWith("]"));
            int lastBracketIndex = path.LastIndexOf("[", StringComparison.Ordinal);
            string indexStr = path.Substring(lastBracketIndex + 1, path.Length - lastBracketIndex - 2);
            return int.Parse(indexStr);
        }

        protected static string GetPropertyArrayPathByItemPath(string path)
        {
            // Array element path looks like this: listName.Array.data[0], so to move to the path of
            // the array property itself, we remove the last two dereferences
            path = path.Substring(0, path.LastIndexOf(".", StringComparison.Ordinal));
            path = path.Substring(0, path.LastIndexOf(".", StringComparison.Ordinal));

            return path;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        protected bool CheckIfArrayOrDrawDefault(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!property.propertyPath.EndsWith("]"))
            {
                Debug.LogWarning("Property is not an array element", property.serializedObject.targetObject);
                EditorGUI.PropertyField(position, property, label, true);
                return false;
            }
            return true;
        }

    }
}