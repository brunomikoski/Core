using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{
    public class CreateStateMachineWizard : EditorWindow
    {
        private static CreateStateMachineWizard windowInstance;
        private static string targetFolderPath;
        private bool createGlobalStateMachine;

        private string stateMachineName;
        private string stateName;
        private string stateCollectionName;
        private string targetNamespace;
        private static string SCRIPTABLE_OBJECT_FOLDER_PATH;
        private static string SCRIPTS_FOLDER_PATH;
        private DefaultAsset scriptableObjectFolder;
        private DefaultAsset scriptsObjectFolder;

        [MenuItem("Assets/Create/State Machine/New State Machine", false, 0)]
        private static void CreateNewItem()
        {
            string targetPath = "";
            if (Selection.objects.FirstOrDefault() is DefaultAsset folder)
                targetPath = AssetDatabase.GetAssetPath(folder);
            
            Show(targetPath);
        }
        
        private static CreateStateMachineWizard GetWindowInstance()
        {
            if (windowInstance == null)
            {
                windowInstance =  CreateInstance<CreateStateMachineWizard>();
                windowInstance.titleContent = new GUIContent("Create State Machine");
                windowInstance.minSize = new Vector2(EditorGUIUtility.labelWidth + EditorGUIUtility.labelWidth + 75, 370);
            }

            return windowInstance;
        }


        private void OnEnable()
        {
            // targetNamespace = CompilationPipeline.GetAssemblyRootNamespaceFromScriptPath(pathName);
        }

        public static void Show(string targetPath)
        {
            targetFolderPath = targetPath;
            SCRIPTABLE_OBJECT_FOLDER_PATH = targetFolderPath;
            GetWindowInstance().ShowUtility();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();


            DrawSettings();
            EditorGUILayout.Space();
            DrawScriptableObjects();
            EditorGUILayout.Space();

            DrawScriptsObjects();
        }
        
        private void DrawScriptsObjects()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.BeginFoldoutHeaderGroup(true, "Scripts");

                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                {
                    EditorGUILayout.LabelField(SCRIPTS_FOLDER_PATH, EditorStyles.miniLabel);

                    if (!string.IsNullOrEmpty(SCRIPTS_FOLDER_PATH) && scriptableObjectFolder == null)
                    {
                        scriptsObjectFolder =
                            AssetDatabase.LoadAssetAtPath<DefaultAsset>(SCRIPTS_FOLDER_PATH);
                    }
                        
                    scriptsObjectFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                        "Folder",
                        scriptsObjectFolder, typeof(DefaultAsset),
                        false);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (scriptsObjectFolder == null)
                        {
                            SCRIPTS_FOLDER_PATH = "";
                        }
                        else
                        {
                            SCRIPTS_FOLDER_PATH = AssetDatabase.GetAssetPath(scriptsObjectFolder);

                            if (string.IsNullOrEmpty(targetNamespace))
                            {
                                targetNamespace =
                                    CompilationPipeline.GetAssemblyRootNamespaceFromScriptPath(SCRIPTS_FOLDER_PATH);
                            }
                        }
                    }
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawScriptableObjects()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.BeginFoldoutHeaderGroup(true, "ScriptableObjects");

                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                {
                    EditorGUILayout.LabelField(SCRIPTABLE_OBJECT_FOLDER_PATH, EditorStyles.miniLabel);

                    if (!string.IsNullOrEmpty(SCRIPTABLE_OBJECT_FOLDER_PATH) && scriptableObjectFolder == null)
                    {
                        scriptableObjectFolder =
                            AssetDatabase.LoadAssetAtPath<DefaultAsset>(SCRIPTABLE_OBJECT_FOLDER_PATH);
                    }
                        
                    scriptableObjectFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                        "Folder",
                        scriptableObjectFolder, typeof(DefaultAsset),
                        false);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (scriptableObjectFolder == null)
                        {
                            SCRIPTABLE_OBJECT_FOLDER_PATH = "";
                        }
                        else
                        {
                            SCRIPTABLE_OBJECT_FOLDER_PATH = AssetDatabase.GetAssetPath(scriptableObjectFolder);
                        }
                    }
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSettings()
        {
            using (new EditorGUILayout.VerticalScope("Box"))
            {
                EditorGUILayout.BeginFoldoutHeaderGroup(true, "Settings");

                EditorGUI.indentLevel++;
                createGlobalStateMachine = EditorGUILayout.Toggle("Global State Machine", createGlobalStateMachine);
                if (createGlobalStateMachine)
                {
                    EditorGUILayout.HelpBox(
                        @"Global State Machines are exclusive, you cannot have more than one running at the same time, but gives you the ability to subscribe to state events directly from the GlobalStateID objects itself",
                        MessageType.Info);
                }
                
                EditorGUILayout.Space();
                stateMachineName = EditorGUILayout.TextField("State Machine Name", stateMachineName);
                EditorGUILayout.Space();
                stateName = EditorGUILayout.TextField("State Name", stateName);
                stateCollectionName = EditorGUILayout.TextField("State Collection Name", stateCollectionName);
                EditorGUILayout.Space();
                targetNamespace = EditorGUILayout.TextField("Namespace", targetNamespace);


                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }
        }
    }
}