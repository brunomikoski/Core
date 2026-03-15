using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Object = UnityEngine.Object;
using BrunoMikoski.ScriptableObjectCollections;

namespace BrunoMikoski.Core.StateMachines
{
    public class CreateStateMachineWizard : EditorWindow
    {
        private const string WAITING_RECOMPILE_KEY = "CreateStateMachineWizard_WaitingRecompile";
        private const string LAST_COLLECTION_PATH_KEY = "CreateStateMachineWizard_CollectionPath";
        private const string LAST_COLLECTION_FULL_NAME_KEY = "CreateStateMachineWizard_CollectionFullName";
        private const string LAST_STATE_ID_FULL_NAME_KEY = "CreateStateMachineWizard_StateIDFullName";
        private const string LAST_NAMESPACE_KEY = "CreateStateMachineWizard_Namespace";
        private const string LAST_STATE_NAMES_KEY = "CreateStateMachineWizard_StateNames";
        private const string LAST_INITIAL_STATE_INDEX_KEY = "CreateStateMachineWizard_InitialStateIndex";
        private const string LAST_SCRIPTS_FOLDER_KEY = "CreateStateMachineWizard_ScriptsFolder";

        private static CreateStateMachineWizard windowInstance;
        private static string targetFolderPath;
        private bool createGlobalStateMachine;
        private bool createCustomStateIDType = true;
        private bool createClassPerState;
        private bool createClassPerTransition;

        private string stateMachineName;
        private string stateCollectionName;
        private string targetNamespace;
        [SerializeField] private List<string> stateNamesList = new List<string>();
        private string newStateNameInput = "";
        private int initialStateIndex = 0;

        private static string SCRIPTABLE_OBJECT_FOLDER_PATH;
        private static string SCRIPTS_FOLDER_PATH;
        private DefaultAsset scriptableObjectFolder;
        private DefaultAsset scriptsObjectFolder;

        private string warningText;
        private Vector2 scrollPosition;

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
                windowInstance = CreateInstance<CreateStateMachineWizard>();
                windowInstance.titleContent = new GUIContent("Create State Machine");
                windowInstance.minSize = new Vector2(EditorGUIUtility.labelWidth * 2 + 75, 480);
            }

