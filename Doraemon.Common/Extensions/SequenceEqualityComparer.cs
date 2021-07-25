using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Doraemon.Common.Extensions
{
    // Thanks Scott
    public class SequenceEqualityComparer<T> : IEqualityComparer<IReadOnlyCollection<T>>
    {
        public bool Equals(IReadOnlyCollection<T> x, IReadOnlyCollection<T> y)
        {
            return x.SequenceEqual(y);
        }
        public int GetHashCode(IReadOnlyCollection<T> obj)
        {
            var hashCode = new HashCode();

            foreach (var item in obj)
            {
                hashCode.Add(item);
            }

            return hashCode.ToHashCode();
        }
    }
}