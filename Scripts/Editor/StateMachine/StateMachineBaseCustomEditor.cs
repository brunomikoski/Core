using System;
using System.Collections;
using System.Collections.Generic;
using BrunoMikoski.Core.Routines;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.StateMachines
{
    [CustomEditor( typeof(StateMachineBase), true)]
    public class StateMachineBaseCustomEditor : UnityEditor.Editor
    {
        private StateMachineBase stateMachine;

        private float changeColorAnimation;
        private EditorCoroutine animationRoutine;

        private List<StateHistory> stateHistory = new();
        private List<StateID> availableNextStates = new();

        private void OnEnable()
        {
            stateMachine = (StateMachineBase) target;

            if (Application.isPlaying)
            {
                stateMachine.OnStateChangedEvent -= OnStateChange;
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
                            $"[{stateHistory[i].FrameNumber}]\t{stateHistory[i].StateName}");
                        if (GUILayout.Button("Print StackTrace", GUILayout.Width(130)))
                            Debug.Log($"{stateHistory[i].StateName}\n\n{stateHistory[i].StackTrace}");
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
                    GUILayout.Label( stateMachine.CurrentState.StateID.ToString() );
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

                UpdateAvailableNextStates();

                for (int i = 0; i < availableNextStates.Count; i++)
                {
                    StateID availableNextState = availableNextStates[i];
                    GUILayout.Label(availableNextState.ToString());
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

        private void UpdateAvailableNextStates()
        {
            availableNextStates.Clear();
            for (int i = 0; i < stateMachine.AvailableTransitions.Count; i++)
            {
                TransitionBase availableTransition = stateMachine.AvailableTransitions[i];
                if (availableTransition.FromState == stateMachine.CurrentState.StateID)
                {
                    if (availableNextStates.Contains(availableTransition.ToState))
                        continue;
                    
                    availableNextStates.Add(availableTransition.ToState);
                }
            }

            for (int i = 0; i < stateMachine.AvailableStates.Count; i++)
            {
                StateBase availableState = stateMachine.AvailableStates[i];
                if (availableState.StateID.FromAnyState)
                {
                    if (availableNextStates.Contains(availableState.StateID))
                        continue;
                    
                    availableNextStates.Add(availableState.StateID);
                }
            }
        }

        private void OnStateChange(StateID previousState, StateID currentState)
        {
            stateHistory.Add(new StateHistory(
                previousState.name,
                Time.frameCount.ToString(),
                StackTraceUtility.ExtractStackTrace()));

            if (stateHistory.Count > 9)
                stateHistory.RemoveAt(0);


            if (animationRoutine != null)
                EditorCoroutineUtility.StopCoroutine(animationRoutine);

            animationRoutine = EditorCoroutineUtility.StartCoroutine(FadeOutEnumerator(), this);
        }

        private IEnumerator FadeOutEnumerator(float duration = 0.2f)
        {
            changeColorAnimation = 1;
            double startTime = EditorApplication.timeSinceStartup;
            float timer = 0;
            while (timer < duration)
            {
                double editorDeltaTime = EditorApplication.timeSinceStartup - startTime;
                timer += (float)editorDeltaTime;
                Repaint();
                changeColorAnimation = 1 - timer;
                yield return null;
            }
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
