using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BrunoMikoski.Core.DevelopmentTools
{
    public static class ObjectExtensions
    {
        private const string IS_NATIVE_OBJECT_ALIVE = "IsNativeObjectAlive";
        private static Dictionary<string, MethodInfo> nameToMethodInfoCache = new ();

        
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
            return obj.InvokeMethod<bool>(IS_NATIVE_OBJECT_ALIVE);
        }
        
        public static T InvokeMethod<T>(this object obj, string methodName, params object[] list)
        {
            try
            {
                Type type = obj.GetType();
                string key = $"{type.FullName}.{methodName}";

                if (!nameToMethodInfoCache.TryGetValue(key, out MethodInfo method))
                {
                    method = type.GetInstanceMethod(methodName, list.Select(o => o.GetType()).ToArray());
                    if (method == null)
                        return default;
                    
                    nameToMethodInfoCache.Add(key, method);
                }

                return (T)method.Invoke(obj, list);
            }
            catch
            {
                // ignored
            }

            return default;
        }
    }
}