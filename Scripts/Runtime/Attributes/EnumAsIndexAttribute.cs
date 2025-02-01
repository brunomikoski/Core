using System;
using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    /// <summary>
    /// Add this attribute to a list/array property to connect it to an enum. Instead of indexes in
    /// the array, you will see the values of the enum. Make sure that in the initializer it has at
    /// least one element otherwise this code is not executed.
    /// </summary>
    public class EnumAsIndexAttribute : PropertyAttribute
    {
        private Type enumType;

        public EnumAsIndexAttribute(Type enumType)
        {
            if (!enumType.IsEnum)
            {
                throw new Exception("Expected an enum type instead of " + enumType.Name);
            }
            this.enumType = enumType;
        }

        public Type EnumType => enumType;
    }
}