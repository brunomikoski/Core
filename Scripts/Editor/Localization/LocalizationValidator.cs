#if UNITY_LOCALIZATION_AVAILABLE
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;
using UnityEngine.Localization;

namespace BrunoMikoski.Core.Utils.Localization
{
    public class LocalizationValidator : EditorWindow
    {
        private static readonly Color ColorOk = new Color(0.35f, 0.75f, 0.35f);
        private static readonly Color ColorError = new Color(0.90f, 0.35f, 0.35f);

        private Locale _referenceLocale;
        private string _selectedTableName = "";
        private List<string> _availableTableNames = new List<string>();
        private int _selectedTableIndex = 0;

        private List<ValidationResult> _results = new List<ValidationResult>();
        private Vector2 _scrollPos;
        private bool _hasRun = false;
        private bool _isLoading = false;

        private class ValidationResult
        {
            public Locale Locale;
            public int ReferenceCount;
            public int LocaleCount;
            public List<string> MissingKeys = new List<string>();

            public bool IsComplete => MissingKeys.Count == 0;

            public bool ShowMissing = false;
            public bool ShowEmpty = false;
        }

        [MenuItem("Window/Asset Management/Localization Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalizationValidator>("Localization Validator");
            window.minSize = new Vector2(480, 400);
            window.RefreshTableList();
        }

        private void OnEnable()
        {
            RefreshTableList();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(6);
            DrawSettings();
            EditorGUILayout.Space(6);
            DrawRunButton();

            if (_isLoading)
            {
                EditorGUILayout.HelpBox("Validating tables, please wait…", MessageType.Info);
                return;
            }

            if (_hasRun)
            {
                EditorGUILayout.Space(4);
                DrawResults();
            }
        }

        private void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(4, 0, 8, 4)
            };
            EditorGUILayout.LabelField("🌐  Localization String Validator", headerStyle);
            DrawHorizontalLine();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _referenceLocale = (Locale)EditorGUILayout.ObjectField(
                new GUIContent("Reference Locale", "Locale used as the source of truth (e.g. English)"),
                _referenceLocale, typeof(Locale), false);

            EditorGUILayout.BeginHorizontal();
            if (_availableTableNames.Count == 0)
            {
                EditorGUILayout.HelpBox("No String Tables found in the project.", MessageType.Warning);
            }
            else
            {
                _selectedTableIndex = EditorGUILayout.Popup(
                    new GUIContent("String Table", "The string table collection to validate"),
                    _selectedTableIndex, _availableTableNames.ToArray());

                _selectedTableName = _availableTableNames[_selectedTableIndex];
            }

            if (GUILayout.Button("↺", GUILayout.Width(28), GUILayout.Height(18)))
                RefreshTableList();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRunButton()
        {
            GUI.enabled = _referenceLocale != null && _availableTableNames.Count > 0 && !_isLoading;

            var buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
            if (GUILayout.Button("Validate", buttonStyle, GUILayout.Height(30)))
                RunValidation();

            GUI.enabled = true;
        }

        private void DrawResults()
        {
            DrawHorizontalLine();
            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var result in _results)
                DrawLocaleResult(result);

            EditorGUILayout.EndScrollView();
        }

        private void DrawLocaleResult(ValidationResult result)
        {
            var boxStyle = new GUIStyle(GUI.skin.box) { padding = new RectOffset(8, 8, 6, 6) };
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.BeginHorizontal();

            Color dotColor = result.IsComplete ? ColorOk : ColorError;

            var dotStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = dotColor } };
            EditorGUILayout.LabelField("●", dotStyle, GUILayout.Width(16));
            EditorGUILayout.LabelField(result.Locale.LocaleName, EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            string countLabel = $"{result.LocaleCount} / {result.ReferenceCount} entries";
            EditorGUILayout.LabelField(countLabel, GUILayout.Width(130));
            EditorGUILayout.EndHorizontal();

            if (result.IsComplete)
            {
                var okStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = ColorOk } };
                EditorGUILayout.LabelField("✔  All entries present and non-empty", okStyle);
            }
            else if (result.MissingKeys.Count > 0)
            {
                var foldStyle = new GUIStyle(EditorStyles.foldout)
                    { normal = { textColor = ColorError }, onNormal = { textColor = ColorError } };

                result.ShowMissing = EditorGUILayout.Foldout(result.ShowMissing,
                                                             $"Missing keys ({result.MissingKeys.Count})", true, foldStyle);

                if (result.ShowMissing)
                {
                    EditorGUI.indentLevel++;
                    foreach (var key in result.MissingKeys)
                        EditorGUILayout.LabelField($"• {key}", EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private async void RunValidation()
        {
            try
            {
                _results.Clear();
                _hasRun = false;
                _isLoading = true;
                Repaint();

                await LocalizationSettings.InitializationOperation.Task;

                var referenceOp = LocalizationSettings.StringDatabase.GetTableAsync(_selectedTableName, _referenceLocale);
                await referenceOp.Task;
                StringTable referenceTable = referenceOp.Result;

                if (referenceTable == null)
                {
                    Debug.LogError($"[LocalizationValidator] Could not load table '{_selectedTableName}' for reference locale '{_referenceLocale.LocaleName}'.");
                    _isLoading = false;
                    Repaint();
                    return;
                }

                var referenceKeys = new List<string>();
                foreach (var entry in referenceTable)
                    referenceKeys.Add(entry.Value.Key);

                var locales = LocalizationSettings.AvailableLocales.Locales;
                foreach (var locale in locales)
                {
                    if (locale == _referenceLocale) continue;

                    var op = LocalizationSettings.StringDatabase.GetTableAsync(_selectedTableName, locale);
                    await op.Task;
                    StringTable table = op.Result;

                    var result = new ValidationResult
                    {
                        Locale = locale,
                        ReferenceCount = referenceKeys.Count,
                        LocaleCount = table != null ? table.Count : 0
                    };

                    if (table == null)
                    {
                        result.MissingKeys.AddRange(referenceKeys);
                    }
                    else
                    {
                        foreach (var key in referenceKeys)
                        {
                            var entry = table.GetEntry(key);
                            if (entry == null || string.IsNullOrWhiteSpace(entry.LocalizedValue))
                                result.MissingKeys.Add(key);
                        }
                    }

                    _results.Add(result);
                }

                _isLoading = false;
                _hasRun = true;
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"FAILED to Run the validator with error {e.Message}");
            }
        }

        private void RefreshTableList()
        {
            _availableTableNames.Clear();

            var collections = LocalizationEditorSettings.GetStringTableCollections();
            foreach (var col in collections)
                _availableTableNames.Add(col.TableCollectionName);

            _selectedTableIndex = 0;
            if (_availableTableNames.Count > 0)
                _selectedTableName = _availableTableNames[0];

            Repaint();
        }

        private static void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }

    }
}
#endif
