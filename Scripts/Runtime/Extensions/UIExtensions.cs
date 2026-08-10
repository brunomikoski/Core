using System;
using System.Collections;
using BrunoMikoski.Core.DevelopmentTools;
using BrunoMikoski.Core.DevelopmentTools.Routines;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BrunoMikoski.Core.DevelopmentTools.UI
{
   public static class UiExtensions
    {
        //http://answers.unity.com/answers/1610964/view.html
        //https://gist.github.com/Josef212/b3f4bcf9cd827f5dc125ffc013548491
        public static void SetPosX(this RectTransform rectTransform, float x)
        {
            rectTransform.anchoredPosition3D = new Vector3(x, rectTransform.anchoredPosition3D.y, rectTransform.anchoredPosition3D.z);
        }

        public static void SetHeight(this RectTransform rectTransform, float h)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
        }

        // added to 2019.1 https://docs.unity3d.com/2019.1/Documentation/ScriptReference/UI.Toggle.SetIsOnWithoutNotify.html
        public static void SetValue(this Toggle toggle, bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
        }

        public static void SetValue(this Slider slider, float v)
        {
            slider.SetValueWithoutNotify(v);
        }

        public static void SetValue(this InputField inputField, string v)
        {
            inputField.SetTextWithoutNotify(v);
        }

        // https://forum.unity.com/threads/deleting-all-chidlren-of-an-object.92827/#post-2058407
        /// <summary>
        /// Calls GameObject.Destroy on all children of transform. and immediately detaches the children
        /// from transform so after this call transform.childCount is zero.
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            if (null != transform && transform)
            {
                for (int i = transform.childCount - 1; i >= 0; --i)
                {
                    Transform child = transform.GetChild(i);
                    if (child && child.gameObject)
                    {
                        Object.Destroy(child.gameObject);
                    }
                }
                transform.DetachChildren();
            }
        }

        public static void Hide(this CanvasGroup canvasGroup, bool blockRaycasts = false, bool interactable = false)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = blockRaycasts;
            canvasGroup.interactable = interactable;
        }

        public static void Show(this CanvasGroup canvasGroup, bool blockRaycasts = true, bool interactable = true)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = blockRaycasts;
            canvasGroup.interactable = interactable;
        }

        public static bool IsHidden(this CanvasGroup canvasGroup)
        {
            return canvasGroup.alpha <= 0f;
        }

        public static bool IsShown(this CanvasGroup canvasGroup)
        {
            return canvasGroup.alpha > 0f;
        }

        public static void SetSingleOnClickCallback(this Button button, UnityAction action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        public static void SetSingleOnValueChangedCallback(this Toggle toggle, UnityAction<bool> action)
        {
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(action);
        }

        public static void SetSingleOnValueChangedCallback(this InputField inputField, UnityAction<string> action)
        {
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener(action);
        }

        public static void SetSingleOnEndEditCallback(this InputField inputField, UnityAction<string> action)
        {
            inputField.onEndEdit.RemoveAllListeners();
            inputField.onEndEdit.AddListener(action);
        }

        public static void SetSingleOnValueChangedCallback(this Dropdown inputField, UnityAction<int> action)
        {
            inputField.onValueChanged.RemoveAllListeners();
            inputField.onValueChanged.AddListener(action);
        }

        public static void SetSingleOnValueChangedCallback(this Slider slider, UnityAction<float> action)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(action);
        }
    }
}