            return windowInstance;
        }

        public static void Show(string targetPath)
        {
            targetFolderPath = targetPath;
            SCRIPTABLE_OBJECT_FOLDER_PATH = targetFolderPath;
            GetWindowInstance().ShowUtility();
        }

        private void OnEnable()
        {
            windowInstance = this;
            if (stateNamesList == null || stateNamesList.Count == 0)
            {
                stateNamesList = new List<string> { "Idle", "Attack", "Damaged", "Dead" };
                initialStateIndex = 0;
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space();
            DrawSettings();
            EditorGUILayout.Space();
            DrawScriptableObjects();
            EditorGUILayout.Space();
            DrawScriptsObjects();
            EditorGUILayout.Space();

            bool isValid = CheckValidity(out warningText);
            if (!isValid)
                EditorGUILayout.HelpBox(warningText, MessageType.Warning);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!isValid);
            if (GUILayout.Button("Create", GUILayout.Height(35)))
                CreateStateMachine();
            EditorGUI.EndDisabledGroup();
        }

        private string GetSanitizedStateMachineName()
        {
            if (string.IsNullOrWhiteSpace(stateMachineName))
                return "";
            return stateMachineName.Replace(" ", "").Replace("-", "").Replace("_", "");
        }

        private string[] GetStateNames()
        {
            if (stateNamesList == null || stateNamesList.Count == 0)
                return Array.Empty<string>();
            return stateNamesList
                .Select(s => s != null ? s.Trim() : "")
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s.Sanitize())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToArray();
        }

        private bool CheckValidity(out string message)
        {
            message = "";
            string name = GetSanitizedStateMachineName();
            if (string.IsNullOrEmpty(name))
            {
                message = "State Name is required.";
                return false;
            }

            string[] stateNames = GetStateNames();
            if (stateNames.Length == 0)
            {
                message = "Enter at least one state name (comma-separated).";
                return false;
            }

            if (string.IsNullOrEmpty(SCRIPTS_FOLDER_PATH) || AssetDatabase.LoadAssetAtPath<DefaultAsset>(SCRIPTS_FOLDER_PATH) == null)
            {
                message = "Scripts folder must be set and valid.";
                return false;
            }

            if (string.IsNullOrEmpty(SCRIPTABLE_OBJECT_FOLDER_PATH) || AssetDatabase.LoadAssetAtPath<DefaultAsset>(SCRIPTABLE_OBJECT_FOLDER_PATH) == null)
            {
                message = "Scriptable Object folder must be set and valid.";
                return false;
            }

            if (initialStateIndex >= stateNames.Length)
                initialStateIndex = 0;

            return true;
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
                        "Global State Machines are exclusive. You cannot have more than one running at the same time, but you can subscribe to state events directly from the GlobalStateID objects.",
                        MessageType.Info);
                }

                EditorGUILayout.Space();
                stateMachineName = EditorGUILayout.TextField("State Name", stateMachineName);
                if (string.IsNullOrEmpty(stateCollectionName) && !string.IsNullOrEmpty(GetSanitizedStateMachineName()))
                    stateCollectionName = GetSanitizedStateMachineName() + "States";
                stateCollectionName = EditorGUILayout.TextField("State Collection Name", stateCollectionName);
                targetNamespace = EditorGUILayout.TextField("Namespace", targetNamespace);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("States", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                newStateNameInput = EditorGUILayout.TextField("New state", newStateNameInput);
                EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(newStateNameInput));
                if (GUILayout.Button("+", GUILayout.Width(24)))
                {
                    string sanitized = newStateNameInput.Trim().Sanitize();
                    if (!string.IsNullOrEmpty(sanitized) && (stateNamesList == null || !stateNamesList.Any(s => s != null && s.Sanitize().Equals(sanitized, StringComparison.OrdinalIgnoreCase))))
                    {
                        if (stateNamesList == null) stateNamesList = new List<string>();
                        stateNamesList.Add(newStateNameInput.Trim());
                        newStateNameInput = "";
                        GUI.FocusControl(null);
                    }
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();

                if (stateNamesList != null && stateNamesList.Count > 0)
                {
                    initialStateIndex = Mathf.Clamp(initialStateIndex, 0, stateNamesList.Count - 1);
                    for (int i = 0; i < stateNamesList.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        stateNamesList[i] = EditorGUILayout.TextField(stateNamesList[i]);
                        if (GUILayout.Button("-", GUILayout.Width(24)))
                        {
                            stateNamesList.RemoveAt(i);
                            if (initialStateIndex > i)
                                initialStateIndex--;
                            else if (initialStateIndex >= stateNamesList.Count)
                                initialStateIndex = Mathf.Max(0, stateNamesList.Count - 1);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    string[] stateNames = GetStateNames();
                    if (stateNames.Length > 0)
                    {
                        initialStateIndex = Mathf.Clamp(initialStateIndex, 0, stateNames.Length - 1);
                        initialStateIndex = EditorGUILayout.Popup("Initial State", initialStateIndex, stateNames);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Add at least one state using the field above and the + button.", MessageType.Info);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Code generation", EditorStyles.boldLabel);
                createCustomStateIDType = EditorGUILayout.Toggle("Create custom StateID type", createCustomStateIDType);
                if (createCustomStateIDType)
                {
                    EditorGUILayout.HelpBox(
                        "Generates a partial class extending " + (createGlobalStateMachine ? "GlobalStateID" : "StateID") + ". Use this type for the state machine instead of the base StateID.",
                        MessageType.None);
                }

                createClassPerState = EditorGUILayout.Toggle("Create class per state", createClassPerState);
                if (createClassPerState)
                {
                    EditorGUILayout.HelpBox(
                        "Generates one state class per state (e.g. IdleState, AttackState) extending the state base.",
                        MessageType.None);
                }

                createClassPerTransition = EditorGUILayout.Toggle("Create class per transition", createClassPerTransition);
                if (createClassPerTransition)
                {
                    EditorGUILayout.HelpBox(
                        "Generates one transition class per from-to pair (e.g. FromIdleToAttackTransition).",
                        MessageType.None);
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }
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

                    if (!string.IsNullOrEmpty(SCRIPTS_FOLDER_PATH) && scriptsObjectFolder == null)
                        scriptsObjectFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(SCRIPTS_FOLDER_PATH);

                    scriptsObjectFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                        "Folder",
                        scriptsObjectFolder, typeof(DefaultAsset),
                        false);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (scriptsObjectFolder == null)
                            SCRIPTS_FOLDER_PATH = "";
                        else
                        {
                            SCRIPTS_FOLDER_PATH = AssetDatabase.GetAssetPath(scriptsObjectFolder);
                            if (string.IsNullOrEmpty(targetNamespace))
                                targetNamespace = CompilationPipeline.GetAssemblyRootNamespaceFromScriptPath(SCRIPTS_FOLDER_PATH);
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
                        scriptableObjectFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(SCRIPTABLE_OBJECT_FOLDER_PATH);

                    scriptableObjectFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                        "Folder",
                        scriptableObjectFolder, typeof(DefaultAsset),
                        false);

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (scriptableObjectFolder == null)
                            SCRIPTABLE_OBJECT_FOLDER_PATH = "";
                        else
                            SCRIPTABLE_OBJECT_FOLDER_PATH = AssetDatabase.GetAssetPath(scriptableObjectFolder);
                    }
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
                EditorGUI.indentLevel--;
            }
        }

        private void CreateStateMachine()
        {
            string baseName = GetSanitizedStateMachineName();
            string ns = string.IsNullOrWhiteSpace(targetNamespace) ? "" : targetNamespace.Trim();
            string[] stateNames = GetStateNames();
            string scriptsFolder = SCRIPTS_FOLDER_PATH.ToPathWithConsistentSeparators();
            string soFolder = SCRIPTABLE_OBJECT_FOLDER_PATH.ToPathWithConsistentSeparators();

            string stateIDTypeName = baseName + "StateID";
            string stateBaseName = baseName + "StateBase";
            string transitionBaseName = baseName + "TransitionBase";
            string collectionClassName = baseName + "StateCollection";
            string stateMachineClassName = baseName + "StateMachine";

            bool anyGenerated = false;

            // 1. StateID (custom type) or we use StateID from core
            if (createCustomStateIDType)
            {
                string baseType = createGlobalStateMachine ? "GlobalStateID" : "StateID";
                anyGenerated |= CodeGenerationUtility.CreateNewScript(
                    stateIDTypeName,
                    scriptsFolder,
                    ns,
                    "",
                    $"public partial class {stateIDTypeName} : {baseType}",
                    null,
                    typeof(StateID).Namespace,
                    "UnityEngine");
            }
            else
            {
                stateIDTypeName = "StateID";
            }

            string stateIDFullNameForStorage = createCustomStateIDType
                ? (string.IsNullOrEmpty(ns) ? stateIDTypeName : ns + "." + stateIDTypeName)
                : "BrunoMikoski.Core.StateMachines.StateID";

            // 2. State base
            anyGenerated |= CodeGenerationUtility.CreateNewScript(
                stateBaseName,
                scriptsFolder,
                ns,
                "",
                $"public class {stateBaseName} : State<{stateIDTypeName}>",
                new[] { $"public {stateBaseName}({stateIDTypeName} targetStateID) : base(targetStateID) {{ }}" },
                typeof(StateID).Namespace,
                "UnityEngine");

            // 3. Per-state classes (in States subfolder when enabled): {StateName}{StateMachineName}State (e.g. IdleHeroStateMachineState)
            if (createClassPerState)
            {
                string statesFolder = Path.Combine(scriptsFolder, "States");
                foreach (string stateName in stateNames)
                {
                    string className = stateName + baseName + "State";
                    anyGenerated |= CodeGenerationUtility.CreateNewScript(
                        className,
                        statesFolder,
                        ns,
                        "",
                        $"public class {className} : {stateBaseName}",
                        new[]
                        {
                            $"public {className}({stateIDTypeName} targetStateID) : base(targetStateID) {{ }}",
                            "protected override void Enter() { base.Enter(); }",
                            "protected override void Exit() { base.Exit(); }"
                        },
                        typeof(StateID).Namespace,
                        "UnityEngine");
                }
            }

            // 4. Transition base
            anyGenerated |= CodeGenerationUtility.CreateNewScript(
                transitionBaseName,
                scriptsFolder,
                ns,
                "",
                $"public class {transitionBaseName} : Transition<{stateIDTypeName}>",
                new[]
                {
                    $"public {transitionBaseName}({stateIDTypeName} fromState, {stateIDTypeName} toState) : base(fromState, toState) {{ }}",
                    "public override void Start() { ReportFinished(); }"
                },
                typeof(Transition<>).Namespace,
                "UnityEngine");

            // 5. Per-transition classes (in Transitions subfolder when enabled)
            if (createClassPerTransition)
            {
                string transitionsFolder = Path.Combine(scriptsFolder, "Transitions");
                for (int i = 0; i < stateNames.Length; i++)
                {
                    for (int j = 0; j < stateNames.Length; j++)
                    {
                        if (i == j) continue;
                        string from = stateNames[i];
                        string to = stateNames[j];
                        string className = "From" + from + "To" + to + "Transition";
                        anyGenerated |= CodeGenerationUtility.CreateNewScript(
                            className,
                            transitionsFolder,
                            ns,
                            "",
                            $"public class {className} : {transitionBaseName}",
                            new[]
                            {
                                $"public {className}({stateIDTypeName} fromState, {stateIDTypeName} toState) : base(fromState, toState) {{ }}",
                                "public override void Start() { base.Start(); }"
                            },
                            typeof(Transition<>).Namespace,
                            "UnityEngine");
                    }
                }
            }

            // 6. State machine class
            anyGenerated |= CodeGenerationUtility.CreateNewScript(
                stateMachineClassName,
                scriptsFolder,
                ns,
                "",
                $"public class {stateMachineClassName} : StateMachine<{stateIDTypeName}>",
                null,
                typeof(StateMachine<>).Namespace,
                "UnityEngine");

            // 7. State collection class
            anyGenerated |= CodeGenerationUtility.CreateNewScript(
                collectionClassName,
                scriptsFolder,
                ns,
                "",
                $"public class {collectionClassName} : ScriptableObjectCollection<{stateIDTypeName}>",
                null,
                "BrunoMikoski.ScriptableObjectCollections",
                "UnityEngine");

            // Store data for after recompile
            string collectionPath = Path.Combine(soFolder, stateCollectionName + ".asset");
            string collectionDir = Path.GetDirectoryName(collectionPath);
            if (!string.IsNullOrEmpty(collectionDir))
                collectionPath = Path.Combine(collectionDir, stateCollectionName + ".asset");

            EditorPrefs.SetBool(WAITING_RECOMPILE_KEY, true);
            EditorPrefs.SetString(LAST_COLLECTION_PATH_KEY, Path.Combine(soFolder, stateCollectionName + ".asset"));
            EditorPrefs.SetString(LAST_COLLECTION_FULL_NAME_KEY, string.IsNullOrEmpty(ns) ? collectionClassName : ns + "." + collectionClassName);
            EditorPrefs.SetString(LAST_STATE_ID_FULL_NAME_KEY, stateIDFullNameForStorage);
            EditorPrefs.SetString(LAST_NAMESPACE_KEY, ns);
            EditorPrefs.SetString(LAST_STATE_NAMES_KEY, string.Join(",", stateNames));
            EditorPrefs.SetInt(LAST_INITIAL_STATE_INDEX_KEY, initialStateIndex);
            EditorPrefs.SetString(LAST_SCRIPTS_FOLDER_KEY, scriptsFolder);

            AssetDatabase.Refresh();

            if (!anyGenerated)
                AfterScriptsAreReady();
        }

        private static void AfterScriptsAreReady()
        {
            if (!EditorPrefs.GetBool(WAITING_RECOMPILE_KEY, false))
                return;

            EditorPrefs.SetBool(WAITING_RECOMPILE_KEY, false);

            string collectionPath = EditorPrefs.GetString(LAST_COLLECTION_PATH_KEY, "");
            string collectionFullName = EditorPrefs.GetString(LAST_COLLECTION_FULL_NAME_KEY, "");
            string stateIDFullName = EditorPrefs.GetString(LAST_STATE_ID_FULL_NAME_KEY, "");
            string stateNamesStr = EditorPrefs.GetString(LAST_STATE_NAMES_KEY, "");
            int initialIndex = EditorPrefs.GetInt(LAST_INITIAL_STATE_INDEX_KEY, 0);
            string scriptsFolder = EditorPrefs.GetString(LAST_SCRIPTS_FOLDER_KEY, "");

            if (string.IsNullOrEmpty(collectionFullName) || string.IsNullOrEmpty(stateIDFullName))
                return;

            string collectionClassName = collectionFullName.Contains(".") ? collectionFullName.Substring(collectionFullName.LastIndexOf('.') + 1) : collectionFullName;
            string scriptPath = Path.Combine(scriptsFolder, collectionClassName + ".cs");
            string assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(scriptPath);
            if (string.IsNullOrEmpty(assemblyName))
                assemblyName = "Assembly-CSharp";

            Type collectionType = Type.GetType($"{collectionFullName}, {assemblyName}");
            Type stateIDType = stateIDFullName == "BrunoMikoski.Core.StateMachines.StateID" || stateIDFullName == "StateID"
                ? typeof(StateID)
                : Type.GetType($"{stateIDFullName}, {assemblyName}");

            if (collectionType == null || stateIDType == null)
            {
                Debug.LogWarning("CreateStateMachineWizard: Could not resolve types after recompile. Collection or StateID type not found.");
                return;
            }

            string folderPath = Path.GetDirectoryName(collectionPath).ToPathWithConsistentSeparators();
            string collectionAssetName = Path.GetFileNameWithoutExtension(collectionPath);

            AssetDatabaseUtils.CreatePathIfDoesntExist(Path.Combine(folderPath, "Items"));

            ScriptableObject collectionAsset = ScriptableObject.CreateInstance(collectionType);
            collectionAsset.name = collectionAssetName;
            string fullAssetPath = Path.Combine(folderPath, collectionAssetName + ".asset");
            AssetDatabase.CreateAsset(collectionAsset, fullAssetPath);

            var collection = collectionAsset as ScriptableObjectCollection;
            if (collection != null && stateIDType != null)
            {
                string[] stateNames = stateNamesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < stateNames.Length; i++)
                {
                    string stateName = stateNames[i].Trim();
                    if (string.IsNullOrEmpty(stateName)) continue;
                    ScriptableObject newItem = collection.AddNew(stateIDType, stateName);
                    if (newItem != null && i == initialIndex)
                    {
                        var so = new SerializedObject(newItem);
                        SerializedProperty prop = so.FindProperty("initialState");
                        if (prop != null)
                        {
                            prop.boolValue = true;
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(EditorPrefs.GetString(LAST_NAMESPACE_KEY, "")))
            {
                try
                {
                    var socSettings = SOCSettings.Instance;
                    if (socSettings != null)
                        socSettings.SetNamespaceForCollection(collection, EditorPrefs.GetString(LAST_NAMESPACE_KEY));
                }
                catch { /* optional */ }
            }

            Selection.objects = new Object[] { collectionAsset };
            EditorGUIUtility.PingObject(collectionAsset);

            if (windowInstance != null)
            {
                try
                {
                    GetWindow<CreateStateMachineWizard>()?.Close();
                }
                catch { }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private class StateMachineWizardAssetPostProcessor : AssetPostprocessor
        {
            [UsedImplicitly]
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
            {
                if (!didDomainReload)
                    return;
                AfterScriptsAreReady();
            }
        }
    }
}
