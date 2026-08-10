using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BrunoMikoski.Core.DevelopmentTools
{
    public class DevelopmentSettingGeneric<T>
    {
        private static readonly Type BOOL_TYPE = typeof(bool);
        private static readonly Type INT_TYPE = typeof(int);
        private static readonly Type STRING_TYPE = typeof(string);
        private static readonly Type COLOR_TYPE = typeof(Color);

        private string editorPrefKey;
        private readonly T defaultValue;
#if UNITY_EDITOR
        private bool hasCachedValue;
        private T cachedValue;
#elif !UNITY_EDITOR
        private T runtimeValue = default(T);
#endif

        public DevelopmentSettingGeneric(string editorPrefKey, T defaultValue, bool cacheValue = true)
        {
            this.defaultValue = defaultValue;
            this.editorPrefKey = editorPrefKey;
#if UNITY_EDITOR
            if (cacheValue)
                this.cachedValue = GetValue();
#else
            runtimeValue = defaultValue;
#endif
        }

#if UNITY_EDITOR
        public T Value
        {
            get
            {
                if (!hasCachedValue)
                {
                    cachedValue = GetValue();
                    hasCachedValue = true;
                }

                return cachedValue;
            }
            set { SetValue(value); }
        }
#else
       public T Value
        {
            get { return runtimeValue; }
            set { runtimeValue = value; }
        }
#endif

        public void ResetValue()
        {
            Value = defaultValue;
        }

        public static implicit operator T(DevelopmentSettingGeneric<T> item)
        {
            return item.Value;
        }

#if UNITY_EDITOR
        private T GetValue()
        {
            if (EditorPrefs.HasKey(editorPrefKey))
            {
                Type type = typeof(T);
                if (type.IsEnum)
                {
                    return (T) Enum.ToObject(type, EditorPrefs.GetInt(editorPrefKey));
                }

                if (type == BOOL_TYPE)
                {
                    return (T) (object) EditorPrefs.GetBool(editorPrefKey);
                }

                if (type == STRING_TYPE)
                {
                    return (T) (object) EditorPrefs.GetString(editorPrefKey);
                }

                if (type == INT_TYPE)
                {
                    return (T) (object) EditorPrefs.GetInt(editorPrefKey);
                }

                if (type == COLOR_TYPE)
                {
                    string resultString = EditorPrefs.GetString(editorPrefKey);
                    Color result;
                    if (UnityEngine.ColorUtility.TryParseHtmlString(resultString, out result))
                        return (T) (object) result;
                }
            }

            return defaultValue;
        }

        private void SetValue(T value)
        {
            Type type = typeof(T);
            if (type.IsEnum)
            {
                EditorPrefs.SetInt(editorPrefKey, Convert.ToInt32(value));
            }

            if (type == BOOL_TYPE)
            {
                EditorPrefs.SetBool(editorPrefKey, (bool) (object)value);
            }

            if (type == STRING_TYPE)
            {
                EditorPrefs.SetString(editorPrefKey, (string) (object)value);
            }

            if (type == INT_TYPE)
            {
                EditorPrefs.SetInt(editorPrefKey, (int) (object)value);
            }

            if (type == COLOR_TYPE)
            {
                Color colorValue = (Color) (object) value;
                string colorString = "#" + UnityEngine.ColorUtility.ToHtmlStringRGBA(colorValue);
                EditorPrefs.SetString(editorPrefKey, colorString);
            }

            cachedValue = value;
        }
#endif
    }
}
