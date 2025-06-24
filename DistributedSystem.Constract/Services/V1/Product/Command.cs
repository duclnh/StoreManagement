using DistributedSystem.Constract.Abstraction.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DistributedSystem.Constract.Services.V1.Product
{
    public static class Command
    {
        public record CreateProductCommand(string Name, decimal Price, string Description) : ICommand;
        public record UpdateProductCommand(Guid Id, string Name, decimal Price, string Description) : ICommand;
        public record DeleteProductCommand(Guid Id) : ICommand;
    }
}
