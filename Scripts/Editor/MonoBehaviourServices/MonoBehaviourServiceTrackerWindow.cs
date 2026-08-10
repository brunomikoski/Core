using System;
using System.Collections.Generic;
using System.Linq;
using BrunoMikoski.ServicesLocation;
#if SERVICELOCATOR_AVAILABLE
using BrunoMikoski.ServicesLocation;
#endif
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

#if UNITY_6000_2_OR_NEWER
using TreeViewBase = UnityEditor.IMGUI.Controls.TreeView<int>;
using TreeViewItemBase = UnityEditor.IMGUI.Controls.TreeViewItem<int>;
using TreeViewStateBase = UnityEditor.IMGUI.Controls.TreeViewState<int>;
#else
using TreeViewBase = UnityEditor.IMGUI.Controls.TreeView;
using TreeViewItemBase = UnityEditor.IMGUI.Controls.TreeViewItem;
using TreeViewStateBase = UnityEditor.IMGUI.Controls.TreeViewState;
#pragma warning disable CS0618
#endif

namespace BrunoMikoski.Core.DevelopmentTools
{
    public class MonoBehaviourServiceTrackerViewItem : TreeViewItemBase
    {
        public int ExecutionIndex;
        public string CallbackName;
        public string UpdateTypeName;
        public string Sparse;
        public string FrameInterval;
        public string CallsPerTick;
        public string FullStackTrace;

        public MonoBehaviourServiceTrackerViewItem(int id) : base(id)
        {
        }
    }

    public class MonoBehaviourServiceTrackerTreeView : TreeViewBase
    {
        const string SORTED_COLUMN_INDEX_STATE_KEY = "MonoBehaviourServiceTrackerTreeView_sortedColumnIndex";

        public IReadOnlyList<TreeViewItemBase> CurrentBindingItems;

        readonly Dictionary<(MonoBehaviourService.UpdateType, int, int), int> sparseGroupCounts = new();

