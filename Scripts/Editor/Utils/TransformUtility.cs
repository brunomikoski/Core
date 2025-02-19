using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core
{
    public static class TransformUtility
    {
        [MenuItem("GameObject/Align First Game Object to Second &a", false, 500)]
        private static void AlignFirstGameObjectToSecond()
        {
            // NOTE: Need to get the selection this way to ensure it is in chronological order.
            Transform[] transforms = Selection.objects.OfType<GameObject>().Select(g => g.transform).ToArray();

            Transform from = transforms[0];
            Transform to = transforms[1];

            Undo.RecordObject(from, "Align First Object To Second");
            from.position = to.position;
            from.rotation = to.rotation;
            from.localScale = to.localScale;
        }

        [MenuItem("GameObject/Align First Game Object to Second &a", true)]
        private static bool ValidateAlignFirstGameObjectToSecond()
        {
            return Selection.transforms.Length == 2;
        }
    }
}