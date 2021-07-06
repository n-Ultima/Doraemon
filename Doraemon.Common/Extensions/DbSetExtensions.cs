using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Doraemon.Common.Extensions
{
    public static class DbSetExtensions
    {
        public static IAsyncEnumerable<TEntity> AsAsyncEnumerable<TEntity>(this DbSet<TEntity> obj)
            where TEntity : class
        {
            return EntityFrameworkQueryableExtensions.AsAsyncEnumerable(obj);
        }

        public static IQueryable<TEntity> Where<TEntity>(this DbSet<TEntity> obj,
            Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return Queryable.Where(obj, predicate);
        }

        public static IQueryable<bool> Select<TEntity>(this DbSet<TEntity> obj,
            Expression<Func<TEntity, bool>> selector) where TEntity : class
        {
            return Queryable.Select(obj, selector);
        }

        public static Task<TEntity> FirstOrDefaultAsync<TEntity>(this DbSet<TEntity> obj,
            Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(obj, predicate);
        }

        public static Task<TEntity> FirstOrDefaultAsync<TEntity>(this DbSet<TEntity> obj) where TEntity : class
        {
            return EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(obj);
        }

        public static IQueryable<T> FilterBy<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate,
            bool criteria)
        {
            return criteria
                ? source.Where(predicate)
                : source;
        }
    }
}