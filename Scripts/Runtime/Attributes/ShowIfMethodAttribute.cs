using System;
using UnityEditor;
using UnityEngine;

namespace BrunoMikoski.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ShowIfMethodAttribute : PropertyAttribute
    {
        public string MethodName;
        public bool ExpectedValue;

        public ShowIfMethodAttribute(string methodName, bool expectedValue)
        {
            MethodName = methodName;
            ExpectedValue = expectedValue;
        }
    }

    public class ShowIfMethodTrueAttribute : ShowIfMethodAttribute
    {
        public ShowIfMethodTrueAttribute(string methodName) : base(methodName, true)
        {
        }
    }

    public class ShowIfMethodFalseAttribute : ShowIfMethodAttribute
    {
        public ShowIfMethodFalseAttribute(string methodName) : base(methodName, false)
        {
        }
    }
}