        public MonoBehaviourServiceTrackerTreeView()
            : this(new TreeViewStateBase(), new MultiColumnHeader(new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("#"), width = 40, minWidth = 30, maxWidth = 60 },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Callback"), width = 250, minWidth = 100 },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Update Type"), width = 90, minWidth = 70, maxWidth = 120 },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Sparse"), width = 50, minWidth = 40, maxWidth = 70 },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Interval"), width = 60, minWidth = 40, maxWidth = 80 },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Instances/Tick"), width = 80, minWidth = 50, maxWidth = 100 },
            })))
        {
        }

        MonoBehaviourServiceTrackerTreeView(TreeViewStateBase state, MultiColumnHeader header)
            : base(state, header)
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            header.sortingChanged += Header_sortingChanged;

            header.ResizeToFit();
            Reload();

            header.sortedColumnIndex = SessionState.GetInt(SORTED_COLUMN_INDEX_STATE_KEY, 0);
        }

        public void ReloadAndSort()
        {
            var currentSelected = state.selectedIDs;
            Reload();
            Header_sortingChanged(multiColumnHeader);
            state.selectedIDs = currentSelected;
        }

        void Header_sortingChanged(MultiColumnHeader multiColumnHeader)
        {
            SessionState.SetInt(SORTED_COLUMN_INDEX_STATE_KEY, multiColumnHeader.sortedColumnIndex);
            int index = multiColumnHeader.sortedColumnIndex;
            bool ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            var items = rootItem.children.Cast<MonoBehaviourServiceTrackerViewItem>();

            IOrderedEnumerable<MonoBehaviourServiceTrackerViewItem> orderedEnumerable;
            switch (index)
            {
                case 0:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.ExecutionIndex)
                        : items.OrderByDescending(item => item.ExecutionIndex);
                    break;
                case 1:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.CallbackName)
                        : items.OrderByDescending(item => item.CallbackName);
                    break;
                case 2:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.UpdateTypeName)
                        : items.OrderByDescending(item => item.UpdateTypeName);
                    break;
                case 3:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.Sparse)
                        : items.OrderByDescending(item => item.Sparse);
                    break;
                case 4:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => int.Parse(item.FrameInterval))
                        : items.OrderByDescending(item => int.Parse(item.FrameInterval));
                    break;
                case 5:
                    orderedEnumerable = ascending
                        ? items.OrderBy(item => item.CallsPerTick == "-" ? -1 : int.Parse(item.CallsPerTick))
                        : items.OrderByDescending(item => item.CallsPerTick == "-" ? -1 : int.Parse(item.CallsPerTick));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            CurrentBindingItems = rootItem.children = orderedEnumerable.Cast<TreeViewItemBase>().ToList();
            BuildRows(rootItem);
        }

        protected override TreeViewItemBase BuildRoot()
        {
            TreeViewItemBase root = new TreeViewItemBase { depth = -1 };
            List<TreeViewItemBase> children = new List<TreeViewItemBase>();

            var tracked = MonoBehaviourServiceTracker.TRACKED_SUBSCRIPTIONS;

            ComputeSparseGroupCounts(tracked);

            int updateIdx = 0, lateUpdateIdx = 0, fixedUpdateIdx = 0;

            for (int i = 0; i < tracked.Count; i++)
            {
                MonoBehaviourServiceTracker.SubscriptionTrackingData data = tracked[i];

                int execIndex = data.UpdateType switch
                {
                    MonoBehaviourService.UpdateType.Update => updateIdx++,
                    MonoBehaviourService.UpdateType.LateUpdate => lateUpdateIdx++,
                    MonoBehaviourService.UpdateType.FixedUpdate => fixedUpdateIdx++,
                    _ => -1
                };

                string callsPerTick = "-";
                if (data.SparseCalls && data.FrameInterval > 1)
                {
                    var key = (data.UpdateType, data.FrameInterval, data.UpdateGroup);
                    if (sparseGroupCounts.TryGetValue(key, out int count))
                        callsPerTick = count.ToString();
                }

                children.Add(new MonoBehaviourServiceTrackerViewItem(i)
                {
                    ExecutionIndex = execIndex,
                    CallbackName = data.CallbackName,
                    UpdateTypeName = data.UpdateType.ToString(),
                    Sparse = data.SparseCalls ? "Yes" : "No",
                    FrameInterval = data.FrameInterval.ToString(),
                    CallsPerTick = callsPerTick,
                    FullStackTrace = data.StackTrace
                });
            }

            CurrentBindingItems = children;
            root.children = CurrentBindingItems as List<TreeViewItemBase>;
            return root;
        }

        void ComputeSparseGroupCounts(List<MonoBehaviourServiceTracker.SubscriptionTrackingData> tracked)
        {
            sparseGroupCounts.Clear();
            for (int i = 0; i < tracked.Count; i++)
            {
                var data = tracked[i];
                if (!data.SparseCalls || data.FrameInterval <= 1)
                    continue;

                var key = (data.UpdateType, data.FrameInterval, data.UpdateGroup);
                sparseGroupCounts.TryGetValue(key, out int count);
                sparseGroupCounts[key] = count + 1;
            }
        }

        protected override bool CanMultiSelect(TreeViewItemBase item)
        {
            return false;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as MonoBehaviourServiceTrackerViewItem;

            for (int visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                Rect rect = args.GetCellRect(visibleColumnIndex);
                int columnIndex = args.GetColumn(visibleColumnIndex);

                var labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                switch (columnIndex)
                {
                    case 0:
                        EditorGUI.LabelField(rect, item.ExecutionIndex.ToString(), labelStyle);
                        break;
                    case 1:
                        EditorGUI.LabelField(rect, item.CallbackName, labelStyle);
                        break;
                    case 2:
                        EditorGUI.LabelField(rect, item.UpdateTypeName, labelStyle);
                        break;
                    case 3:
                        EditorGUI.LabelField(rect, item.Sparse, labelStyle);
                        break;
                    case 4:
                        EditorGUI.LabelField(rect, item.FrameInterval, labelStyle);
                        break;
                    case 5:
                        EditorGUI.LabelField(rect, item.CallsPerTick, labelStyle);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                }
            }
        }
    }

    public class MonoBehaviourServiceTrackerWindow : EditorWindow
    {
        private static int INTERVAL;

        private static MonoBehaviourServiceTrackerWindow Window;

        [MenuItem("Window/Analysis/MonoBehaviourService Tracker")]
        public static void OpenWindow()
        {
            if (Window != null)
                Window.Close();

            GetWindow<MonoBehaviourServiceTrackerWindow>("MonoBehaviourService Tracker").Show();
        }

        private static readonly GUILayoutOption[] EMPTY_LAYOUT_OPTION = Array.Empty<GUILayoutOption>();

        private MonoBehaviourServiceTrackerTreeView _treeView;
        private object _splitterState;

        private ServiceReference<MonoBehaviourService> _monoBehaviourService = new();
        

        private void OnEnable()
        {
            Window = this;
            _splitterState = SplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);
            _treeView = new MonoBehaviourServiceTrackerTreeView();

            SnapshotExistingSubscriptions();
        }

        private void OnDisable()
        {
            Window = null;
        }

        private readonly List<MonoBehaviourService.UpdateSubscription> _snapshotBuffer = new(256);

        private void SnapshotExistingSubscriptions()
        {
            if (!Application.isPlaying)
                return;

            if (!_monoBehaviourService.Exists)
                return;

            _monoBehaviourService.Reference.GetAllUpdateListenersForTracking(_snapshotBuffer);
            for (int i = 0; i < _snapshotBuffer.Count; i++)
            {
                MonoBehaviourServiceTracker.TrackExistingSubscription(_snapshotBuffer[i]);
            }
        }

        private void OnGUI()
        {
            RenderHeadPanel();

            SplitterGUILayout.BeginVerticalSplit(_splitterState, EMPTY_LAYOUT_OPTION);
            {
                RenderTable();
                RenderDetailsPanel();
            }
            SplitterGUILayout.EndVerticalSplit();
        }

        private void RenderHeadPanel()
        {
            EditorGUILayout.BeginVertical(EMPTY_LAYOUT_OPTION);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, EMPTY_LAYOUT_OPTION);

            List<MonoBehaviourServiceTracker.SubscriptionTrackingData> data = MonoBehaviourServiceTracker.TRACKED_SUBSCRIPTIONS;
            int total = data.Count;
            int updateCount = 0, lateCount = 0, fixedCount = 0, sparseCount = 0;

            for (int i = 0; i < data.Count; i++)
            {
                switch (data[i].UpdateType)
                {
                    case MonoBehaviourService.UpdateType.Update:
                        updateCount++;
                        break;
                    case MonoBehaviourService.UpdateType.LateUpdate:
                        lateCount++;
                        break;
                    case MonoBehaviourService.UpdateType.FixedUpdate:
                        fixedCount++;
                        break;
                }

                if (data[i].SparseCalls)
                    sparseCount++;
            }

            GUILayout.Label(
                $"Total: {total}  |  Update: {updateCount}  |  LateUpdate: {lateCount}  |  FixedUpdate: {fixedCount}  |  Sparse: {sparseCount}  |  Sequential: {total - sparseCount}");
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

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

            tableScroll = EditorGUILayout.BeginScrollView(tableScroll, new GUILayoutOption[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.MaxWidth(2000f)
            });
            Rect controlRect = EditorGUILayout.GetControlRect(new GUILayoutOption[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true)
            });

            _treeView?.OnGUI(controlRect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void Update()
        {
            if (INTERVAL++ % 120 == 0)
            {
                _treeView.ReloadAndSort();
                Repaint();
            }
        }

        private static GUIStyle detailsStyle;
        private Vector2 detailsScroll;

        private void RenderDetailsPanel()
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
            List<int> selected = _treeView.state.selectedIDs;
            if (selected.Count > 0)
            {
                int first = selected[0];
                if (_treeView.CurrentBindingItems.FirstOrDefault(x => x.id == first) is
                    MonoBehaviourServiceTrackerViewItem item)
                {
                    message = item.FullStackTrace ?? "(Subscribed before tracker was opened. No stack trace available.)";
                }
            }

            detailsScroll = EditorGUILayout.BeginScrollView(detailsScroll, EMPTY_LAYOUT_OPTION);
            Vector2 vector = detailsStyle.CalcSize(new GUIContent(message));
            EditorGUILayout.SelectableLabel(message, detailsStyle, new[]
            {
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true),
                GUILayout.MinWidth(vector.x),
                GUILayout.MinHeight(vector.y)
            });
            EditorGUILayout.EndScrollView();
        }

    }
}

#if !UNITY_6000_2_OR_NEWER
#pragma warning restore CS0618
#endif
