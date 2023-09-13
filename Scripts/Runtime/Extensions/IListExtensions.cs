using System.Collections.Generic;


namespace System.Linq
{
    /// <summary>
    /// These extension methods are in the same convention as System.Linq and can be viewed as an
    /// addition to those methods, so they are placed in the System.Linq namespace
    /// </summary>
    public static class IListExtensions
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

        /// <summary>
        /// Returns the minimal element of the given sequence, based on
        /// the given projection.
        /// </summary>
        /// <remarks>
        /// If more than one element has the minimal projected value, the first
        /// one encountered will be returned. This overload uses the default comparer
        /// for the projected type. This operator uses immediate execution, but
        /// only buffers a single result (the current minimal element).
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="selector">Selector to use to pick the results to compare</param>
        /// <returns>The minimal element, according to the projection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is null</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty</exception>

        public static TSource MinBy<TSource, TKey>(this IList<TSource> source,
            Func<TSource, TKey> selector)
        {
            return source.MinBy(selector, null);
        }

        /// <summary>
        /// Returns the minimal element of the given sequence, based on
        /// the given projection and the specified comparer for projected values.
        /// </summary>
        /// <remarks>
        /// If more than one element has the minimal projected value, the first
        /// one encountered will be returned. This operator uses immediate execution, but
        /// only buffers a single result (the current minimal element).
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="selector">Selector to use to pick the results to compare</param>
        /// <param name="comparer">Comparer to use to compare projected values</param>
        /// <returns>The minimal element, according to the projection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="selector"/>
        /// or <paramref name="comparer"/> is null</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty</exception>

        public static TSource MinBy<TSource, TKey>(this IList<TSource> source,
            Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (source.Count == 0) throw new InvalidOperationException("Sequence contains no elements");

            comparer = comparer ?? Comparer<TKey>.Default;

            TSource min = source[0];
            TKey minKey = selector(min);

            for (int i = 1; i < source.Count; i++)
            {
                TSource candidate = source[i];
                TKey candidateProjected = selector(candidate);
                if (comparer.Compare(candidateProjected, minKey) < 0)
                {
                    min = candidate;
                    minKey = candidateProjected;
                }
            }
            return min;
        }

        /// <summary>
        /// Returns the maximal element of the given sequence, based on
        /// the given projection.
        /// </summary>
        /// <remarks>
        /// If more than one element has the maximal projected value, the first
        /// one encountered will be returned. This overload uses the default comparer
        /// for the projected type. This operator uses immediate execution, but
        /// only buffers a single result (the current maximal element).
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="selector">Selector to use to pick the results to compare</param>
        /// <returns>The maximal element, according to the projection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="selector"/> is null</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty</exception>

        public static TSource MaxBy<TSource, TKey>(this IList<TSource> source,
            Func<TSource, TKey> selector)
        {
            return source.MaxBy(selector, null);
        }

        /// <summary>
        /// Returns the maximal element of the given sequence, based on
        /// the given projection and the specified comparer for projected values.
        /// </summary>
        /// <remarks>
        /// If more than one element has the maximal projected value, the first
        /// one encountered will be returned. This operator uses immediate execution, but
        /// only buffers a single result (the current maximal element).
        /// </remarks>
        /// <typeparam name="TSource">Type of the source sequence</typeparam>
        /// <typeparam name="TKey">Type of the projected element</typeparam>
        /// <param name="source">Source sequence</param>
        /// <param name="selector">Selector to use to pick the results to compare</param>
        /// <param name="comparer">Comparer to use to compare projected values</param>
        /// <returns>The maximal element, according to the projection.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/>, <paramref name="selector"/>
        /// or <paramref name="comparer"/> is null</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> is empty</exception>
        public static TSource MaxBy<TSource, TKey>(this IList<TSource> source,
            Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (source.Count == 0) throw new InvalidOperationException("Sequence contains no elements");

            comparer = comparer ?? Comparer<TKey>.Default;

            TSource max = source[0];
            TKey maxKey = selector(max);

            for (int i = 1; i < source.Count; i++)
            {
                TSource candidate = source[i];
                TKey candidateProjected = selector(candidate);
                if (comparer.Compare(candidateProjected, maxKey) > 0)
                {
                    max = candidate;
                    maxKey = candidateProjected;
                }
            }

            return max;
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
    }
}
