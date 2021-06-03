using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doraemon.Common.Extensions
{
    public static class DbSetExtensions
    {
        public static IAsyncEnumerable<TEntity> AsAsyncEnumerable<TEntity>(this Microsoft.EntityFrameworkCore.DbSet<TEntity> obj) where TEntity : class
        {
            return Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsAsyncEnumerable(obj);
        }

        public static IQueryable<TEntity> Where<TEntity>(this Microsoft.EntityFrameworkCore.DbSet<TEntity> obj, System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return System.Linq.Queryable.Where(obj, predicate);
        }

        public static IQueryable<bool> Select<TEntity>(this Microsoft.EntityFrameworkCore.DbSet<TEntity> obj, System.Linq.Expressions.Expression<Func<TEntity, bool>> selector) where TEntity : class
        {
            return System.Linq.Queryable.Select(obj, selector);
        }

        public static Task<TEntity> FirstOrDefaultAsync<TEntity>(this Microsoft.EntityFrameworkCore.DbSet<TEntity> obj, System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate) where TEntity : class
        {
            return Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(obj, predicate);
        }

        public static Task<TEntity> FirstOrDefaultAsync<TEntity>(this Microsoft.EntityFrameworkCore.DbSet<TEntity> obj) where TEntity : class
        {
            return Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(obj);
        }
    }
}
