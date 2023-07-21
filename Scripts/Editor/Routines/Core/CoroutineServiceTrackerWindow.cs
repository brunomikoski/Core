using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Routines
{
     public class CoroutineServiceTrackerWindow : EditorWindow
    {
        private static CoroutineServiceTrackerWindow window;
        private Vector2 scrollPosition;
        static GUIStyle detailsStyle;
        private Vector2 detailsScroll;
        private static readonly GUILayoutOption[] EmptyLayoutOption = new GUILayoutOption[0];

        
        [MenuItem("Window/Analysis/CoroutineRunner Tracker")]
        public static void OpenWindow()
        {
            if (window != null)
            {
                window.Close();
            }

            // will called OnEnable(singleton instance will be set).
            GetWindow<CoroutineServiceTrackerWindow>("CoroutineRunner Tracker").Show();
        }

        private void OnEnable()
        {
            window = this;
        }

        private void Update()
        {
            Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
                return;
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            for (int i = 0; i < CoroutineServiceTracker.RUNNING_ROUTINES.Count; i++)
            {
                CoroutineServiceTracker.RoutineData routineData = CoroutineServiceTracker.RUNNING_ROUTINES[i];

                if (routineData.Routine == null)
                    continue;

                if (routineData.Routine.IsCompleted)
                    continue;

                EditorGUILayout.BeginVertical("Box");

                TimeSpan elapsedTime = DateTime.UtcNow - routineData.UtcNow;
                EditorGUILayout.LabelField($"Line: {routineData.Name}");
                EditorGUILayout.LabelField($"Elapsed Time: {elapsedTime.TotalSeconds}");
                
                
                if (detailsStyle == null)
                {
                    detailsStyle = new GUIStyle("CN Message");
                    detailsStyle.wordWrap = false;
                    detailsStyle.stretchHeight = true;
                    detailsStyle.margin.right = 15;
                }
                EditorGUILayout.BeginVertical("Box");
                detailsScroll = EditorGUILayout.BeginScrollView(this.detailsScroll, EmptyLayoutOption);
                Vector2 vector = detailsStyle.CalcSize(new GUIContent(routineData.StackTrace));
                EditorGUILayout.SelectableLabel(routineData.StackTrace, detailsStyle, new[]
                {
                    GUILayout.ExpandHeight(false),
                    GUILayout.ExpandWidth(true),
                    GUILayout.MinWidth(vector.x),
                    GUILayout.MinHeight(vector.y)
                });
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();

                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();

            }
            EditorGUILayout.EndScrollView();
        }
    }
}
