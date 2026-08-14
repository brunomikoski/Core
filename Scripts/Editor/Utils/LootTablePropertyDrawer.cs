#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using BrunoMikoski.Core.DevelopmentTools.Utils;
using BrunoMikoski.Core.Utils;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Core.Utils
{
    [CustomPropertyDrawer(typeof(LootTable<>), true)]
    public class LootTablePropertyDrawer : PropertyDrawer
    {
        private class ListCache
        {
            public ReorderableList List;
            public float TotalWeight;
            public SerializedObject SerializedObject; // track which SO the list was created for
        }

        private static readonly Dictionary<string, ListCache> _lists = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property?.serializedObject?.targetObject == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("(object destroyed)"));
                return;
            }

            SerializedProperty entriesProp = property.FindPropertyRelative("_entries");
            SerializedProperty avoidProp = property.FindPropertyRelative("_avoidConsecutiveDuplicate");
            SerializedProperty prdProp = property.FindPropertyRelative("_pseudoRandom");
            if (entriesProp == null || !entriesProp.isArray)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            // compute total considering same rules as runtime: ignore null items and non-positive weights
            float total = 0f;
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty element = entriesProp.GetArrayElementAtIndex(i);
                SerializedProperty itemProp = element.FindPropertyRelative("Item");
                SerializedProperty weightProp = element.FindPropertyRelative("Weight");
                if (IsItemValid(itemProp))
                {
                    float w = weightProp != null ? Mathf.Max(0f, weightProp.floatValue) : 0f;
                    if (w > 0f) total += w;
                }
            }

            string key = GetKey(property);
            bool cacheValid = _lists.TryGetValue(key, out ListCache cache) && cache != null && cache.List != null
                              && cache.SerializedObject == property.serializedObject
                              && cache.SerializedObject?.targetObject != null;

            if (!cacheValid)
            {
                if (cache == null) cache = new ListCache();

                var list = new ReorderableList(property.serializedObject, entriesProp, true, true, true, true);
                cache.SerializedObject = property.serializedObject;
                cache.List = list;

                list.elementHeightCallback = index =>
                {
                    if (index < 0 || index >= entriesProp.arraySize)
                        return EditorGUIUtility.singleLineHeight + 6f;
                    SerializedProperty element = entriesProp.GetArrayElementAtIndex(index);
                    SerializedProperty itemProp = element.FindPropertyRelative("Item");
                    float itemHeight = itemProp != null
                        ? EditorGUI.GetPropertyHeight(itemProp, GUIContent.none, true)
                        : EditorGUIUtility.singleLineHeight;
                    return Mathf.Max(EditorGUIUtility.singleLineHeight, itemHeight) + 6f;
                };

                list.drawHeaderCallback = rect =>
                {
                    if (property.isExpanded && entriesProp.arraySize > 0)
                    {
                        string totalText = $"Total: {cache.TotalWeight:0.###}";
                        var totalSize = EditorStyles.miniLabel.CalcSize(new GUIContent(totalText));
                        var totalRect = new Rect(rect.xMax - totalSize.x - 6f, rect.y + (rect.height - totalSize.y) * 0.5f, totalSize.x, totalSize.y);
                        EditorGUI.LabelField(totalRect, totalText, EditorStyles.miniLabel);

                        float btnWidth = 66f;
                        var btnRect = new Rect(totalRect.x - btnWidth - 6f, rect.y + 1f, btnWidth, rect.height - 2f);

                        // Foldout only covers the area left of the buttons so clicks on them aren't swallowed
                        var foldoutRect = new Rect(rect.x, rect.y, btnRect.x - rect.x - 4f, rect.height);
                        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

                        if (GUI.Button(btnRect, new GUIContent("Split Odds", "Set all weights to 1 so each entry has equal odds"), EditorStyles.miniButton))
                        {
                            Undo.RecordObject(property.serializedObject.targetObject, "Split Loot Table Odds");
                            for (int i = 0; i < entriesProp.arraySize; i++)
                            {
                                SerializedProperty weightProp = entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Weight");
                                if (weightProp != null)
                                    weightProp.floatValue = 1f;
                            }

                            property.serializedObject.ApplyModifiedProperties();
                        }
                    }
                    else
                    {
                        // Collapsed or empty — foldout takes full width, show total if expanded
                        var foldoutRect = new Rect(rect.x, rect.y, rect.width, rect.height);
                        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

                        if (property.isExpanded)
                        {
                            string totalText = $"Total: {cache.TotalWeight:0.###}";
                            var totalSize = EditorStyles.miniLabel.CalcSize(new GUIContent(totalText));
                            var totalRect = new Rect(rect.xMax - totalSize.x - 6f, rect.y + (rect.height - totalSize.y) * 0.5f, totalSize.x, totalSize.y);
                            EditorGUI.LabelField(totalRect, totalText, EditorStyles.miniLabel);
                        }
                    }
                };

                list.drawElementCallback = (rect, index, active, focused) =>
                {
                    if (!property.isExpanded) return;

                    rect.y += 2f;
                    float lineHeight = EditorGUIUtility.singleLineHeight;

                    SerializedProperty element = entriesProp.GetArrayElementAtIndex(index);
                    SerializedProperty itemProp = element.FindPropertyRelative("Item");
                    SerializedProperty weightProp = element.FindPropertyRelative("Weight");

                    float itemHeight = itemProp != null
                        ? EditorGUI.GetPropertyHeight(itemProp, GUIContent.none, true)
                        : lineHeight;
                    itemHeight = Mathf.Max(lineHeight, itemHeight);

                    float spacing = 4f;
                    float weightFieldWidth = 70f; // compact weight field
                    float oddsLabelWidth = 64f;

                    Rect itemRect = new Rect(rect.x, rect.y, rect.width - weightFieldWidth - oddsLabelWidth - spacing * 2f, itemHeight);
                    Rect weightRect = new Rect(itemRect.xMax + spacing, rect.y, weightFieldWidth, lineHeight);
                    Rect oddsRect = new Rect(weightRect.xMax + spacing, rect.y, oddsLabelWidth, lineHeight);

                    // Draw item and weight fields
                    EditorGUI.PropertyField(itemRect, itemProp, GUIContent.none, true);

                    // Draw weight with no label but tooltip
                    var weightLabel = new GUIContent("", "Relative weight used for sampling");
                    EditorGUIUtility.labelWidth = 0f;
                    EditorGUI.PropertyField(weightRect, weightProp, weightLabel, true);

                    // Draw odds percentage
                    float w = weightProp != null ? Mathf.Max(0f, weightProp.floatValue) : 0f;
                    string oddsText = "-";
                    if (IsItemValid(itemProp) && w > 0f && cache.TotalWeight > 0f)
                    {
                        float pct = w / cache.TotalWeight * 100f;
                        oddsText = $"{pct:0.##}%";
                    }

                    var rightAlignMini = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
                    EditorGUI.LabelField(oddsRect, new GUIContent(oddsText, "Approximate odds based on current total"), rightAlignMini);
                };

                list.onCanAddCallback = l => property.isExpanded;
                list.onCanRemoveCallback = l => property.isExpanded;

                _lists[key] = cache;
            }

            // update total for this frame
            cache.TotalWeight = total;

            // Draw list (collapsed still needs a header)
            var listToDraw = cache.List;
            if (!property.isExpanded)
            {
                // Draw only header
                var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                listToDraw.drawHeaderCallback?.Invoke(headerRect);
            }
            else
            {
                float y = position.y;
                if (avoidProp != null)
                {
                    Rect avoidRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(
                        avoidRect,
                        avoidProp,
                        new GUIContent(
                            "Avoid same pick twice in a row",
                            "When enabled, the next roll excludes the previous result from the weighted pool. If only one entry has weight, that entry is returned again."));
                    y += EditorGUIUtility.singleLineHeight + 2f;
                }

                if (prdProp != null)
                {
                    Rect prdRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(
                        prdRect,
                        prdProp,
                        new GUIContent(
                            "Pseudo-Random Distribution",
                            "Like Dota 2's PRD: items that haven't been picked get progressively higher odds each roll, preventing long dry streaks. Resets when the item is finally picked."));
                    y += EditorGUIUtility.singleLineHeight + 2f;
                }

                float listHeight = listToDraw.GetHeight();
                Rect listRect = new Rect(position.x, y, position.width, listHeight);
                listToDraw.DoList(listRect);
            }

            HandleDragAndDrop(position, property, entriesProp);
        }

        // Runs after the list is drawn so a drop on a row's own object field still replaces that row;
        // a drop anywhere else on the drawer appends every valid unique dragged item.
        private void HandleDragAndDrop(Rect position, SerializedProperty property, SerializedProperty entriesProp)
        {
            Event evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;

            if (!position.Contains(evt.mousePosition))
                return;

            Type itemType = GetItemType();
            if (itemType == null || !typeof(Object).IsAssignableFrom(itemType))
                return;

            List<Object> items = GetDroppableItems(itemType, property, entriesProp);
            if (items.Count == 0)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object item in items)
                {
                    entriesProp.arraySize++;
                    SerializedProperty element = entriesProp.GetArrayElementAtIndex(entriesProp.arraySize - 1);
                    element.FindPropertyRelative("Item").objectReferenceValue = item;
                    element.FindPropertyRelative("Weight").floatValue = 1f;
                }

                property.isExpanded = true;
                property.serializedObject.ApplyModifiedProperties();
            }

            evt.Use();
        }

        private Type GetItemType()
        {
            Type type = fieldInfo?.FieldType;
            if (type == null)
                return null;

            if (type.IsArray)
                type = type.GetElementType();
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                type = type.GetGenericArguments()[0];

            for (; type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LootTable<>))
                    return type.GetGenericArguments()[0];
            }

            return null;
        }

        private static List<Object> GetDroppableItems(Type itemType, SerializedProperty property, SerializedProperty entriesProp)
        {
            List<Object> result = new List<Object>();
            Object[] draggedObjects = DragAndDrop.objectReferences;
            if (draggedObjects == null || draggedObjects.Length == 0)
                return result;

            // A persistent asset cannot reference scene objects — Unity nulls them on apply
            bool targetIsAsset = EditorUtility.IsPersistent(property.serializedObject.targetObject);

            HashSet<Object> seen = new HashSet<Object>();
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty itemProp = entriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Item");
                if (itemProp != null && itemProp.propertyType == SerializedPropertyType.ObjectReference && itemProp.objectReferenceValue != null)
                    seen.Add(itemProp.objectReferenceValue);
            }

            foreach (Object draggedObject in draggedObjects)
            {
                Object item = ResolveDraggedItem(draggedObject, itemType);
                if (item == null)
                    continue;

                if (targetIsAsset && !EditorUtility.IsPersistent(item))
                    continue;

                if (!seen.Add(item))
                    continue;

                result.Add(item);
            }

            return result;
        }

        private static Object ResolveDraggedItem(Object draggedObject, Type itemType)
        {
            if (draggedObject == null)
                return null;

            if (itemType.IsInstanceOfType(draggedObject))
                return draggedObject;

            // Same convenience as ObjectField: a dragged GameObject satisfies a Component-typed table
            if (draggedObject is GameObject gameObject && typeof(Component).IsAssignableFrom(itemType))
                return gameObject.GetComponent(itemType);

            return null;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Avoid NRE when SerializedObject is invalid (destroyed object, multi-edit edge cases)
            if (property?.serializedObject?.targetObject == null)
                return EditorGUIUtility.singleLineHeight + 4f;

            SerializedProperty entriesProp = property.FindPropertyRelative("_entries");
            if (entriesProp == null || !entriesProp.isArray)
                return EditorGUI.GetPropertyHeight(property, label, true);

            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight + 4f;

            // Use the list's own height if already cached — most accurate
            string key = GetKey(property);
            SerializedProperty avoidProp = property.FindPropertyRelative("_avoidConsecutiveDuplicate");
            SerializedProperty prdProp = property.FindPropertyRelative("_pseudoRandom");
            float avoidExtra = avoidProp != null ? EditorGUIUtility.singleLineHeight + 2f : 0f;
            float prdExtra = prdProp != null ? EditorGUIUtility.singleLineHeight + 2f : 0f;
            avoidExtra += prdExtra;

            if (_lists.TryGetValue(key, out ListCache cache) && cache?.List != null
                                                             && cache.SerializedObject == property.serializedObject
                                                             && cache.SerializedObject?.targetObject != null)
            {
                return avoidExtra + cache.List.GetHeight();
            }

            // Fallback: match ReorderableList defaults (header=18, footer=13) instead of
            // using elementHeight for the header, which was under-reporting footer space
            float lineHeight = EditorGUIUtility.singleLineHeight;
            int count = Mathf.Max(1, entriesProp.arraySize);
            float elementsTotal = 0f;
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty element = entriesProp.GetArrayElementAtIndex(i);
                SerializedProperty itemProp = element.FindPropertyRelative("Item");
                float itemHeight = itemProp != null
                    ? EditorGUI.GetPropertyHeight(itemProp, GUIContent.none, true)
                    : lineHeight;
                elementsTotal += Mathf.Max(lineHeight, itemHeight) + 6f;
            }
            if (entriesProp.arraySize == 0)
                elementsTotal = (lineHeight + 6f) * count;
            return avoidExtra + 18f + elementsTotal + 13f;
        }

        private static string GetKey(SerializedProperty property)
        {
            var obj = property.serializedObject.targetObject;
            ulong id = obj != null ? EntityId.ToULong(obj.GetEntityId()) : 0;
            return id + ":" + property.propertyPath;
        }

        private static bool IsItemValid(SerializedProperty itemProp)
        {
            if (itemProp == null) return false;
            switch (itemProp.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return itemProp.objectReferenceValue != null;
                case SerializedPropertyType.String:
                    return !string.IsNullOrEmpty(itemProp.stringValue);
                default:
                    // For other serializable types, assume not-null if it's not boxed null which Unity can't represent; treat as valid
                    return true;
            }
        }
    }
}
#endif
