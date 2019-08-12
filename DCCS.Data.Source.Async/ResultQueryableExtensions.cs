using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace DCCS.Data.Source
{
    public static class AsyncResultQueryableExtensions
    {
        public static Task<AsyncResult<T>> ToAsyncResult<T>(this IQueryable<T> source, Params ps)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return AsyncResult<T>.Create(ps, source);
        }


    }
}
