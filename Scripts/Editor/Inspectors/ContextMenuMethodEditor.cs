using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class ContextMenuMethodEditor : UnityEditor.Editor
    {
        private Dictionary<MethodInfo, List<ContextMenu>> methodToContextMenus = new Dictionary<MethodInfo, List<ContextMenu>>();
        private void OnEnable()
        {
            Type targetType = target.GetType();
            MethodInfo[] methods = targetType.GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            foreach (MethodInfo methodInfo in methods)
            {
                ContextMenu[] contextMenus =
                    (ContextMenu[])methodInfo.GetCustomAttributes(typeof(ContextMenu), false);

                foreach (ContextMenu contextMenu in contextMenus)
                {
                    if (methodToContextMenus.TryAdd(methodInfo, new List<ContextMenu>()))
                    {
                        methodToContextMenus[methodInfo].Add(contextMenu);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            foreach (var methodToContextMenus in methodToContextMenus)
            {
                if (GUILayout.Button(methodToContextMenus.Key.Name))
                {
                    methodToContextMenus.Key.Invoke(target, null);
                }

            }
        }
    }
}