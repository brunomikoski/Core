#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace BrunoMikoski.Core.DevelopmentTools.ScenesReferences
{
    public class GameplayUnityScene : UnityScene
    {
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            SerializedObject serializedObject = new SerializedObject(this);
            SerializedProperty isNetworkedScene = serializedObject.FindProperty("isNetworkedScene");
            if (!isNetworkedScene.boolValue)
            {
                isNetworkedScene.boolValue = true;
                Debug.LogWarning($"GameplayUnityScene {name} is a Gameplay scene and should be marked as Networked. Automatically marking it as Networked.");
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
