using System;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools
{
    // Define the custom serializable dictionary types
    [Serializable]
    public class StringStringDictionary : SerializableDictionary<string, string> { }

    [Serializable]
    public class StringIntDictionary : SerializableDictionary<string, int> { }

    [Serializable]
    public class StringBoolDictionary : SerializableDictionary<string, bool> { }

    [Serializable]
    public class StringFloatDictionary : SerializableDictionary<string, float> { }

    [Serializable]
    public class SaveData
    {
        [SerializeField]
        private int Version = 1;
        
        [SerializeField]
        private StringStringDictionary StringList = new();
        
        [SerializeField]
        private StringIntDictionary IntList = new();

        [SerializeField]
        private StringBoolDictionary BoolList = new();

        [SerializeField]
        private StringFloatDictionary FloatList = new();
        
        public string Serialise()
        {
            return JsonUtility.ToJson(this, Debug.isDebugBuild);
        }
        
        public void Deserialise(string data)
        {
            _ = Version;
            JsonUtility.FromJsonOverwrite(data, this);
        }
        
        public void SetString(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogWarning($"Setting empty or null string for key '{key}' in SaveDataService.");
                return;
            }
            
            StringList[key] = value;
        }
        
        public string GetString(string key)
        {
            if (StringList.TryGetValue(key, out string value))
            {
                return value;
            }

            Debug.LogWarning($"Key '{key}' not found in SaveDataService. Returning default value.");
            return string.Empty;
        }
        
        public void SetInt(string key, int value = default(int))
        {
            IntList[key] = value;
        }
        
        public int GetInt(string key, int defaultValue = default(int))
        {
            if (IntList.TryGetValue(key, out int value))
            {
                return value;
            }

            Debug.LogWarning($"Key '{key}' not found in SaveDataService. Returning default value.");
            return defaultValue;
        }

        public void SetFloat(string key, float value = default(float))
        {
            FloatList[key] = value;
        }

        public float GetFloat(string key, float defaultValue = default(float))
        {
            if (FloatList.TryGetValue(key, out float value))
            {
                return value;
            }

            Debug.LogWarning($"Key '{key}' not found in SaveDataService. Returning default value.");
            return defaultValue;
        }

        public void SetBool(string key, bool value = default(bool))
        {
            BoolList[key] = value;
        }

        public bool GetBool(string key, bool defaultValue = default(bool))
        {
            if (BoolList.TryGetValue(key, out bool value))
            {
                return value;
            }

            Debug.LogWarning($"Key '{key}' not found in SaveDataService. Returning default value.");
            return defaultValue;
        }

        public bool HasKey(string key)
        {
            return StringList.ContainsKey(key) ||
                   IntList.ContainsKey(key) ||
                   FloatList.ContainsKey(key) ||
                   BoolList.ContainsKey(key);
        }

        public void DeleteKey(string key)
        {
            StringList.Remove(key);
            IntList.Remove(key);
            FloatList.Remove(key);
            BoolList.Remove(key);
        }

        public void DeleteAll()
        {
            StringList.Clear();
            IntList.Clear();
            FloatList.Clear();
            BoolList.Clear();
        }
    }
}
