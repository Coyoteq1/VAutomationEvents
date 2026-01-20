using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Stunlock.Core;
using LiteDB;

namespace VAuto.Services.Systems
{
    /// <summary>
    /// Extension methods for various types including ILiteCollection support
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets a hash from a string GUID
        /// </summary>
        public static int GuidHash(this string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return 0;
                
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(guid));
                return BitConverter.ToInt32(hash, 0);
            }
        }

        /// <summary>
        /// Gets a hash from a PrefabGUID
        /// </summary>
        public static int GuidHash(this PrefabGUID guid)
        {
            return guid.GuidHash.ToString().GuidHash();
        }

        #region ILiteCollection Extensions

        /// <summary>
        /// Groups elements by a key selector for ILiteCollection
        /// </summary>
        public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
            this ILiteCollection<TSource> source, 
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));

            return source.Select(item => new { Key = keySelector(item), Element = elementSelector(item) })
                      .GroupBy(x => x.Key, x => x.Element);
        }

        /// <summary>
        /// Groups elements by a key selector for ILiteCollection (simplified)
        /// </summary>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
            this ILiteCollection<TSource> source, 
            Func<TSource, TKey> keySelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return source.GroupBy(keySelector, x => x);
        }

        /// <summary>
        /// Sums numeric values in ILiteCollection
        /// </summary>
        public static int Sum<TSource>(this ILiteCollection<TSource> source, Func<TSource, int> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            int sum = 0;
            foreach (var item in source)
            {
                sum += selector(item);
            }
            return sum;
        }

        /// <summary>
        /// Projects elements into a new form for ILiteCollection
        /// </summary>
        public static IEnumerable<TResult> Select<TSource, TResult>(
            this ILiteCollection<TSource> source, 
            Func<TSource, TResult> selector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (selector == null) throw new ArgumentNullException(nameof(selector));

            foreach (var item in source)
            {
                yield return selector(item);
            }
        }

        /// <summary>
        /// Projects elements and flattens for ILiteCollection
        /// </summary>
        public static IEnumerable<TResult> SelectMany<TSource, TCollection, TResult>(
            this ILiteCollection<TSource> source,
            Func<TSource, IEnumerable<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (collectionSelector == null) throw new ArgumentNullException(nameof(collectionSelector));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

            foreach (var item in source)
            {
                foreach (var subItem in collectionSelector(item))
                {
                    yield return resultSelector(item, subItem);
                }
            }
        }

        /// <summary>
        /// Converts ILiteCollection to List for LINQ operations
        /// </summary>
        public static List<TSource> ToList<TSource>(this ILiteCollection<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new List<TSource>();
            foreach (var item in source)
            {
                result.Add(item);
            }
            return result;
        }

        #endregion
    }

    /// <summary>
    /// Simple IGrouping implementation for ILiteCollection extensions
    /// </summary>
    public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
    {
        public TKey Key { get; }
        public IEnumerable<TElement> Group { get; }

        public Grouping(TKey key, IEnumerable<TElement> group)
        {
            Key = key;
            Group = group;
        }

        public IEnumerator<TElement> GetEnumerator()
        {
            return Group.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Group.GetEnumerator();
        }
    }
}
