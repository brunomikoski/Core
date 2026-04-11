using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BrunoMikoski.Utils
{
    public static class TransformUtilities
    {
        private const string CREATE_PARENT_MENU_NAME = "GameObject/Create Parent at Center";
        private static readonly List<GameObject> createParentCache = new List<GameObject>();

        [MenuItem(CREATE_PARENT_MENU_NAME, false, 0)]
        private static void CreateParent(MenuCommand menuCommand)
        {
            GameObject context = (GameObject)menuCommand.context;

            if (createParentCache.Count > 0 && !createParentCache.Contains(context))
            {
                Debug.LogError("Item not found in cache!");
                return;
            }

            if (createParentCache.Count == 0)
            {
                createParentCache.AddRange(Selection.objects.Cast<GameObject>());
                createParentCache.Remove(context);
            }
            else
            {
                createParentCache.Remove(context);
                return;
            }

            List<GameObject> gameObjects = Selection.objects.Cast<GameObject>().ToList();

            Vector3 position = gameObjects.Aggregate(new Vector3(0, 0, 0), (v, o) => v += o.transform.position);
            Vector3 average = position / gameObjects.Count;

            Transform originalParent = gameObjects.First().transform.parent;

            Undo.SetCurrentGroupName("Create Parent");
            int group = Undo.GetCurrentGroup();

            GameObject parent = new GameObject(gameObjects.Count == 1
                ? $"Parent ({gameObjects[0].name})"
                : "Parent");
            Undo.RegisterCreatedObjectUndo(parent, "Create Parent");

            parent.transform.position = average;
            parent.transform.SetParent(originalParent, true);

            foreach (GameObject gameObject in gameObjects)
            {
                Undo.SetTransformParent(gameObject.transform, parent.transform, "Parent Objects");
            }

            Undo.CollapseUndoOperations(group);

            EditorSceneManager.MarkSceneDirty(parent.scene);
        }

        [MenuItem(CREATE_PARENT_MENU_NAME, true, 0)]
        private static bool CreateParentValidation()
        {
            Object[] objects = Selection.objects;

            return objects.Length != 0 && objects.All(x => x is GameObject);
        }
    }
}
