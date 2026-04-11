using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

#pragma warning disable CS0618
namespace BrunoMikoski.Core.Events
{
    public class SafeEventsWindow : EditorWindow
    {
        [Serializable]
        private class WindowState
        {
            public float LeftWidth = 380f;
            public Vector2 RightScroll = Vector2.zero;
            public int SelectedListenerIndex = -1;
        }

        private const float MinLeft = 220f;
        private const float MinRight = 260f;
        private const float SplitterThickness = 4f;

        private readonly SafeEventHierarchyGroup rootGroup = new SafeEventHierarchyGroup();
        private WindowState state = new WindowState();

        private EventsTreeView treeView;
        private TreeViewState treeState;
        private MultiColumnHeader multiColumnHeader;

        private int lastTotalEventCount = -1;
        private bool draggingSplitter;

        private static readonly Color ErrorRed = new Color(1f, 0.5f, 0.5f, 1f);

        [MenuItem("Window/Analysis/Safe Events")]
        private static void ShowWindow()
        {
            SafeEventsWindow wnd = GetWindow<SafeEventsWindow>();
            wnd.titleContent = new GUIContent("Events");
            wnd.Show();
        }

        private void OnEnable()
        {
            if (treeState == null)
                treeState = new TreeViewState();

            MultiColumnHeaderState.Column[] cols = {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Event"),
                    width = 220f,
                    minWidth = 160f,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Listeners"),
                    width = 90f,
                    minWidth = 70f,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Dispatches"),
                    width = 100f,
                    minWidth = 80f,
                    autoResize = true,
                    allowToggleVisibility = true
                }
            };

            MultiColumnHeaderState headerState = new MultiColumnHeaderState(cols);
            multiColumnHeader = new MultiColumnHeader(headerState)
            {
                canSort = true,
                height = 24f
            };
            multiColumnHeader.sortingChanged += OnSortingChanged;

            treeView = new EventsTreeView(treeState, multiColumnHeader, rootGroup);
            treeView.SelectionChangedExtern  += OnTreeSelectionChanged;
            treeView.onDoubleClickedItem += OnTreeItemDoubleClicked;

            RefreshHierarchy(force: true);
        }

        private void OnDisable()
        {
            if (multiColumnHeader != null)
                multiColumnHeader.sortingChanged -= OnSortingChanged;
        }

        private void Update()
        {
            if (lastTotalEventCount != SafeEventBase.TotalEventsCreated)
                RefreshHierarchy(force: true);

            for (int i = rootGroup.children.Count - 1; i >= 0; --i)
            {
                SafeEventHierarchyGroup g = rootGroup.children[i] as SafeEventHierarchyGroup;
                if (g == null)
                    continue;
                if (g.unityObject.IsNotNull() && g.unityObject == null)
                    rootGroup.children.RemoveAt(i);
            }

            Repaint();
        }

        private void RefreshHierarchy(bool force)
        {
            lastTotalEventCount = SafeEventBase.TotalEventsCreated;
            SafeEventsFinder.FindEvents(rootGroup);
            if (treeView != null)
            {
                treeView.Reload();
                if (force)
                    treeView.ExpandRootDefaults();
            }
        }

        private void OnGUI()
        {
            Rect full = position;
            full.x = 0f;
            full.y = 0f;

            float leftWidth = Mathf.Clamp(state.LeftWidth, MinLeft, Mathf.Max(MinLeft, position.width - MinRight));
            Rect left = new Rect(0f, 0f, leftWidth, position.height);
            Rect splitter = new Rect(leftWidth, 0f, SplitterThickness, position.height);
            Rect right = new Rect(leftWidth + SplitterThickness, 0f, Mathf.Max(MinRight, position.width - leftWidth - SplitterThickness), position.height);

            DrawLeft(left);
            HandleSplitter(splitter, ref leftWidth);
            DrawRight(right);

            state.LeftWidth = leftWidth;
        }

