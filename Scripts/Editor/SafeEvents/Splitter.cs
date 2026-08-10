using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.SafeEvents
{
    internal static class Splitter
    {
        [Serializable]
        public class SplitterState
        {
            public SplitterState(float[] relativeSizes, int[] minSizes = null, int[] maxSizes = null)
            {
                Initialize();
                state = splitterStateConstructor.Invoke(new object[]
                {
                    relativeSizes,
                    minSizes,
                    maxSizes
                });
            }

            public float WidthA
            {
                get
                {
                    return ((int[])realSizesField.GetValue(state))[0];
                }
            }

            public float WidthB
            {
                get
                {
                    return ((int[])realSizesField.GetValue(state))[1];
                }
            }

            [SerializeField]
            internal object state;
        }

        private static bool isInitialized;
        private static Type splitterStateType;
        private static Type splitterGUILayoutType;
        private static ConstructorInfo splitterStateConstructor;
        private static MethodInfo beginHorizontalSplitMethod;
        private static MethodInfo endHorizontalSplitMethod;
        private static FieldInfo realSizesField;

        private static void Initialize()
        {
            if (isInitialized)
                return;

            splitterStateType = typeof(EditorGUILayout).Assembly.GetType("UnityEditor.SplitterState");
            splitterStateConstructor = splitterStateType.GetConstructor(new[]
            {
                typeof(float[]),
                typeof(int[]),
                typeof(int[])
            });

            realSizesField
                = splitterStateType.GetField("realSizes", BindingFlags.Instance | BindingFlags.Public);

            splitterGUILayoutType =
                typeof(EditorGUILayout).Assembly.GetType("UnityEditor.SplitterGUILayout");
            beginHorizontalSplitMethod = splitterGUILayoutType.GetMethod("BeginHorizontalSplit",
                new[] { splitterStateType, typeof(GUILayoutOption[]) });

            endHorizontalSplitMethod = splitterGUILayoutType.GetMethod("EndHorizontalSplit");

            isInitialized = true;
        }

        public static void BeginHorizontalSplit(SplitterState state, params GUILayoutOption[] options)
        {
            Initialize();

            beginHorizontalSplitMethod.Invoke(null, new[] { state.state, options });
        }

        public static void EndHorizontalSplit()
        {
            Initialize();
            endHorizontalSplitMethod.Invoke(null, null);
        }
    }
}
