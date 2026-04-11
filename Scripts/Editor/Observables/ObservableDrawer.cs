using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Observables.Editor
{
    [CustomPropertyDrawer(typeof(ObservableBase), true)]
    public class ObservableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var obs = fieldInfo.GetValue(GetParent(property)) as ObservableBase;
            if (obs == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            InvokeBeginGui(obs);

            var val = property.FindPropertyRelative(GetValuePropName(obs));

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, val, label, true);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                obs.OnValidate();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var obs = fieldInfo.GetValue(GetParent(property)) as ObservableBase;
            if (obs == null)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            var val = property.FindPropertyRelative(GetValuePropName(obs));
            return EditorGUI.GetPropertyHeight(val, label, true);
        }

        static string GetValuePropName(ObservableBase obs)
        {
            var prop = typeof(ObservableBase).GetProperty("ValuePropName", BindingFlags.Instance | BindingFlags.NonPublic);
            return (string)prop.GetValue(obs);
        }

        static void InvokeBeginGui(ObservableBase obs)
        {
            var m = typeof(ObservableBase).GetMethod("OnBeginGui", BindingFlags.Instance | BindingFlags.NonPublic);
            m.Invoke(obs, null);
        }

        public static object GetParent(SerializedProperty prop)
        {
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }
            return obj;
        }

        static object GetValue(object source, string name)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (f == null)
            {
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p == null)
                    return null;
                return p.GetValue(source, null);
            }
            return f.GetValue(source);
        }

        static object GetValue(object source, string name, int index)
        {
            var enumerable = GetValue(source, name) as IEnumerable;
            var enm = enumerable.GetEnumerator();
            while (index-- >= 0)
                enm.MoveNext();
            return enm.Current;
        }
    }
}