        private void DrawLeft(Rect rect)
        {
            GUILayout.BeginArea(rect);
            Rect headerRect = new Rect(0f, 0f, rect.width, EditorGUIUtility.singleLineHeight + 4f);
            GUI.Box(headerRect, GUIContent.none, EditorStyles.toolbar);
            Rect treeRect = new Rect(0f, headerRect.height, rect.width, rect.height - headerRect.height);
            treeView?.OnGUI(treeRect);
            GUILayout.EndArea();
        }

        private void DrawRight(Rect rect)
        {
            GUILayout.BeginArea(rect);
            Rect headerRect = new Rect(0f, 0f, rect.width, EditorGUIUtility.singleLineHeight + 4f);
            GUI.Box(headerRect, GUIContent.none, EditorStyles.toolbar);
            GUILayout.Space(headerRect.height);

            state.RightScroll = EditorGUILayout.BeginScrollView(state.RightScroll);
            DrawListenersPanel();
            EditorGUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void HandleSplitter(Rect splitter, ref float leftWidth)
        {
            EditorGUIUtility.AddCursorRect(splitter, MouseCursor.ResizeHorizontal);

            Event e = Event.current;

            if (e.type == EventType.MouseDown && splitter.Contains(e.mousePosition))
            {
                draggingSplitter = true;
                e.Use();
            }

            if (draggingSplitter)
            {
                if (e.type == EventType.MouseDrag)
                {
                    leftWidth = Mathf.Clamp(e.mousePosition.x, MinLeft, position.width - MinRight);
                    Repaint();
                }
                else if (e.rawType == EventType.MouseUp)
                {
                    draggingSplitter = false;
                }
            }

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(splitter, EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f, 1f) : new Color(0.7f, 0.7f, 0.7f, 1f));
        }

        private void DrawListenersPanel()
        {
            EventsTreeView.EventRow selected = treeView != null ? treeView.SelectedEvent : null;
            if (selected == null || selected.EventBase == null)
            {
                EditorGUILayout.LabelField("Select an event to see listeners.", EditorStyles.label);
                return;
            }

            IList<SafeEventBase.CallbackData> listeners = selected.EventBase.Listeners;

            if (listeners.Count == 0)
            {
                EditorGUILayout.LabelField("No listeners registered.", EditorStyles.label);
                return;
            }

            for (int i = 0; i < listeners.Count; ++i)
            {
                SafeEventBase.CallbackData cb = listeners[i];
                Object asObject = cb.target as Object;

                Rect rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(EditorGUIUtility.singleLineHeight + 2f));
                bool isSelected = state.SelectedListenerIndex == i;

                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    state.SelectedListenerIndex = i;
                    if (asObject != null)
                        EditorGUIUtility.PingObject(asObject);
                }

