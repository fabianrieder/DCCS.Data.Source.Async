using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DCCS.Data.Source
{

    public static class AsyncResultExtensions
    {
        public static async Task<AsyncResult<TProjection>> Select<T, TProjection>(this Task<AsyncResult<T>> source, Func<T, TProjection> selector)
        {
            var intermediateResult = await source;
            return await intermediateResult.SelectAsync(selector);
        }
    }

    public static class AsyncResult
    {
        public static async Task<AsyncResult<T>> Create<T>(Params ps, IQueryable<T> data)
        {
            return await AsyncResult<T>.Create(ps, data);
        }

        public static async Task<AsyncResult<T>> CreateUnmodified<T>(Params ps, IQueryable<T> data)
        {
            return new AsyncResult<T>(ps, data);
        }
    }

    public class AsyncResult<T> : Params
    {
        private IQueryable<T> _data;

        public static async Task<AsyncResult<T>> Create(Params ps, IQueryable<T> data)
        {
            var result = new AsyncResult<T>(ps);
            await result.SetData(data);
            return result;
        }

        private AsyncResult()
        {
            
        }

        internal AsyncResult(Params ps) : base(ps)
        {

        }

        internal AsyncResult(Params ps, IEnumerable<T> data) : base(ps)
        {
            Data = data;
        }


        public IEnumerable<T> Data { get; protected set; }
        public int Total { get; set; }

        public virtual async Task SetData(IQueryable<T> data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));

            await SetCount(_data);
            await DoSort(_data);
            await DoPage(_data);

            Data = _data is IAsyncEnumerable<T> ? await _data.ToArrayAsync() : _data.ToArray();
        }

        protected async Task SetCount(IQueryable<T> data)
        {
            Total = data is IAsyncEnumerable<T> ? await data.CountAsync() : data.Count();
            if (Count.HasValue)
            {
                Count = Math.Min(Count.Value, Total);
            }
        }

        public AsyncResult<TProjection> Select<TProjection>(Func<T, TProjection> selector)
        {
            var result = new AsyncResult<TProjection>(new Params
            {
                Count = Count,
                OrderBy = OrderBy,
                Desc = Desc,
                Page = Page
            })
            {
                Data = Data.Select(selector),
            };

            result.SetCount(result.Data.AsQueryable()).GetAwaiter().GetResult();

            return result;
        }

        public async Task<AsyncResult<TProjection>> SelectAsync<TProjection>(Func<T, TProjection> selector)
        {
            var result = new AsyncResult<TProjection>(new Params
            {
                Count = Count,
                OrderBy = OrderBy,
                Desc = Desc,
                Page = Page
            })
            {
                Data = Data.Select(selector),
            };

            await result.SetCount(result.Data.AsQueryable());

            return result;
        }


        protected async Task DoSort(IQueryable<T> data)
        {
            // Sortieren...
            if (!string.IsNullOrWhiteSpace(OrderBy))
            {
                _data = data.OrderBy($"{OrderBy} {(Desc ? "desc" : "")}");
                return;
            }

            if (data.Expression.Type != typeof(IOrderedQueryable<T>))
            {
                // Vor einem Skip (siehe unten) muß ein OrderBy aufgerufen werden.
                // Ist keine Sortierung angegeben, müssen wir dennoch sortieren und behalten
                // dabei die Reihenfolge bei.
                _data = data.OrderBy(x => true);
                return;
            }

            _data = data;
        }

        protected async Task DoPage(IQueryable<T> data)
        {
            if (!Page.HasValue)
            {
                _data = new List<T>().AsQueryable();
                return;
            }
            if (!Count.HasValue) throw new ArgumentNullException(nameof(Count), "Page size is required.");


            var skip = (Page.Value - 1) * Count.Value;
            var take = Count.Value;

            var intermediateResult = !string.IsNullOrWhiteSpace(OrderBy)
                ? data.Skip(skip).Take(take)
                : data.Skip(skip).OrderBy(x => true).Take(take).OrderBy(x => true);

            var hasEntries = intermediateResult is IAsyncEnumerable<T>
                ? await intermediateResult.AnyAsync()
                : intermediateResult.Any();

            if (!hasEntries)
            {
                Page = 1;
                intermediateResult = data.Take(Count.Value);
            }

            _data = intermediateResult;

        }
    }
}
