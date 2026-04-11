using System;
using System.Collections.Generic;
using BrunoMikoski.Core.Observables;
using BrunoMikoski.ScriptableObjectCollections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
#if LOCALIZATION_ENABLED
using UnityEngine.Localization;
#endif
using UnityEngine.UI;

namespace BrunoMikoski.Extensions.Observables
{
    public readonly struct UIBinding : IDisposable
    {
        private readonly Action _dispose;
        public UIBinding(Action dispose) { _dispose = dispose; }
        public void Dispose() { _dispose?.Invoke(); }
    }
    public static class ObservablesUIExtensions
    {
        private static int GetIndexFromSteps(float targetFloat, float[] availableSteps)
        {
            if (availableSteps == null || availableSteps.Length == 0)
                return -1;

            int closestIndex = 0;
            float minDifference = Mathf.Abs(targetFloat - availableSteps[0]);

            for (int i = 1; i < availableSteps.Length; i++)
            {
                float difference = Mathf.Abs(targetFloat - availableSteps[i]);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private static int GetIndexFromSteps(int targetFloat, int[] availableSteps)
        {
            if (availableSteps == null || availableSteps.Length == 0)
                return -1;

            int closestIndex = 0;
            float minDifference = Mathf.Abs(targetFloat - availableSteps[0]);

            for (int i = 1; i < availableSteps.Length; i++)
            {
                float difference = Mathf.Abs(targetFloat - availableSteps[i]);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }


        public static UIBinding Bind(this Observable<float> observable, Slider slider, float[] availableSteps = null, Action onValueChanged = null)
        {
            if (observable == null || slider == null)
            {
                Debug.LogError("Observable or Slider is null, binding aborted.");
                return new UIBinding(null);
            }

            if (availableSteps != null && availableSteps.Length > 1)
            {
                slider.minValue = 0;
                slider.maxValue = availableSteps.Length - 1;
                slider.wholeNumbers = true;
            }

            if (availableSteps != null && availableSteps.Length > 0)
            {
                slider.SetValueWithoutNotify(GetIndexFromSteps(observable.Value, availableSteps));
            }
            else
            {
                slider.SetValueWithoutNotify(observable.Value);
            }

            UnityAction<float> valueChanged = newValue =>
            {
                if (availableSteps != null)
                {
                    newValue = availableSteps[Mathf.RoundToInt(newValue)];
                }

                if (!Mathf.Approximately(newValue, observable.Value))
                {
                    observable.Value = newValue;
                }
            };

            slider.onValueChanged.AddListener(valueChanged);

            Action<float, float> observableValueChanged = (_, current) =>
            {
                if (availableSteps != null && availableSteps.Length > 0)
                {
                    int index = GetIndexFromSteps(current, availableSteps);
                    if (!Mathf.Approximately(slider.value, index))
                    {
                        slider.SetValueWithoutNotify(index);
                    }
                }
                else
                {
                    if (!Mathf.Approximately(slider.value, current))
                    {
                        slider.SetValueWithoutNotify(current);
                    }
                }
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                slider.onValueChanged.RemoveListener(valueChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }

        public static UIBinding Bind(this Observable<int> observable, Slider slider, int[] availableSteps = null, Action onValueChanged = null)
        {
            if (observable == null || slider == null)
            {
                Debug.LogError("Observable or Slider is null, binding aborted.");
                return new UIBinding(null);
            }

            if (availableSteps != null && availableSteps.Length > 1)
            {
                slider.minValue = 0;
                slider.maxValue = availableSteps.Length - 1;
                slider.wholeNumbers = true;
            }

            if (availableSteps != null && availableSteps.Length > 0)
            {
                slider.SetValueWithoutNotify(GetIndexFromSteps(observable.Value, availableSteps));
            }
            else
            {
                slider.SetValueWithoutNotify(observable.Value);
            }

            UnityAction<float> valueChanged = newValue =>
            {
                if (availableSteps != null)
                {
                    newValue = availableSteps[Mathf.RoundToInt(newValue)];
                }

                int intValue = Mathf.RoundToInt(newValue);
                if (intValue != observable.Value)
                {
                    observable.Value = intValue;
                }
            };

            slider.onValueChanged.AddListener(valueChanged);

            Action<int, int> observableValueChanged = (_, current) =>
            {
                if (availableSteps != null && availableSteps.Length > 0)
                {
                    int idx = GetIndexFromSteps(current, availableSteps);
                    if (Mathf.RoundToInt(slider.value) != idx)
                    {
                        slider.SetValueWithoutNotify(idx);
                    }
                }
                else
                {
                    if (Mathf.RoundToInt(slider.value) != current)
                    {
                        slider.SetValueWithoutNotify(current);
                    }
                }
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                slider.onValueChanged.RemoveListener(valueChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }

        public static UIBinding Bind(this Observable<float> observable, Slider slider, TMP_Text text, string format = "", Action onValueChanged = null)
        {
            if (observable == null || slider == null || text == null)
            {
                Debug.LogError("Observable, Slider or Text is null, binding aborted.");
                return new UIBinding(null);
            }

            UIBinding sliderBinding = observable.Bind(slider, null, onValueChanged);

            void TextUpdated()
            {
                text.SetText(observable.Value.ToString(format));
            }

            TextUpdated();
            observable.OnChangedEvent += TextUpdated;

            return new UIBinding(() =>
            {
                observable.OnChangedEvent -= TextUpdated;
                sliderBinding.Dispose();
            });
        }

        public static UIBinding Bind(this Observable<float> observable, Slider slider, TMP_Text text, float[] availableSteps, string format = "", Action onValueChanged = null)
        {
            if (observable == null || slider == null || text == null)
            {
                Debug.LogError("Observable, Slider or Text is null, binding aborted.");
                return new UIBinding(null);
            }

            UIBinding sliderBinding = observable.Bind(slider, availableSteps, onValueChanged);

            void TextUpdated()
            {
                text.SetText(observable.Value.ToString(format));
            }

            TextUpdated();
            observable.OnChangedEvent += TextUpdated;

            return new UIBinding(() =>
            {
                observable.OnChangedEvent -= TextUpdated;
                sliderBinding.Dispose();
            });
        }

        public static UIBinding Bind(this Observable<int> observable, Slider slider, TMP_Text text, int[] availableSteps, string format = "", Action onValueChanged = null)
        {
            if (observable == null || slider == null || text == null)
            {
                Debug.LogError("Observable, Slider or Text is null, binding aborted.");
                return new UIBinding(null);
            }

            UIBinding sliderBinding = observable.Bind(slider, availableSteps, onValueChanged);

            void TextUpdated()
            {
                text.SetText(observable.Value.ToString(format));
            }

            TextUpdated();
            observable.OnChangedEvent += TextUpdated;

            return new UIBinding(() =>
            {
                observable.OnChangedEvent -= TextUpdated;
                sliderBinding.Dispose();
            });
        }

        public static UIBinding Bind(this Observable<int> observable, Slider slider, TMP_Text text, string format = "", Action onValueChanged = null)
        {
            if (observable == null || slider == null)
            {
                Debug.LogError("Observable or Slider is null, binding aborted.");
                return new UIBinding(null);
            }

            slider.SetValueWithoutNotify(observable.Value);

            UnityAction<float> valueChanged = newValue =>
            {
                int newInt = Mathf.RoundToInt(newValue);
                if (newInt != observable.Value)
                {
                    observable.Value = newInt;
                }
            };

            slider.onValueChanged.AddListener(valueChanged);

            Action<int, int> observableValueChanged = (_, current) =>
            {
                if (Mathf.RoundToInt(slider.value) == current)
                    return;

                slider.SetValueWithoutNotify(current);
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            void TextUpdated()
            {
                text.SetText(observable.Value.ToString(format));
            }

            TextUpdated();
            observable.OnChangedEvent += TextUpdated;

            return new UIBinding(() =>
            {
                slider.onValueChanged.RemoveListener(valueChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
                observable.OnChangedEvent -= TextUpdated;
            });
        }

        public static UIBinding Bind(this Observable<bool> observable, Toggle toggle, Action onValueChanged = null)
        {
            if (observable == null || toggle == null)
            {
                Debug.LogError("Observable or Toggle is null, binding aborted.");
                return new UIBinding(null);
            }

            toggle.SetIsOnWithoutNotify(observable.Value);

            UnityAction<bool> toggleValueChanged = newValue =>
            {
                if (newValue == observable.Value)
                    return;

                observable.Value = newValue;
            };

            toggle.onValueChanged.AddListener(toggleValueChanged);

            Action<bool, bool> observableValueChanged = (prev, current) =>
            {
                if (toggle.isOn == current)
                    return;

                toggle.SetIsOnWithoutNotify(current);
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                toggle.onValueChanged.RemoveListener(toggleValueChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }

        public static UIBinding Bind(this Observable<bool> observable, Selectable selectable, bool inverse = false, Action onValueChanged = null)
        {
            if (observable == null || selectable == null)
            {
                Debug.LogError("Observable or Selectable is null, binding aborted.");
                return new UIBinding(null);
            }

            bool GetInteractable(bool value) => inverse ? !value : value;

            selectable.interactable = GetInteractable(observable.Value);

            Action<bool, bool> observableValueChanged = (prev, current) =>
            {
                if (selectable == null)
                    return;

                bool targetInteractable = GetInteractable(current);
                if (selectable.interactable == targetInteractable)
                    return;

                selectable.interactable = targetInteractable;
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }

#if LOCALIZATION_ENABLED
        public static UIBinding Bind<T>(this Observable<T> observable, TMP_Dropdown dropDown, LocalizedString[] displayValues, Action onValueChanged = null) where T : Enum
        {
            string[] stringDisplayValues = null;

            if (displayValues != null && displayValues.Length > 0)
            {
                stringDisplayValues = new string[displayValues.Length];
                for (int i = 0; i < displayValues.Length; i++)
                {
                    if (displayValues[i] != null && !displayValues[i].IsEmpty)
                    {
                        stringDisplayValues[i] = displayValues[i].GetLocalizedString();
                    }
                }
            }

            return Bind(observable, dropDown, stringDisplayValues, onValueChanged);
        }
#endif

        public static UIBinding Bind<T>(this Observable<T> observable, TMP_Dropdown dropDown, string[] displayValues, Action onValueChanged = null) where T : Enum
        {
            if (observable == null || dropDown == null)
            {
                Debug.LogError("Observable or Dropdown is null, binding aborted.");
                return new UIBinding(null);
            }

            dropDown.ClearOptions();
            Array values = Enum.GetValues(typeof(T));
            int count = 0;
            foreach (object value in values)
            {
                string text = value.ToString();

                if (displayValues != null && displayValues.Length > 0 && count < displayValues.Length && displayValues[count] != null)
                {
                    text = displayValues[count];
                }

                dropDown.options.Add(new TMP_Dropdown.OptionData(text));
                count++;
            }

            dropDown.value = Convert.ToInt32(observable.Value);
            dropDown.RefreshShownValue();

            UnityAction<int> dropdownChanged = newValue =>
            {
                T enumValue = (T)Enum.ToObject(typeof(T), newValue);
                if (EqualityComparer<T>.Default.Equals(enumValue, observable.Value))
                    return;

                observable.Value = enumValue;
            };

            dropDown.onValueChanged.AddListener(dropdownChanged);

            Action<T, T> observableValueChanged = (prev, current) =>
            {
                int currentValue = Convert.ToInt32(current);
                if (dropDown.value == currentValue)
                    return;

                dropDown.SetValueWithoutNotify(currentValue);
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                dropDown.onValueChanged.RemoveListener(dropdownChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }

        public static UIBinding Bind<TRef, TItem>(this Observable<TRef> observable, TMP_Dropdown dropDown, Action onValueChanged = null)
            where TRef : CollectionItemIndirectReference<TItem>, new()
            where TItem : ScriptableObjectCollectionItem
        {
            if (observable == null || dropDown == null)
            {
                Debug.LogError("Observable or Dropdown is null, binding aborted.");
                return new UIBinding(null);
            }

            TItem currentItem = observable.Value;

            ScriptableObjectCollection collection = currentItem != null
                ? currentItem.Collection
                : CollectionsRegistry.Instance.TryGetCollectionFromItemType(typeof(TItem), out ScriptableObjectCollection c) ? c : null;

            if (collection == null)
            {
                Debug.LogError($"Could not find collection for {typeof(TItem).Name}.");
                return new UIBinding(null);
            }

            dropDown.ClearOptions();
            foreach (ScriptableObject so in collection)
            {
                string text = so.name;
#if LOCALIZATION_ENABLED
                if (so is ILocalizedDisplayValue ldv && !ldv.DisplayValue.IsEmpty)
                    text = ldv.DisplayValue.GetLocalizedString();
#endif

                dropDown.options.Add(new TMP_Dropdown.OptionData(text));
            }

            int currentIndex = currentItem != null ? currentItem.Index : 0;
            currentIndex = Mathf.Clamp(currentIndex, 0, Mathf.Max(0, dropDown.options.Count - 1));
            dropDown.SetValueWithoutNotify(currentIndex);
            dropDown.RefreshShownValue();

            UnityAction<int> dropdownChanged = newValue =>
            {
                TItem newItem = (TItem)collection[newValue];
                TRef newRef = new TRef();
                newRef.FromCollectionItem(newItem);

                observable.Value = newRef;
            };

            dropDown.onValueChanged.AddListener(dropdownChanged);

            Action<TRef, TRef> observableValueChanged = (_, current) =>
            {
                TItem curItem = current; 
                int idx = curItem != null ? curItem.Index : 0;
                if (dropDown.value != idx)
                    dropDown.SetValueWithoutNotify(idx);
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                dropDown.onValueChanged.RemoveListener(dropdownChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }
        
        public static UIBinding Bind<T>(this Observable<T> observable, TMP_Dropdown dropDown, Action onValueChanged = null) where T : ScriptableObjectCollectionItem
        {
            if (observable == null || dropDown == null)
            {
                Debug.LogError("Observable or Dropdown is null, binding aborted.");
                return new UIBinding(null);
            }

            dropDown.ClearOptions();

            foreach (ScriptableObject scriptableObject in observable.Value.Collection)
            {
                string displayValue = scriptableObject.name;
#if LOCALIZATION_ENABLED                
                if (scriptableObject is ILocalizedDisplayValue localizedDisplayValue)
                {
                    displayValue = localizedDisplayValue.DisplayValue.GetLocalizedString();
                }
#endif
                dropDown.options.Add(new TMP_Dropdown.OptionData(displayValue));
            }
            
            dropDown.value = observable.Value.Index;
            dropDown.RefreshShownValue();

            UnityAction<int> dropdownChanged = newValue =>
            {
                ScriptableObjectCollectionItem newItem = (ScriptableObjectCollectionItem)observable.Value.Collection[newValue];
                if (newItem == observable.Value)
                    return;

                observable.Value = (T)newItem;
            };

            dropDown.onValueChanged.AddListener(dropdownChanged);

            Action<T, T> observableValueChanged = (prev, current) =>
            {
                if (dropDown.value == current.Index)
                    return;

                dropDown.SetValueWithoutNotify(current.Index);
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                dropDown.onValueChanged.RemoveListener(dropdownChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }
        

        public static UIBinding Bind(this Observable<int> observable, TMP_Dropdown dropDown, string[] displayValues, Action onValueChanged = null)
        {
            if (observable == null || dropDown == null)
            {
                Debug.LogError("Observable or Dropdown is null, binding aborted.");
                return new UIBinding(null);
            }

            dropDown.ClearOptions();
            foreach (string value in displayValues)
            {
                dropDown.options.Add(new TMP_Dropdown.OptionData(value));
            }

            dropDown.value = observable.Value;
            dropDown.RefreshShownValue();

            UnityAction<int> dropdownChanged = newValue =>
            {
                if (newValue == observable.Value)
                    return;

                observable.Value = newValue;
            };

            dropDown.onValueChanged.AddListener(dropdownChanged);

            Action<int, int> observableValueChanged = (prev, current) =>
            {
                if (dropDown.value == current)
                    return;

                dropDown.SetValueWithoutNotify(current);
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                dropDown.onValueChanged.RemoveListener(dropdownChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }
        
        public static UIBinding Bind(this Observable<int> observable, TMP_Dropdown dropDown, Action onValueChanged = null)
        {
            if (observable == null || dropDown == null)
            {
                Debug.LogError("Observable or Dropdown is null, binding aborted.");
                return new UIBinding(null);
            }

            dropDown.value = observable.Value;

            UnityAction<int> dropdownChanged = newValue =>
            {
                if (newValue == observable.Value)
                    return;

                observable.Value = newValue;
            };

            dropDown.onValueChanged.AddListener(dropdownChanged);

            Action<int, int> observableValueChanged = (prev, current) =>
            {
                if (dropDown.value == current)
                    return;

                dropDown.SetValueWithoutNotify(current);
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                dropDown.onValueChanged.RemoveListener(dropdownChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }

        public static UIBinding Bind(this Observable<string> observable, TMP_Text text, Action onValueChanged = null)
        {
            if (observable == null || text == null)
            {
                Debug.LogError("Observable or Text is null, binding aborted.");
                return new UIBinding(null);
            }

            text.SetText(observable.Value ?? string.Empty);

            Action<string, string> observableValueChanged = (prev, current) =>
            {
                string currentSafe = current ?? string.Empty;
                if (text.text == currentSafe)
                    return;

                text.SetText(currentSafe);
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }

        public static UIBinding Bind(this Observable<string> observable, TMP_InputField inputField, bool useEndEdit = true, Action onValueChanged = null)
        {
            if (observable == null || inputField == null)
            {
                Debug.LogError("Observable or InputField is null, binding aborted.");
                return new UIBinding(null);
            }

            inputField.SetTextWithoutNotify(observable.Value ?? string.Empty);

            UnityAction<string> inputChanged = newValue =>
            {
                if (newValue == observable.Value)
                    return;

                observable.Value = newValue;
            };

            if (useEndEdit)
            {
                inputField.onEndEdit.AddListener(inputChanged);
            }
            else
            {
                inputField.onValueChanged.AddListener(inputChanged);
            }

            Action<string, string> observableValueChanged = (prev, current) =>
            {
                if (inputField == null)
                    return;

                string currentSafe = current ?? string.Empty;
                if (inputField.text == currentSafe)
                    return;

                inputField.SetTextWithoutNotify(currentSafe);
            };

            observable.OnChangedValuesEvent += observableValueChanged;

            if (onValueChanged != null)
            {
                observable.OnChangedEvent += onValueChanged;
            }

            return new UIBinding(() =>
            {
                if (useEndEdit)
                    inputField.onEndEdit.RemoveListener(inputChanged);
                else
                    inputField.onValueChanged.RemoveListener(inputChanged);
                observable.OnChangedValuesEvent -= observableValueChanged;
                if (onValueChanged != null)
                    observable.OnChangedEvent -= onValueChanged;
            });
        }
    }
}
