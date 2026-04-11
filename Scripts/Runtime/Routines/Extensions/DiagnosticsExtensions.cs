using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
#if UNITASK_ENABLED
using Cysharp.Threading.Tasks;
#endif

namespace BrunoMikoski.Core.Routines
{
    public static class DiagnosticsExtensions
    {
        static bool displayFilenames = true;

        static readonly Regex typeBeautifyRegex = new Regex("`.+$", RegexOptions.Compiled);

        static readonly Dictionary<Type, string> builtInTypeNames = new Dictionary<Type, string>
        {
            { typeof(void), "void" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(object), "object" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(string), "string" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(ushort), "ushort" },
            { typeof(Task), "Task" },
#if UNITASK_ENABLED
            { typeof(UniTask), "UniTask" },
            { typeof(UniTaskVoid), "UniTaskVoid" }
#endif
        };

        public static string GetFirstFileHyperLink(this StackTrace stackTrace)
        {
            if (stackTrace == null)
                return "";

            StringBuilder sb = new StringBuilder();
            int usableFrames = stackTrace.FrameCount;

            for (int i = 0; i < usableFrames; i++)
            {
                StackFrame sf = stackTrace.GetFrame(i);

                MethodBase mb = sf.GetMethod();

                if (IgnoreLine(mb))
                    continue;

                // file name
                if (displayFilenames && (sf.GetILOffset() != -1))
                {
                    String fileName = null;

                    try
                    {
                        fileName = sf.GetFileName();
                    }
                    catch (NotSupportedException)
                    {
                        displayFilenames = false;
                    }
                    catch (SecurityException)
                    {
                        displayFilenames = false;
                    }

                    if (fileName != null)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", AppendHyperLink(fileName, sf.GetFileLineNumber().ToString()));
                        return sb.ToString();
                    }
                }
                sb.AppendLine();
            }
            return "";
        }
        public static string CleanupAsyncStackTrace(this StackTrace stackTrace, int maxFrames = -1)
        {
            if (stackTrace == null)
                return "";

            StringBuilder sb = new StringBuilder();
            int usableFrames = stackTrace.FrameCount;
            if (maxFrames != -1)
                usableFrames = Mathf.Min(maxFrames, usableFrames);

            for (int i = 0; i < usableFrames; i++)
            {
                StackFrame sf = stackTrace.GetFrame(i);

                MethodBase mb = sf.GetMethod();

                if (IgnoreLine(mb))
                    continue;

                if (IsAsync(mb))
                {
                    sb.Append("async ");
                    TryResolveStateMachineMethod(ref mb, out Type decType);
                }

                // return type
                if (mb is MethodInfo mi)
                {
                    sb.Append(BeautifyType(mi.ReturnType, false));
                    sb.Append(" ");
                }

                // method name
                sb.Append(BeautifyType(mb.DeclaringType, false));
                if (!mb.IsConstructor)
                {
                    sb.Append(".");
                }
                sb.Append(mb.Name);
                if (mb.IsGenericMethod)
                {
                    sb.Append("<");
                    foreach (Type item in mb.GetGenericArguments())
                    {
                        sb.Append(BeautifyType(item, true));
                    }
                    sb.Append(">");
                }

                // parameter
                sb.Append("(");
                sb.Append(string.Join(", ", mb.GetParameters().Select(p => BeautifyType(p.ParameterType, true) + " " + p.Name)));
                sb.Append(")");

                // file name
                if (displayFilenames && (sf.GetILOffset() != -1))
                {
                    String fileName = null;

                    try
                    {
                        fileName = sf.GetFileName();
                    }
                    catch (NotSupportedException)
                    {
                        displayFilenames = false;
                    }
                    catch (SecurityException)
                    {
                        displayFilenames = false;
                    }

                    if (fileName != null)
                    {
                        sb.Append(' ');
                        sb.AppendFormat(CultureInfo.InvariantCulture, "(at {0})", AppendHyperLink(fileName, sf.GetFileLineNumber().ToString()));
                    }
                }

                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static void TypeBeautify(Type type, StringBuilder sb)
        {
            if (type.IsNested)
            {
                // TypeBeautify(type.DeclaringType, sb);
                sb.Append(type.DeclaringType.Name.ToString());
                sb.Append(".");
            }

            if (type.IsGenericType)
            {
                int genericsStart = type.Name.IndexOf("`", StringComparison.Ordinal);
                if (genericsStart != -1)
                {
                    sb.Append(type.Name.Substring(0, genericsStart));
                }
                else
                {
                    sb.Append(type.Name);
                }
                sb.Append("<");
                bool first = true;
                foreach (Type item in type.GetGenericArguments())
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    first = false;
                    TypeBeautify(item, sb);
                }
                sb.Append(">");
            }
            else
            {
                sb.Append(type.Name);
            }
        }


        static bool IsAsync(MethodBase methodInfo)
        {
            Type declareType = methodInfo.DeclaringType;
            return typeof(IAsyncStateMachine).IsAssignableFrom(declareType);
        }

        // code from Ben.Demystifier/EnhancedStackTrace.Frame.cs
        static bool TryResolveStateMachineMethod(ref MethodBase method, out Type declaringType)
        {
            declaringType = method.DeclaringType;

            Type parentType = declaringType.DeclaringType;
            if (parentType == null)
            {
                return false;
            }

            MethodInfo[] methods = parentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (methods == null)
            {
                return false;
            }

            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo candidateMethod = methods[i];
                IEnumerable<StateMachineAttribute> attributes = candidateMethod.GetCustomAttributes<StateMachineAttribute>();
                if (attributes == null)
                {
                    continue;
                }

                foreach (StateMachineAttribute asma in attributes)
                {
                    if (asma.StateMachineType == declaringType)
                    {
                        method = candidateMethod;
                        declaringType = candidateMethod.DeclaringType;
                        // Mark the iterator as changed; so it gets the + annotation of the original method
                        // async statemachines resolve directly to their builder methods so aren't marked as changed
                        return asma is IteratorStateMachineAttribute;
                    }
                }
            }

            return false;
        }

