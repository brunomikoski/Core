using UnityEngine;


namespace System.Collections.Generic
{
    public static partial class IListExtensions
    {
        private static readonly Random RANDOM = new Random();

        public static T Random<T>(this IList<T> list)
        {
            if (list.Count == 0)
                throw new IndexOutOfRangeException("Cannot take random element from an empty list");
            return list[RANDOM.Next(list.Count)];
        }

        public static T GetRandomButNotThis<T>(this IList<T> list, T item)
        {
            if (list.Count == 0)
                throw new IndexOutOfRangeException("Cannot take random element from an empty list");

            int index = list.IndexOf(item);
            if (index == -1)
                return list.Random();

            if (list.Count == 1)
                return list[0];

            int newIndex = RANDOM.Next(list.Count - 1);
            if (newIndex >= index)
                newIndex++;

            return list[newIndex];
        }

        public static T NextOrFirst<T>(this IList<T> list, T current)
        {
            int index = list.IndexOf(current);
            if (index == -1)
                throw new IndexOutOfRangeException("Cannot find the object \"" + current + "\" inside list: " + list);
            return list.NextOrFirst(index);
        }

        public static T PreviousOrLast<T>(this IList<T> list, T current)
        {
            int index = list.IndexOf(current);
            if (index == -1)
                throw new IndexOutOfRangeException("Cannot find the object \"" + current + "\" inside list: " + list);
            return list.PreviousOrLast(index);
        }

        public static T NextOrFirst<T>(this IList<T> list, int index)
        {
            int listCount = list.Count;
            if (index < 0 || index > (listCount - 1))
                throw new IndexOutOfRangeException("Index is outside list range: " + list);
            return list[(index + 1) % listCount];
        }

        public static T PreviousOrLast<T>(this IList<T> list, int index)
        {
            int listCount = list.Count;
            if (index < 0 || index > (listCount - 1))
                throw new IndexOutOfRangeException("Index is outside list range: " + list);
            return list[(index + listCount - 1) % listCount];
        }

        public static T FirstOrDefault<T>(this IList<T> list)
        {
            return list.Count == 0 ? default(T) : list[0];
        }

        public static T First<T>(this IList<T> list)
        {
            return list[0];
        }

        public static T LastOrDefault<T>(this IList<T> list)
        {
            return list.Count == 0 ? default(T) : list[^1];
        }

        public static T Last<T>(this IList<T> list)
        {
            return list[^1];
        }

        public static void Shuffle<T>(this IList<T> list, Random random = null)
        {
            if (random == null)
                random = RANDOM;
            
            int i = list.Count;
            while (i > 1)
            {
                i--;
                int k = random.Next(i + 1);
                (list[k], list[i]) = (list[i], list[k]);
            }
        }
        
        public static T IndexOrLast<T>(this IList<T> list, int index)
        {
            index = Mathf.Clamp(index, 0, list.Count - 1);
            return list[index];
        }
    }
}
