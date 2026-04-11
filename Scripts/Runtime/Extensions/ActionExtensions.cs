namespace System
{
    public static partial class ActionExtensions
    {
        public static void Dispatch<T1, T2>(this Action<T1, T2> action, T1 value1, T2 value2)
        {
            if (action != null)
                action(value1, value2);
        }

        public static void Dispatch<T>(this Action<T> action, T value)
        {
            if (action != null)
                action(value);
        }

        public static void Dispatch(this Action action)
        {
            if (action != null)
                action();
        }
    }
}
