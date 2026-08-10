using System.Collections.Generic;
using BrunoMikoski.Core.DevelopmentTools.Triggers;
using BrunoMikoski.Core.DevelopmentTools.PhysX;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.DevelopmentTools.Utils
{
    public static class ColliderUtility
    {
        [MenuItem("GameObject/Macros/Encapsulate Object with BoxCollider (Based on Renderers)")]
        [MenuItem("CONTEXT/Macros/Encapsulate Object with BoxCollider (Based on Renderers)")]
        public static void EncapsulateObjectBasedOnRenderer()
        {
            if (Selection.objects.Length > 1)
                return;

            GameObject selection = Selection.activeGameObject;

            Undo.RecordObject(selection, "Generate Collider");

            BoxCollider boxCollider  = selection.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = Undo.AddComponent<BoxCollider>(selection);
            }

            Bounds localBounds = selection.GetCompleteLocalBounds();

            boxCollider.center = localBounds.center;
            boxCollider.size = localBounds.size;
        }

        [MenuItem("GameObject/Macros/Fix Collider/BoxTriggerVolume Scale issues on Selected Items")]
        public static void FixScaleIssues()
        {
            List<BoxTriggerVolume> targets = new List<BoxTriggerVolume>();
            List<BoxCollider> targetsColliders = new List<BoxCollider>();
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                GameObject gameObject = Selection.objects[i] as GameObject;
                if (gameObject == null)
                    continue;
                targets.AddRange(
                    gameObject.GetComponentsInChildren<BoxTriggerVolume>(true));

                targetsColliders.AddRange(gameObject.GetComponentsInChildren<BoxCollider>(true));
            }

            for (int i = 0; i < targets.Count; i++)
                FixSizeScale(targets[i]);

            for (int i = 0; i < targetsColliders.Count; i++)
            {
                FixSizeScale(targetsColliders[i]);
            }
        }

        [MenuItem("GameObject/Macros/Box Trigger Volume/Fix Object Scale to Size (With Children)")]
        [MenuItem("CONTEXT/BoxTriggerVolume/Fix Object Scale to Size (With Children)")]
        public static void FixObjectScaleToSizeWithChildren()
        {
            List<BoxTriggerVolume> targets = new List<BoxTriggerVolume>();
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                GameObject gameObject = Selection.objects[i] as GameObject;
                if (gameObject == null)
                    continue;
                targets.AddRange(
                    gameObject.GetComponentsInChildren<BoxTriggerVolume>(true));
            }

            for (int i = 0; i < targets.Count; i++)
                FixSizeScale(targets[i]);
        }

        [MenuItem("GameObject/Macros/Box Trigger Volume/Fix Object Scale to Size")]
        [MenuItem("CONTEXT/BoxTriggerVolume/Fix Object Scale to Size")]
        public static void FixObjectScaleToSize()
        {
            List<BoxTriggerVolume> targets = new List<BoxTriggerVolume>();
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                GameObject gameObject = Selection.objects[i] as GameObject;
                if (gameObject == null)
                    continue;
                targets.AddRange(gameObject.GetComponents<BoxTriggerVolume>());
            }

            for (int i = 0; i < targets.Count; i++)
                FixSizeScale(targets[i]);
        }


        [MenuItem("GameObject/Macros/BoxCollider/Fix Object Scale to Size")]
        [MenuItem("CONTEXT/BoxCollider/Fix Object Scale to Size")]
        public static void FixBoxColliderObjectScaleToSize()
        {
            List<BoxCollider> targets = new List<BoxCollider>();
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                GameObject gameObject = Selection.objects[i] as GameObject;
                if (gameObject == null)
                    continue;
                targets.AddRange(gameObject.GetComponents<BoxCollider>());
            }

            for (int i = 0; i < targets.Count; i++)
                FixSizeScale(targets[i]);
        }

        [MenuItem("GameObject/Macros/BoxCollider/Fix Object Scale to Size (With Children)")]
        [MenuItem("CONTEXT/BoxCollider/Fix Object Scale to Size (With Children)")]
        public static void FixBoxColliderObjectScaleToSizeWithChildren()
        {
            List<BoxCollider> targets = new List<BoxCollider>();
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                GameObject gameObject = Selection.objects[i] as GameObject;
                if (gameObject == null)
                    continue;
                targets.AddRange(gameObject.GetComponentsInChildren<BoxCollider>(true));
            }

            for (int i = 0; i < targets.Count; i++)
                FixSizeScale(targets[i]);
        }

        private static void FixSizeScale(BoxTriggerVolume targetBoxTriggerVolume)
        {
            BoxTriggerVolume boxTriggerVolume = targetBoxTriggerVolume;
            if (!Mathf.Approximately(boxTriggerVolume.transform.localScale.magnitude, 1.0f))
            {
                Vector3 finalScale = boxTriggerVolume.transform.localScale;
                finalScale.x *= boxTriggerVolume.LocalSize.x;
                finalScale.y *= boxTriggerVolume.LocalSize.y;
                finalScale.z *= boxTriggerVolume.LocalSize.z;

                SerializedObject boxTriggerSerialized = new SerializedObject(boxTriggerVolume);
                boxTriggerSerialized.FindProperty("size").vector3Value = finalScale;
                boxTriggerSerialized.ApplyModifiedProperties();

                boxTriggerVolume.transform.localScale = Vector3.one;
            }
        }

        private static void FixSizeScale(BoxCollider target)
        {
            if (!Mathf.Approximately(target.transform.localScale.magnitude, 1.0f))
            {
                Vector3 finalScale = target.transform.localScale;
                finalScale.x *= target.size.x;
                finalScale.y *= target.size.y;
                finalScale.z *= target.size.z;

                target.size = finalScale;
                target.transform.localScale = Vector3.one;
            }
        }
    }
}