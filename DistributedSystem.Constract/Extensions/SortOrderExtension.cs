using DistributedSystem.Constract.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Constract.Extensions
{
    public static class SortOrderExtension
    {
        public static SortOrder ConvertStringToSortOrder(string? sortOrder)
            => !string.IsNullOrEmpty(sortOrder) ? sortOrder.Trim().ToUpper().Equals("ASC") ? SortOrder.Ascending : SortOrder.Descending : SortOrder.Descending;

        public static Dictionary<string, SortOrder> ConvertStringToSortOrderV2(string? sortOrder)
        {
            var result = new Dictionary<string, SortOrder>();

            if (!string.IsNullOrEmpty(sortOrder))
            {
                if (sortOrder.Trim().Split(",").Length > 0)
                {
                    foreach (var item in sortOrder.Trim().Split(","))
                    {
                        if (!item.Contains("-"))
                            throw new FormatException("Sort condition should be follow by format:  Column1-ASC,Column2-DESC...");

                        var property = sortOrder.Trim().Split("-");

                        string key = ProductExtension.GetSortProductProperty(property[0]);
                        string value = ConvertStringToSortOrder(property[1]);

                        result.TryAdd(key, value);
                    }
                }
                else
                {
                    if (!sortOrder.Contains("-"))
                        throw new FormatException("Sort condition should be follow by format: Column1-ASC,Column2-DESC...");

                    var property = sortOrder.Trim().Split("-");
                    result.Add(property[0], ConvertStringToSortOrder(property[1]));
                }
            }

            return result;
        }
    }
}
