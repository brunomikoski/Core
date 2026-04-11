using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Core.Events
{
    public static class SafeEventsFinder
    {
        private static Type eventsType;
        private static Type EventsType
        {
            get
            {
                if (eventsType == null)
                {
                    eventsType = typeof(SafeEventBase).Assembly.GetTypes().FirstOrDefault(
                        t => t.Name == "Events" && t.IsClass && t.IsAbstract && t.IsSealed);
                }
                return eventsType;
            }
        }

        public static void FindEvents(SafeEventHierarchyGroup rootGroup)
        {
            rootGroup.children.Clear();

            FindEventsInStaticType(EventsType, rootGroup);

            Dictionary<Type, SafeEventHierarchyGroup> typeToEventGroup
                = new Dictionary<Type, SafeEventHierarchyGroup>();
            MonoBehaviour[] monoBehaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            for (int i = 0; i < monoBehaviours.Length; ++i)
            {
                if (monoBehaviours[i].gameObject.scene.IsValid())
                    FindEventsInType(monoBehaviours[i], monoBehaviours[i].GetType(), rootGroup,
                        typeToEventGroup);
            }

            ScriptableObject[] scriptableObjects =
                Resources.FindObjectsOfTypeAll<ScriptableObject>();

            for (int i = 0; i < scriptableObjects.Length; ++i)
            {
                FindEventsInType(scriptableObjects[i], scriptableObjects[i].GetType(), rootGroup,
                    typeToEventGroup);
            }

            FindEventsRegisteredByContent(rootGroup, typeToEventGroup);
        }

        private static void FindEventsRegisteredByContent(SafeEventHierarchyGroup rootGroup,
            Dictionary<Type, SafeEventHierarchyGroup> typeToEventGroup)
        {
            Dictionary<object, List<SafeEventBase>> contextsToEvents
                = SafeEventBase.GetEventContextToEvents();
            foreach (var contextToEvents in contextsToEvents)
            {
                object target = contextToEvents.Key;
                Type targetType = target.GetType();
                SafeEventHierarchyGroup typeSafeEventGroup;

                if (typeToEventGroup.ContainsKey(targetType))
                {
                    typeSafeEventGroup = typeToEventGroup[targetType];
                }
                else
                {
                    typeSafeEventGroup = new SafeEventHierarchyGroup()
                    {
                        name = target.GetType().Name,
                        parent = rootGroup
                    };
                    rootGroup.children.Add(typeSafeEventGroup);
                    typeToEventGroup[targetType] = typeSafeEventGroup;
                }

                SafeEventHierarchyGroup safeEventGroup;

                safeEventGroup = new SafeEventHierarchyGroup()
                {
                    name = target.ToString().FirstToUpper(),
                    parent = typeSafeEventGroup
                };
                typeSafeEventGroup.children.Add(safeEventGroup);

                for (int i = 0; i < contextToEvents.Value.Count; i++)
                {
                    SafeEventBase eventBase = contextToEvents.Value[i];

                    FieldInfo fieldInfo = FindFieldWithValue(target, eventBase);
                    safeEventGroup.children.Add(new SafeSafeEventHierarchySafeEvent()
                    {
                        name = fieldInfo.Name.FirstToUpper(),
                        eventBase = eventBase,
                        parent = safeEventGroup
                    });
                }
            }
        }

        private static FieldInfo FindFieldWithValue(object target, SafeEventBase eventBase)
        {
            Type type = target.GetType();

            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic
                | BindingFlags.Public | BindingFlags.FlattenHierarchy);

            for (int i = 0; i < fieldInfos.Length; i++)
            {
                FieldInfo fieldInfo = fieldInfos[i];

                if (fieldInfo.GetValue(target) == eventBase)
                {
                    return fieldInfo;
                }
            }
            return null;
        }


        private static SafeEventHierarchyGroup FindEventsInType(object target, Type eventsType,
            SafeEventHierarchyGroup safeEventGroup, Dictionary<Type, SafeEventHierarchyGroup> typeToEventGroup)
        {
            if (eventsType == typeof(MonoBehaviour))
                return null;

            if (eventsType == typeof(ScriptableObject))
                return null;

            Type targetType = target.GetType();

            SafeEventHierarchyGroup targetSafeEventGroup
                = FindEventsInType(target, eventsType.BaseType, safeEventGroup, typeToEventGroup);

            FieldInfo[] fieldInfos =
                eventsType.GetFields(BindingFlags.Static | BindingFlags.Public
                                     | BindingFlags.Instance | BindingFlags.NonPublic);

            for (int i = 0; i < fieldInfos.Length; ++i)
            {
                FieldInfo fieldInfo = fieldInfos[i];
                if (!typeof(SafeEventBase).IsAssignableFrom(fieldInfo.FieldType))
                    continue;

                if (targetSafeEventGroup == null)
                {
                    string name = targetType.Name;

                    Object asObject = target as Object;
                    if (asObject != null)
                    {
                        name = asObject.name;
                    }

                    if (typeToEventGroup.ContainsKey(targetType))
                    {
                        safeEventGroup = typeToEventGroup[targetType];
                    }
                    else
                    {
                        targetSafeEventGroup = new SafeEventHierarchyGroup()
                        {
                            name = targetType.Name,
                            parent = safeEventGroup
                        };
                        safeEventGroup.children.Add(targetSafeEventGroup);
                        safeEventGroup = targetSafeEventGroup;
                        typeToEventGroup[targetType] = targetSafeEventGroup;
                    }

                    targetSafeEventGroup = new SafeEventHierarchyGroup()
                    {
                        name = name.FirstToUpper(),
                        unityObject = asObject,
                        parent = safeEventGroup
                    };
                    safeEventGroup.children.Add(targetSafeEventGroup);
                    safeEventGroup = targetSafeEventGroup;
                }

                safeEventGroup.children.Add(new SafeSafeEventHierarchySafeEvent()
                {
                    name = fieldInfo.Name.FirstToUpper(),
                    eventBase = (SafeEventBase)fieldInfo.GetValue(target),
                    parent = safeEventGroup
                });
            }

            return targetSafeEventGroup;
        }

        private static void FindEventsInStaticType(Type eventsType, SafeEventHierarchyGroup safeEventGroup)
        {
            if (eventsType == null)
                return;

            FieldInfo[] fieldInfos =
                eventsType.GetFields(BindingFlags.Static | BindingFlags.Public
                                     | BindingFlags.NonPublic);

            for (int i = 0; i < fieldInfos.Length; ++i)
            {
                FieldInfo fieldInfo = fieldInfos[i];
                if (typeof(SafeEventBase).IsAssignableFrom(fieldInfo.FieldType))
                {
                    safeEventGroup.children.Add(new SafeSafeEventHierarchySafeEvent()
                    {
                        name = fieldInfo.Name.FirstToUpper(),
                        eventBase = (SafeEventBase)fieldInfo.GetValue(null),
                        parent = safeEventGroup
                    });
                }
            }

            Type[] nestedTypes =
                eventsType.GetNestedTypes(BindingFlags.Static | BindingFlags.Public);

            for (int i = 0; i < nestedTypes.Length; i++)
            {
                Type nestedType = nestedTypes[i];
                if (!nestedType.IsAbstract || !nestedType.IsSealed)
                    continue;

                SafeEventHierarchyGroup childGroup =
                    new SafeEventHierarchyGroup()
                    {
                        name = nestedType.Name,
                        parent = safeEventGroup
                    };
                safeEventGroup.children.Add(childGroup);
                FindEventsInStaticType(nestedType, childGroup);
            }
        }
    }
}
