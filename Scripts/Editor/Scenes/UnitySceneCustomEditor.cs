using UnityEditor;

namespace BrunoMikoski.Playground.ScenesReferences
{
    [CustomEditor(typeof(UnityScene), true)]
    public class UnitySceneCustomEditor : Editor
    {
        private UnityScene sceneID;
        private SerializedProperty guidProperty;

        private SceneAsset sceneAsset;

        private void OnEnable()
        {
            sceneID = (UnityScene) target;
            guidProperty = serializedObject.FindProperty("sceneGuid");
            
            if (!string.IsNullOrEmpty(sceneID.SceneGuid))
            {
                string path = AssetDatabase.GUIDToAssetPath(sceneID.SceneGuid);
                if (!string.IsNullOrEmpty(path))
                    sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

                if (string.IsNullOrEmpty(serializedObject.FindProperty("sceneName").stringValue))
                {
                    serializedObject.FindProperty("sceneName").stringValue = sceneAsset.name;
                    serializedObject.ApplyModifiedProperties();
                }

                if (string.IsNullOrEmpty(serializedObject.FindProperty("scenePath").stringValue))
                {
                    serializedObject.FindProperty("scenePath").stringValue = path;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            SceneAsset newSceneAsset = EditorGUILayout.ObjectField("Scene", sceneAsset, typeof(SceneAsset), false) as SceneAsset;

            if (EditorGUI.EndChangeCheck())
            {
                if (newSceneAsset != sceneAsset)
                {
                    sceneAsset = newSceneAsset;
                    if (sceneAsset != null)
                    {
                        string path = AssetDatabase.GetAssetPath(sceneAsset);
                        guidProperty.stringValue = AssetDatabase.AssetPathToGUID(path);
                        serializedObject.FindProperty("sceneName").stringValue = sceneAsset.name;
                        serializedObject.FindProperty("scenePath").stringValue = path;
                    }
                    else
                    {
                        guidProperty.stringValue = string.Empty;
                        serializedObject.FindProperty("sceneName").stringValue = string.Empty;
                        serializedObject.FindProperty("scenePath").stringValue = string.Empty;
                    }
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}