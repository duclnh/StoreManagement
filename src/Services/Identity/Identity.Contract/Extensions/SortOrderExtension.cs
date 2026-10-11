using Identity.Contract.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Contract.Extensions
{
    public static class SortOrderExtension
    {
        public static SortOrder ConvertStringToSortOrder(string? sortOrder)
            => !string.IsNullOrEmpty(sortOrder)
                ? sortOrder.Trim().ToUpper().Equals("ASC")
                    ? SortOrder.Ascending
                    : SortOrder.Descending
                : SortOrder.Descending;

        public static Dictionary<string, SortOrder> ConvertStringToSortOrderV2(string? sortOrder)
        {
            var result = new Dictionary<string, SortOrder>();

            if (!string.IsNullOrEmpty(sortOrder))
            {
                foreach (var item in sortOrder.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!item.Contains('-'))
                        throw new FormatException("Sort condition should follow the format: Column1-ASC,Column2-DESC...");

                    var parts = item.Trim().Split('-', 2, StringSplitOptions.TrimEntries);
                    var key = parts[0];
                    var value = ConvertStringToSortOrder(parts[1]);

                    result.TryAdd(key, value);
                }
            }

            return result;
        }
    }
}

