using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Contract.Abstraction.Shared
{
    public class PageResult<T>
    {
        public const int UpperPageSize = 100;
        public const int DefaultPageSize = 10;
        public const int DefaultPageIndex = 1;

        private PageResult(List<T> items, int pageIndex, int pageSize, int totalCount)
        {
            Items = items;
            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        public List<T> Items { get; }
        public int PageIndex { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public bool HasNextPage => PageIndex * PageSize < TotalCount;
        public bool HasPreviousPage => PageIndex > 1;

        public static async Task<PageResult<T>> CreateAsync(IQueryable<T> query, int pageIndex, int pageSize)
        {
            pageIndex = pageIndex <= 0 ? DefaultPageIndex : pageIndex;
            pageSize = pageSize <= 0 ? DefaultPageSize : pageSize > UpperPageSize ? UpperPageSize : pageSize;

            var totalCount = await query.CountAsync();
            var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new(items, pageIndex, pageSize, totalCount);
        }

        public static PageResult<T> Create(List<T> items, int pageIndex, int pageSize, int totalCount)
            => new(items, pageIndex, pageSize, totalCount);
    }
}

