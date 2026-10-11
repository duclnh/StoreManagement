using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.DependencyInjections.Extensions
{
    internal static class NameFormatterExtension
    {
        public static string ToKebabCaseString(this MemberInfo memberInfo) 
            => KebabCaseEndpointNameFormatter.Instance.SanitizeName(memberInfo.Name);
    }

    internal class KebabCaseEntityNameFormatter : IEntityNameFormatter
    {
        public string FormatEntityName<T>()
        => typeof(T).ToKebabCaseString();
    }
}

