using System.Reflection;
using UnityEditor;

namespace BrunoMikoski.Core
{
    public class DevelopmentSetting
    {
        private string editorPrefKey;
        private readonly bool defaultValue;
#if !UNITY_EDITOR
        private bool runtimeValue = false;
#endif

        public DevelopmentSetting(bool defaultValue)
        {
            this.defaultValue = defaultValue;
#if !UNITY_EDITOR
            runtimeValue = defaultValue;
#endif
        }

#if UNITY_EDITOR
        private bool? cachedValue;

        public bool Value
        {
            get
            {
                if (!cachedValue.HasValue)
                {
                    cachedValue = EditorPrefs.GetBool(EditorPrefKey, defaultValue);
                }
                return cachedValue.Value;
            }
            set
            {
                cachedValue = value;
                EditorPrefs.SetBool(EditorPrefKey, value);
            }
        }
#else
       public bool Value
        {
            get { return runtimeValue; }
            set { runtimeValue = value; }
        }
#endif

        public static implicit operator bool(DevelopmentSetting item)
        {
            return item.Value;
        }

#if UNITY_EDITOR
        private string EditorPrefKey
        {
            get
            {
                if (editorPrefKey == null)
                {
                    FieldInfo[] fieldInfos = typeof(DevelopmentSettings).GetFields();
                    for (int i = 0; i < fieldInfos.Length; ++i)
                    {
                        if (fieldInfos[i].GetValue(null) == this)
                        {
                            editorPrefKey = "DevelopmentSetting." + fieldInfos[i].Name;
                            break;
                        }
                    }
                }
                return editorPrefKey;
            }
        }
#endif
    }
}
