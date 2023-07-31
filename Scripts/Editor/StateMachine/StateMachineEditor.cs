using System;
using System.Collections.Generic;
using BrunoMikoski.Core.Routines;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{
    [CustomEditor( typeof(StateMachine), true)]
    public class StateMachineEditor : UnityEditor.Editor
    {
        private StateMachine stateMachine;

        private float changeColorAnimation;
        private Routine animationRoutine;

        private List<StateHistory> stateHistory = new List<StateHistory>();

        private void OnEnable()
        {
            stateMachine = (StateMachine)target;

            if (Application.isPlaying)
            {
                stateMachine.OnStateChangedEvent += OnStateChange;
            }

            if (!PrefabUtility.IsPartOfAnyPrefab(target))
            {
                bool moveUp = true;
                while (moveUp)
                {
                    moveUp = UnityEditorInternal.ComponentUtility.MoveComponentUp(stateMachine);
                }
            }
        }

        private void OnDisable()
        {
            stateMachine.OnStateChangedEvent -= OnStateChange;
        }

        private void OnDestroy()
        {
            stateMachine.OnStateChangedEvent -= OnStateChange;
        }

        public override void OnInspectorGUI()
        {
            if ( !Application.isPlaying )
            {
                base.OnInspectorGUI();
                return;
            }

            EditorUtility.SetDirty( target );
            EditorGUILayout.Space();

            Color originalColor = GUI.color;
            GUI.color = Color.Lerp(originalColor, Color.red, changeColorAnimation);

            if ( stateMachine.PreviousState != null )
            {
                EditorGUILayout.BeginVertical( "box" );
                {
                    GUILayout.BeginHorizontal( EditorStyles.toolbar );
                    GUILayout.FlexibleSpace();
                    GUILayout.Label( "State History" );
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    for (int i = 0; i < stateHistory.Count; i++)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(
                            String.Format("[{0}]\t{1}", stateHistory[i].FrameNumber, stateHistory[i].StateName));
                        if (GUILayout.Button("Print StackTrace", GUILayout.Width(130)))
                            Debug.Log(
                                String.Format("{0}\n\n{1}",
                                    stateHistory[i].StateName,
                                    stateHistory[i].StackTrace ));
                        GUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginVertical( "box" );
            {
                GUILayout.BeginHorizontal( EditorStyles.toolbar );
                GUILayout.FlexibleSpace();
                GUILayout.Label( "Current State" );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if ( stateMachine.CurrentState != null )
                    GUILayout.Label( stateMachine.CurrentState.StateType.ToString() );
                else
                    GUILayout.Label( "Null" );
            }
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical( "box" );
            {
                GUILayout.BeginHorizontal( EditorStyles.toolbar );
                GUILayout.FlexibleSpace();
                GUILayout.Label( "Possible Next States " );
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (stateMachine.AvailableNextStates != null)
                {
                    foreach (Enum availableNextState in stateMachine.AvailableNextStates)
                    {
                        GUILayout.Label( availableNextState.ToString() );
                    }

                    foreach (Enum availableNextState in stateMachine.FromAnyTransitions)
                    {
                        GUILayout.Label(availableNextState.ToString());
                    }
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Slow Motion"))
                UnityEngine.Time.timeScale = 0.1f;
            if (GUILayout.Button("Normal Speed"))
                UnityEngine.Time.timeScale = 1f;
            GUILayout.EndHorizontal();

            GUI.color = originalColor;
        }

        private void OnStateChange(State current, State previous)
        {
            stateHistory.Add(new StateHistory(
                previous.StateType.ToString(),
                UnityEngine.Time.frameCount.ToString(),
                StackTraceUtility.ExtractStackTrace()));

            if (stateHistory.Count > 9)
                stateHistory.RemoveAt(0);

            CoroutineRunner.StopAndClear(ref animationRoutine);

            changeColorAnimation = 1;
        }

        private class StateHistory
        {
            public string StateName;
            public string FrameNumber;
            public string StackTrace;

            public StateHistory(string stateName, string frameNumber, string stackTrace)
            {
                StateName = stateName;
                FrameNumber = frameNumber;
                StackTrace = stackTrace;
            }
        }
    }
}
