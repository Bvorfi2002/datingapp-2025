using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace API.Helpers
{
    public class PaginatedResult<T>
    {
        public PaginationMetaData Metadata { get; set; } = default!;
        public List<T> Items { get; set; } = [];

    };

    public class PaginationMetaData
    {
        public int CurrentPage { get; set; }
        public int TOtalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

    };


    public class PaginationHelper
    {
        public static async Task<PaginatedResult<T>> CreateAsync<T>(IQueryable<T> query, int pageNumber, int pageSize)
        {
            var count = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedResult<T>
            {
                Metadata = new PaginationMetaData
                {
                    CurrentPage = pageNumber,
                    TOtalPages = (int)Math.Ceiling(count / (double)pageSize),
                    PageSize = pageSize,
                    TotalCount = count
                },
                Items = items
            };
        }
        
    }

    
}