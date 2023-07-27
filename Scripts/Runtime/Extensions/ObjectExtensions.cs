using System;
using System.Linq;
using System.Reflection;

namespace BrunoMikoski.Core
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// This method checks if the Unity Object is null. It casts it first to a generic
        /// C# object, to prevent the operator== of UnityEngine.Object to be called, which
        /// also check whether the C++ backend of the UnityEngine.Object is destroyed or not.
        ///
        /// See also: http://blogs.unity3d.com/2014/05/16/custom-operator-should-we-keep-it/
        /// </summary>
        public static bool IsNull(this Object o)
        {
            return ((object)o) == null;
        }

        public static bool IsNotNull(this Object o)
        {
            return ((object)o) != null;
        }
        
        /// <summary>
        /// Expensive method that uses the normal method for checking if is destroyed, but if is still returning as alive,
        /// does the native call to the IsNativeObjectAlive, use carefully
        /// from: https://github.com/MegaPiggy/SALT/blob/9969007aee83c7e33005fa574fceb742bb7cb59b/Extensions/ObjectExtensions.cs#L545
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsDestroyedOrPendingToBeDestroyed(this Object obj)
        {
                if (object.ReferenceEquals(obj, null)) 
                    return true;
               
                if(obj is UnityEngine.Object unityObj)
                {
                    if ((obj as UnityEngine.Object) == null) 
                        return true;

                    if (!unityObj.IsNativeObjectAlive())
                        return true;
                }

                return false;
        }
        
        public static bool IsNativeObjectAlive(this UnityEngine.Object obj)
        {
            return obj.InvokeMethod<bool>("IsNativeObjectAlive");
        }
        
        public static T InvokeMethod<T>(this object obj, string name, params object[] list)
        {
            try
            {
                MethodInfo method = obj.GetType().GetInstanceMethod(name, list.Select(o => o.GetType()).ToArray());

                if (method == null) return default;

                return (T)method?.Invoke(obj, list);
            }
            catch
            {
                // ignored
            }

            return default;
        }
    }
}