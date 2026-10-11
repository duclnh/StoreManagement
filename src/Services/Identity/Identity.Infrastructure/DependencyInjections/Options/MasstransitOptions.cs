using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Infrastructure.DependencyInjections.Options
{
    public class MassTransitOptions
    {
        public string Host { get; set; } = string.Empty;
        public string VHost { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

