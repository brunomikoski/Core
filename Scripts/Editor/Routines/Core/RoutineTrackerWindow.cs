using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace BrunoMikoski.Core.Routines
{
    public class CoroutineTrackerViewItem : TreeViewItem
    {
        static Regex removeHref = new Regex("<a href.+>(.+)</a>", RegexOptions.Compiled);

        public string TaskType { get; set; }
        public string Elapsed { get; set; }
        public string Status { get; set; }
        
        public string OwnerName { get; set; }

        string position;
        public string Position
        {
            get => position;
            set
            {
                position = value;
                PositionFirstLine = GetFirstLine(position);
            }
        }

        public string PositionFirstLine { get; private set; }

        static string GetFirstLine(string str)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '\r' || str[i] == '\n')
                {
                    break;
                }
                sb.Append(str[i]);
            }

            return removeHref.Replace(sb.ToString(), "$1");
        }

        public CoroutineTrackerViewItem(int id) : base(id)
        {

        }
    }

    public class CoroutineTrackerTreeView : TreeView
    {
        private const string SORTED_COLUMN_INDEX_STATE_KEY = "CoroutineTrackerTreeView_sortedColumnIndex";

        public IReadOnlyList<TreeViewItem> CurrentBindingItems;

        public CoroutineTrackerTreeView()
            : this(new TreeViewState(), new MultiColumnHeader(new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Owner"), width = 10},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Enumerator"), width = 20},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Elapsed"), width = 10},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Status"), width = 10},
                new MultiColumnHeaderState.Column() { headerContent = new GUIContent("Position")},
                
            })))
        {
        }

        CoroutineTrackerTreeView(TreeViewState state, MultiColumnHeader header)
            : base(state, header)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            header.sortingChanged += Header_sortingChanged;

            header.ResizeToFit();
            Reload();

            header.sortedColumnIndex = SessionState.GetInt(SORTED_COLUMN_INDEX_STATE_KEY, 1);
        }

        public void ReloadAndSort()
        {
            var currentSelected = this.state.selectedIDs;
            Reload();
            Header_sortingChanged(this.multiColumnHeader);
            this.state.selectedIDs = currentSelected;
        }

        private void Header_sortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SessionState.SetInt(SORTED_COLUMN_INDEX_STATE_KEY, multiColumnHeader.sortedColumnIndex);
            var index = multiColumnHeader.sortedColumnIndex;
            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            var items = rootItem.children.Cast<CoroutineTrackerViewItem>();

            IOrderedEnumerable<CoroutineTrackerViewItem> orderedEnumerable;
            switch (index)
            {
                case 0:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.OwnerName) : items.OrderByDescending(item => item.OwnerName);
                    break;
                case 1:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.TaskType) : items.OrderByDescending(item => item.TaskType);
                    break;
                case 2:
                    orderedEnumerable = ascending ? items.OrderBy(item => double.Parse(item.Elapsed)) : items.OrderByDescending(item => double.Parse(item.Elapsed));
                    break;
                case 3:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.Status) : items.OrderByDescending(item => item.Elapsed);
                    break;
                case 4:
                    orderedEnumerable = ascending ? items.OrderBy(item => item.Position) : items.OrderByDescending(item => item.PositionFirstLine);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            CurrentBindingItems = rootItem.children = orderedEnumerable.Cast<TreeViewItem>().ToList();
            BuildRows(rootItem);
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem { depth = -1 };
            List<TreeViewItem> children = new List<TreeViewItem>();


            for (int i = 0; i < CoroutineServiceTracker.RUNNING_ROUTINES.Count; i++)
            {
                CoroutineServiceTracker.RoutineData routineData = CoroutineServiceTracker.RUNNING_ROUTINES[i];
                string status = "Complete";
                if (!routineData.Routine.IsCompleted)
                {
                    if (routineData.Routine.IsPaused)
                    {
                        status = "Paused";
                    }
                    else
                    {
                        status = "Running";
                    }
                }
                
                string lastKnowPath = "";
                if (routineData.Routine.MonoBehaviour != null)
                {
                    lastKnowPath = routineData.Routine.MonoBehaviour.gameObject.transform.GetPath();
                }

                string elapsed = (RoutineTrackerWindow.Window.LastUpdate - routineData.Created).TotalSeconds.ToString("00.00");
                if (routineData.Routine.IsCompleted)
                    elapsed = (routineData.Routine.FinishedTime - routineData.Created).TotalSeconds.ToString("00.00");
                
                children.Add(new CoroutineTrackerViewItem(i)
                {
                    OwnerName = lastKnowPath,
                    Elapsed = elapsed,
                    TaskType = routineData.Name,
                    Position = routineData.StackTrace,
                    Status =  status
                });
            }

            CurrentBindingItems = children;
            root.children = CurrentBindingItems as List<TreeViewItem>;
            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as CoroutineTrackerViewItem;

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = args.GetColumn(visibleColumnIndex);

                var labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                switch (columnIndex)
                {
                    case 0:
                        EditorGUI.LabelField(rect, item.OwnerName, labelStyle);
                        break;
                    case 1:
                        EditorGUI.LabelField(rect, item.TaskType, labelStyle);
                        break;
                    case 2:
                        EditorGUI.LabelField(rect, item.Elapsed, labelStyle);
                        break;
                    case 3:
                        EditorGUI.LabelField(rect, item.Status, labelStyle);
                        break;
                    case 4:
                        EditorGUI.LabelField(rect, item.PositionFirstLine, labelStyle);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }
    }
    
    public class RoutineTrackerWindow : EditorWindow
    {
        private static int INTERVAL;

        public static RoutineTrackerWindow Window;
        
        public DateTime LastUpdate;

        [MenuItem("Window/Analysis/CoroutineRunner Tracker")]
        public static void OpenWindow()
        {
            if (Window != null)
            {
                Window.Close();
            }

            // will called OnEnable(singleton instance will be set).
            GetWindow<RoutineTrackerWindow>("Coroutine Tracker").Show();
        }

        static readonly GUILayoutOption[] EMPTY_LAYOUT_OPTION = Array.Empty<GUILayoutOption>();

        CoroutineTrackerTreeView treeView;
        object splitterState;

        private void OnEnable()
        {
            Window = this; // set singleton.
            splitterState = SplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);
            treeView = new CoroutineTrackerTreeView();
            LastUpdate = DateTime.UtcNow;
        }

        private void OnGUI()
        {
            // Head
            RenderHeadPanel();

            // Splittable
            SplitterGUILayout.BeginVerticalSplit(this.splitterState, EMPTY_LAYOUT_OPTION);
            {
                // Column Tabble
                RenderTable();

                // StackTrace details
                RenderDetailsPanel();
            }
            SplitterGUILayout.EndVerticalSplit();
        }

        #region HeadPanel

        static void RenderHeadPanel()
        {
            EditorGUILayout.BeginVertical(EMPTY_LAYOUT_OPTION);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, EMPTY_LAYOUT_OPTION);

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region TableColumn

        private Vector2 tableScroll;
        private GUIStyle tableListStyle;

        private void RenderTable()
        {
            if (tableListStyle == null)
            {
                tableListStyle = new GUIStyle("CN Box");
                tableListStyle.margin.top = 0;
                tableListStyle.padding.left = 3;
            }

            EditorGUILayout.BeginVertical(tableListStyle, EMPTY_LAYOUT_OPTION);

            tableScroll = EditorGUILayout.BeginScrollView(this.tableScroll, new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.MaxWidth(2000f)
            });
            Rect controlRect = EditorGUILayout.GetControlRect(new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            });


            treeView?.OnGUI(controlRect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void Update()
        {
            if (INTERVAL++ % 120 == 0)
            {
                if (Application.isPlaying)
                    LastUpdate = DateTime.UtcNow;
                treeView.ReloadAndSort();
                Repaint(); 
            }
        }

        #endregion

        #region Details

        static GUIStyle detailsStyle;
        Vector2 detailsScroll;

        void RenderDetailsPanel()
        {
            if (detailsStyle == null)
            {
                detailsStyle = new GUIStyle("CN Message")
                {
                    wordWrap = false,
                    stretchHeight = true,
                    margin =
                    {
                        right = 15
                    }
                };
            }

            string message = "";
            var selected = treeView.state.selectedIDs;
            if (selected.Count > 0)
            {
                int first = selected[0];
                if (treeView.CurrentBindingItems.FirstOrDefault(x => x.id == first) is CoroutineTrackerViewItem item)
                {
                    message = item.Position;
                }
            }

            detailsScroll = EditorGUILayout.BeginScrollView(this.detailsScroll, EMPTY_LAYOUT_OPTION);
            var vector = detailsStyle.CalcSize(new GUIContent(message));
            EditorGUILayout.SelectableLabel(message, detailsStyle, new[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(vector.x),
                GUILayout.MinHeight(vector.y)
            });
            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}