                if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && rect.Contains(Event.current.mousePosition))
                {
                    OpenScriptFile(cb);
                }

                if (Event.current.type == EventType.Repaint && isSelected)
                {
                    GUIStyle sel = new GUIStyle("PR Label");
                    sel.Draw(rect, false, true, true, true);
                }

                string targetName = asObject != null ? asObject.name : cb.target.GetType().Name;
                string label = targetName + " - " + cb.methodInfo.Name;

                GUIStyle style = EditorStyles.label;
                if (cb.IsTargetDestroyedOrDespawned)
                {
                    GUIStyle error = new GUIStyle(EditorStyles.label);
                    error.normal.textColor = ErrorRed;
                    error.focused.textColor = ErrorRed;
                    error.active.textColor = ErrorRed;
                    style = error;
                }
                else if (isSelected)
                {
                    GUIStyle selLabel = new GUIStyle(EditorStyles.label);
                    selLabel.normal.textColor = Color.white;
                    selLabel.focused.textColor = Color.white;
                    selLabel.active.textColor = Color.white;
                    style = selLabel;
                }

                EditorGUILayout.LabelField(label, style, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void OnSortingChanged(MultiColumnHeader header)
        {
            if (treeView == null)
                return;

            int columnIndex = header.sortedColumnIndex;
            bool ascending = header.IsSortedAscending(columnIndex);
            treeView.ApplySort(columnIndex, ascending);
        }

        private void OnTreeSelectionChanged(IList<int> ids)
        {
            state.SelectedListenerIndex = -1;
        }

        private void OnTreeItemDoubleClicked(int id)
        {
            EventsTreeView.EventRow row = treeView.GetRowById(id);
            if (row == null || row.EventBase == null)
                return;

            IList<SafeEventBase.CallbackData> listeners = row.EventBase.Listeners;
            if (listeners.Count == 0)
                return;

            SafeEventBase.CallbackData cb = listeners[0];
            OpenScriptFile(cb);
        }

        private static void OpenScriptFile(SafeEventBase.CallbackData callbackData)
        {
            string[] guids = AssetDatabase.FindAssets("t:script " + callbackData.target.GetType().Name);
            if (guids == null || guids.Length == 0)
                return;

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
            {
                Object scriptAssetMissing = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (scriptAssetMissing != null)
                    AssetDatabase.OpenAsset(scriptAssetMissing);
                return;
            }

            string[] lines = File.ReadAllLines(assetPath);
            int goToLine = 0;

            for (int i = 0; i < lines.Length; ++i)
            {
                if (lines[i].Contains(callbackData.methodInfo.Name + "("))
                {
                    goToLine = i + 1;
                    break;
                }
            }

            Object scriptAsset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (scriptAsset != null)
                AssetDatabase.OpenAsset(scriptAsset, Math.Max(1, goToLine));
        }

        private class EventsTreeView : TreeView
        {
            public class EventRow : TreeViewItem
            {
                public SafeSafeEventHierarchySafeEvent Data;
                public SafeEventBase EventBase;
                public int ListenerCount;
                public int DispatchCount;
                public bool HasError;
            }

            private readonly SafeEventHierarchyGroup rootGroup;
            private readonly List<EventRow> flatRows = new List<EventRow>();
            public event Action<IList<int>> SelectionChangedExtern;
            
            public EventRow SelectedEvent
            {
                get
                {
                    IList<int> sel = GetSelection();
                    if (sel == null || sel.Count == 0)
                        return null;
                    return flatRows.FirstOrDefault(r => r.id == sel[0] && r.EventBase != null);
                }
            }

            public EventsTreeView(TreeViewState state, MultiColumnHeader header, SafeEventHierarchyGroup root) : base(state, header)
            {
                rootGroup = root;
                showAlternatingRowBackgrounds = true;
                showBorder = true;
                rowHeight = EditorGUIUtility.singleLineHeight + 2f;
                Reload();
            }

            public void ExpandRootDefaults()
            {
                SetExpanded(rootItem.id, true);
                foreach (TreeViewItem child in rootItem.children)
                    SetExpanded(child.id, true);
            }

            protected override TreeViewItem BuildRoot()
            {
                flatRows.Clear();
                int idCounter = 1;
                TreeViewItem root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
                List<TreeViewItem> children = new List<TreeViewItem>();
                BuildRecursive(rootGroup, 0, ref idCounter, children);
                root.children = children;
                if (root.children == null)
                    root.children = new List<TreeViewItem>();
                return root;
            }

            private void BuildRecursive(SafeEventHierarchyGroup group, int depth, ref int idCounter, List<TreeViewItem> into)
            {
                List<TreeViewItem> groupChildren = new List<TreeViewItem>();

                for (int i = 0; i < group.children.Count; ++i)
                {
                    SafeEventHierarchyNode node = group.children[i];

                    SafeSafeEventHierarchySafeEvent e = node as SafeSafeEventHierarchySafeEvent;
                    if (e != null)
                    {
                        EventRow row = new EventRow
                        {
                            id = idCounter++,
                            depth = depth,
                            displayName = e.name,
                            Data = e,
                            EventBase = e.eventBase,
                            ListenerCount = e.eventBase != null ? e.eventBase.Listeners.Count : 0,
                            DispatchCount = e.eventBase != null ? e.eventBase.DispatchCount : 0,
                            HasError = e.eventBase != null && e.eventBase.HasDestroyedOrDespawnedListeners
                        };
                        groupChildren.Add(row);
                        flatRows.Add(row);
                        continue;
                    }

                    SafeEventHierarchyGroup g = node as SafeEventHierarchyGroup;
                    if (g != null)
                    {
                        TreeViewItem groupItem = new TreeViewItem
                        {
                            id = idCounter++,
                            depth = depth,
                            displayName = g.name
                        };

                        List<TreeViewItem> nested = new List<TreeViewItem>();
                        BuildRecursive(g, depth + 1, ref idCounter, nested);
                        groupItem.children = nested;
                        groupChildren.Add(groupItem);
                    }
                }

                for (int i = 0; i < groupChildren.Count; ++i)
                    into.Add(groupChildren[i]);
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                EventRow row = args.item as EventRow;

                for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    Rect cellRect = args.GetCellRect(i);
                    CenterRectUsingSingleLineHeight(ref cellRect);

                    if (row == null)
                    {
                        base.RowGUI(args);
                        continue;
                    }

                    switch (args.GetColumn(i))
                    {
                        case 0:
                            GUIStyle style = row.HasError ? ErrorLabel() : EditorStyles.label;
                            EditorGUI.LabelField(cellRect, row.displayName, style);
                            break;
                        case 1:
                            EditorGUI.LabelField(cellRect, row.ListenerCount.ToString(), row.HasError ? ErrorLabel() : EditorStyles.label);
                            break;
                        case 2:
                            EditorGUI.LabelField(cellRect, row.DispatchCount.ToString(), row.HasError ? ErrorLabel() : EditorStyles.label);
                            break;
                    }
                }
            }

            private static GUIStyle ErrorLabel()
            {
                GUIStyle s = new GUIStyle(EditorStyles.label);
                s.normal.textColor = new Color(1f, 0.5f, 0.5f, 1f);
                s.focused.textColor = s.normal.textColor;
                s.active.textColor = s.normal.textColor;
                return s;
            }

            protected override void DoubleClickedItem(int id)
            {
                if (onDoubleClickedItem != null)
                    onDoubleClickedItem.Invoke(id);
            }

            public void ApplySort(int columnIndex, bool ascending)
            {
                if (rootItem == null || rootItem.children == null)
                    return;

                Comparison<TreeViewItem> comp = GetComparison(columnIndex, ascending);
                rootItem.children.Sort(comp);
                Reload();
            }

            private Comparison<TreeViewItem> GetComparison(int columnIndex, bool ascending)
            {
                return (a, b) =>
                {
                    EventRow ea = a as EventRow;
                    EventRow eb = b as EventRow;

                    int sign = ascending ? 1 : -1;

                    if (ea == null && eb == null)
                        return 0;
                    if (ea == null)
                        return -1;
                    if (eb == null)
                        return 1;

                    switch (columnIndex)
                    {
                        case 0:
                            return sign * string.Compare(ea.displayName, eb.displayName, StringComparison.Ordinal);
                        case 1:
                            return sign * ea.ListenerCount.CompareTo(eb.ListenerCount);
                        case 2:
                            return sign * ea.DispatchCount.CompareTo(eb.DispatchCount);
                        default:
                            return 0;
                    }
                };
            }

            public EventRow GetRowById(int id)
            {
                for (int i = 0; i < flatRows.Count; ++i)
                {
                    if (flatRows[i].id == id)
                        return flatRows[i];
                }
                return null;
            }

            public event Action<int> onDoubleClickedItem;
            protected override void SelectionChanged(IList<int> selectedIds)
            {
                if (SelectionChangedExtern != null)
                    SelectionChangedExtern.Invoke(selectedIds);
            }
        }
    }
}
#pragma warning restore CS0618