        public static string BeautifyType(Type t, bool shortName)
        {
            if (builtInTypeNames.TryGetValue(t, out string builtin))
            {
                return builtin;
            }
            if (t.IsGenericParameter) return t.Name;
            if (t.IsArray) return BeautifyType(t.GetElementType(), shortName) + "[]";
            if (t.FullName?.StartsWith("System.ValueTuple") ?? false)
            {
                return "(" + string.Join(", ", t.GetGenericArguments().Select(x => BeautifyType(x, true))) + ")";
            }
            if (!t.IsGenericType) return shortName ? t.Name : t.FullName.Replace("Cysharp.Threading.Tasks.Triggers.", "").Replace("Cysharp.Threading.Tasks.Internal.", "").Replace("Cysharp.Threading.Tasks.", "") ?? t.Name;

            string innerFormat = string.Join(", ", t.GetGenericArguments().Select(x => BeautifyType(x, true)));

            string genericType = t.GetGenericTypeDefinition().FullName;
            if (genericType == "System.Threading.Tasks.Task`1")
            {
                genericType = "Task";
            }

            return typeBeautifyRegex.Replace(genericType, "").Replace("Cysharp.Threading.Tasks.Triggers.", "").Replace("Cysharp.Threading.Tasks.Internal.", "").Replace("Cysharp.Threading.Tasks.", "") + "<" + innerFormat + ">";
        }

        static bool IgnoreLine(MethodBase methodInfo)
        {
            string declareType = methodInfo.DeclaringType.FullName;

            if (declareType == "System.Threading.ExecutionContext")
                return true;
            if (declareType.StartsWith("System.Runtime.CompilerServices"))
                return true;
            if (declareType.StartsWith("Cysharp.Threading.Tasks.CompilerServices"))
                return true;
            if (declareType == "System.Threading.Tasks.AwaitTaskContinuation")
                return true;
            if (declareType.StartsWith("System.Threading.Tasks.Task"))
                return true;
            if (declareType.StartsWith("Cysharp.Threading.Tasks.UniTaskCompletionSourceCore"))
                return true;
            if (declareType.StartsWith("Cysharp.Threading.Tasks.AwaiterActions"))
                return true;
            if (declareType.StartsWith("BrunoMikoski.Core.Routines"))
                return true;
            if (declareType.StartsWith("Cysharp.Threading.Tasks.UniTask+"))
                return true;
            if (declareType.StartsWith("Cysharp.Threading.Tasks.Internal.PlayerLoopRunner"))
                return true;

            return false;
        }

        static string AppendHyperLink(string path, string line)
        {
            FileInfo fi = new FileInfo(path);
            if (fi.Directory == null)
            {
                return fi.Name;
            }

            return "<a href=\"" + fi.FullName + "\" line=\"" + line + "\">" + path + ":" + line + "</a>";
        }
    }
}