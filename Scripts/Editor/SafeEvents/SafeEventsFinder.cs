using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BrunoMikoski.ScriptableObjectCollections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Core.SafeEvents
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

        public static void FindEvents(SafeSafeEventsesHierarchyGroup rootGroup)
        {
            rootGroup.children.Clear();

            FindEventsInStaticType(EventsType, rootGroup);

            Dictionary<Type, SafeSafeEventsesHierarchyGroup> typeToEventGroup
                = new Dictionary<Type, SafeSafeEventsesHierarchyGroup>();
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

        private static void FindEventsRegisteredByContent(SafeSafeEventsesHierarchyGroup rootGroup,
            Dictionary<Type, SafeSafeEventsesHierarchyGroup> typeToEventGroup)
        {
            Dictionary<object, List<SafeEventBase>> contextsToEvents
                = SafeEventBase.GetEventContextToEvents();
            foreach (var contextToEvents in contextsToEvents)
            {
                object target = contextToEvents.Key;
                Type targetType = target.GetType();
                SafeSafeEventsesHierarchyGroup typeSafeSafeEventsesGroup;

                if (typeToEventGroup.ContainsKey(targetType))
                {
                    typeSafeSafeEventsesGroup = typeToEventGroup[targetType];
                }
                else
                {
                    typeSafeSafeEventsesGroup = new SafeSafeEventsesHierarchyGroup()
                    {
                        name = target.GetType().Name,
                        parent = rootGroup
                    };
                    rootGroup.children.Add(typeSafeSafeEventsesGroup);
                    typeToEventGroup[targetType] = typeSafeSafeEventsesGroup;
                }

                SafeSafeEventsesHierarchyGroup safeSafeEventsesGroup;

                safeSafeEventsesGroup = new SafeSafeEventsesHierarchyGroup()
                {
                    name = target.ToString().FirstToUpper(),
                    parent = typeSafeSafeEventsesGroup
                };
                typeSafeSafeEventsesGroup.children.Add(safeSafeEventsesGroup);

                for (int i = 0; i < contextToEvents.Value.Count; i++)
                {
                    SafeEventBase eventBase = contextToEvents.Value[i];

                    FieldInfo fieldInfo = FindFieldWithValue(target, eventBase);
                    safeSafeEventsesGroup.children.Add(new SafeSafeEventsesHierarchySafeEvents()
                    {
                        name = fieldInfo.Name.FirstToUpper(),
                        eventBase = eventBase,
                        parent = safeSafeEventsesGroup
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


        private static SafeSafeEventsesHierarchyGroup FindEventsInType(object target, Type eventsType,
            SafeSafeEventsesHierarchyGroup safeSafeEventsesGroup, Dictionary<Type, SafeSafeEventsesHierarchyGroup> typeToEventGroup)
        {
            if (eventsType == typeof(MonoBehaviour))
                return null;

            if (eventsType == typeof(ScriptableObject))
                return null;

            Type targetType = target.GetType();

            SafeSafeEventsesHierarchyGroup targetSafeSafeEventsesGroup
                = FindEventsInType(target, eventsType.BaseType, safeSafeEventsesGroup, typeToEventGroup);

            FieldInfo[] fieldInfos =
                eventsType.GetFields(BindingFlags.Static | BindingFlags.Public
                                     | BindingFlags.Instance | BindingFlags.NonPublic);

            for (int i = 0; i < fieldInfos.Length; ++i)
            {
                FieldInfo fieldInfo = fieldInfos[i];
                if (!typeof(SafeEventBase).IsAssignableFrom(fieldInfo.FieldType))
                    continue;

                if (targetSafeSafeEventsesGroup == null)
                {
                    string name = targetType.Name;

                    Object asObject = target as Object;
                    if (asObject != null)
                    {
                        name = asObject.name;
                    }

                    if (typeToEventGroup.ContainsKey(targetType))
                    {
                        safeSafeEventsesGroup = typeToEventGroup[targetType];
                    }
                    else
                    {
                        targetSafeSafeEventsesGroup = new SafeSafeEventsesHierarchyGroup()
                        {
                            name = targetType.Name,
                            parent = safeSafeEventsesGroup
                        };
                        safeSafeEventsesGroup.children.Add(targetSafeSafeEventsesGroup);
                        safeSafeEventsesGroup = targetSafeSafeEventsesGroup;
                        typeToEventGroup[targetType] = targetSafeSafeEventsesGroup;
                    }

                    targetSafeSafeEventsesGroup = new SafeSafeEventsesHierarchyGroup()
                    {
                        name = name.FirstToUpper(),
                        unityObject = asObject,
                        parent = safeSafeEventsesGroup
                    };
                    safeSafeEventsesGroup.children.Add(targetSafeSafeEventsesGroup);
                    safeSafeEventsesGroup = targetSafeSafeEventsesGroup;
                }

                safeSafeEventsesGroup.children.Add(new SafeSafeEventsesHierarchySafeEvents()
                {
                    name = fieldInfo.Name.FirstToUpper(),
                    eventBase = (SafeEventBase)fieldInfo.GetValue(target),
                    parent = safeSafeEventsesGroup
                });
            }

            return targetSafeSafeEventsesGroup;
        }

        private static void FindEventsInStaticType(Type eventsType, SafeSafeEventsesHierarchyGroup safeSafeEventsesGroup)
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
                    safeSafeEventsesGroup.children.Add(new SafeSafeEventsesHierarchySafeEvents()
                    {
                        name = fieldInfo.Name.FirstToUpper(),
                        eventBase = (SafeEventBase)fieldInfo.GetValue(null),
                        parent = safeSafeEventsesGroup
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

                SafeSafeEventsesHierarchyGroup childGroup =
                    new SafeSafeEventsesHierarchyGroup()
                    {
                        name = nestedType.Name,
                        parent = safeSafeEventsesGroup
                    };
                safeSafeEventsesGroup.children.Add(childGroup);
                FindEventsInStaticType(nestedType, childGroup);
            }
        }
    }
}
