using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Utils
{
    public static class HierarchyTransformUtility
    {
        [MenuItem("GameObject/Macros/Reset Parent Position (Keeping Children Position)", false, 0)]
        public static void ResetParentPositionKeepingChildren()
        {
            if (Selection.gameObjects.Length == 0)
                return;

            Undo.RecordObjects(Selection.transforms, "Reset Parent Position (Keeping Children)");

            foreach (GameObject go in Selection.gameObjects)
            {
                if (go == null)
                    continue;

                Transform parent = go.transform;
                Vector3 parentPosition = parent.position;

                // Parent at origin - nothing to do
                if (parentPosition == Vector3.zero)
                    continue;

                // Store world positions of all children before modifying
                int childCount = parent.childCount;
                if (childCount == 0)
                {
                    parent.position = Vector3.zero;
                    continue;
                }

                Vector3[] childWorldPositions = new Vector3[childCount];
                Transform[] children = new Transform[childCount];
                for (int i = 0; i < childCount; i++)
                {
                    children[i] = parent.GetChild(i);
                    childWorldPositions[i] = children[i].position;
                }

                // Record children for undo
                Undo.RecordObjects(children, "Reset Parent Position (Keeping Children)");

                // Reset parent position to origin
                parent.position = Vector3.zero;

                // Restore each child's world position (preserves visual placement)
                for (int i = 0; i < childCount; i++)
                {
                    children[i].position = childWorldPositions[i];
                }
            }
        }

        [MenuItem("GameObject/Macros/Reset Parent Position (Keeping Children Position)", true)]
        public static bool ValidateResetParentPositionKeepingChildren()
        {
            return Selection.gameObjects.Length > 0;
        }
    }
}
