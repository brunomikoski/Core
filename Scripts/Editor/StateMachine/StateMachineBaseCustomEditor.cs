using System;
using System.Collections.Generic;
using BrunoMikoski.Core.Routines;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{
    [CustomEditor(typeof(StateMachineBase), true)]
    public class StateMachineBaseCustomEditor : UnityEditor.Editor
    {
        private StateMachineBase stateMachine;

        private float changeColorAnimation;
        private EditorCoroutine animationRoutine;

        private GUIStyle labelStyle;

        private Dictionary<int, bool> expandedStatus = new();

        private void OnEnable()
        {
            if (target == null)
                return;

            stateMachine = (StateMachineBase) target;

            if (!PrefabUtility.IsPartOfAnyPrefab(target))
            {
                bool moveUp = true;
                while (moveUp)
                {
                    moveUp = UnityEditorInternal.ComponentUtility.MoveComponentUp(stateMachine);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (!Application.isPlaying)
            {
                base.OnInspectorGUI();
                return;
            }

            EditorUtility.SetDirty(target);
            EditorGUILayout.Space();

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true
                };
            }

            if (!StateMachineTracker.TryGetStateMachineState(stateMachine, out StateMachineState stateMachineState))
            {
                base.OnInspectorGUI();
                return;
            }


            Color originalColor = GUI.color;
            GUI.color = Color.Lerp(originalColor, Color.red, changeColorAnimation);


            EditorGUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.FlexibleSpace();
                GUILayout.Label("State History");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                for (int i = 0; i < stateMachineState.History.Count; i++)
                {
                    StepData historyData = stateMachineState.History[i];
                    DrawStepData(historyData);
                    EditorGUILayout.Separator();
                }
            }
            EditorGUILayout.EndVertical();


            if (stateMachineState.CurrentTransition == null)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal(EditorStyles.toolbar);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Current State");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    DrawStepData(stateMachineState.CurrentState);
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal(EditorStyles.toolbar);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Current Transition");
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    DrawStepData(stateMachineState.CurrentTransition);
                }
                EditorGUILayout.EndVertical();
            }


            EditorGUILayout.BeginVertical("box");
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Possible Next States ");
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                foreach (string availableNextState in stateMachineState.AvailableNextStates)
                {
                    GUILayout.Label(availableNextState);
                }
            }
            EditorGUILayout.EndVertical();

            GUI.color = originalColor;
        }



        private void DrawStepData(StepData stepData)
        {
            string displayName;
            if (stepData is StateData)
            {
                displayName = $"[State] - {stepData.DisplayName}";
            }
            else
            {
                displayName = $"[Transition] - {stepData.DisplayName}";
            }

            GUILayout.BeginVertical();
            EditorGUI.indentLevel++;
            SetExpanded(stepData, EditorGUILayout.Foldout(IsExpanded(stepData), displayName));
            EditorGUI.indentLevel--;


            if (IsExpanded(stepData))
            {
                EditorGUI.indentLevel++;
                string elapsed = "";
                if (stepData.ExitTime != default)
                {
                    elapsed =  (stepData.ExitTime - stepData.EnterTime).TotalSeconds.ToString("00.00");
                }
                else
                {
                    elapsed =  (DateTime.Now - stepData.EnterTime).TotalSeconds.ToString("00.00");
                }


                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Duration");
                GUI.enabled = false;
                EditorGUILayout.TextField($"{elapsed} seconds");
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    bool hasExitData  = stepData.ExitStackTrace != null;
                    GUIStyle leftButtonStyle = EditorStyles.miniButtonLeft;
                    if (!hasExitData)
                        leftButtonStyle = EditorStyles.miniButton;

                    EditorGUI.indentLevel++;

                    if (GUILayout.Button("Log Enter Stack Trace", leftButtonStyle))
                    {
                        Debug.Log(stepData.EnterStackTrace.CleanupAsyncStackTrace());
                    }

                    if (hasExitData)
                    {
                        if(GUILayout.Button("Log Exit Stack Trace", EditorStyles.miniButtonRight))
                        {
                            Debug.Log(stepData.ExitStackTrace.CleanupAsyncStackTrace());
                        }
                    }
                    EditorGUI.indentLevel--;

                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }
            GUILayout.EndVertical();
        }

        private bool IsExpanded(object targetObject)
        {
            if (!expandedStatus.TryGetValue(targetObject.GetHashCode(), out bool expanded))
                return false;

            return expanded;
        }

        private void SetExpanded(object targetObject, bool foldout)
        {
            expandedStatus[targetObject.GetHashCode()] = foldout;
        }
    }
}