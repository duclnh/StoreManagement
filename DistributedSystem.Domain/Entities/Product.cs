using DistributedSystem.Domain.Abstractions.Aggregates;
using DistributedSystem.Domain.Abstractions.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Domain.Entities
{
    public class Product : AggregateRoot<Guid>, IAuditTableEntity
    {
        public string Name { get; private set; }
        public decimal Price { get; private set; }
        public string Description { get; private set; }
        public DateTimeOffset CreatedOnUtc { get; set; }
        public DateTimeOffset? UpdatedOnUtc { get; set; }

        public Product(Guid id, string name, decimal price, string description)
        {
            Id = id;
            Name = name;
            Price = price;
            Description = description;
        }
        public static Product CreateProduct(Guid id, string name, decimal price, string description)
        {
            var product = new Product(id, name, price, description);

            product.RaiseDomainEvent(new Constract.Services.V1.Product.DomainEvent.ProductCreated(Guid.NewGuid(), product.Id, product.Name, product.Price, product.Description));

            return product;
        }

        public void UpdateProduct(string name, decimal price, string description)
        {
            Name = name;
            Price = price;
            Description = description;

            RaiseDomainEvent(new Constract.Services.V1.Product.DomainEvent.ProductUpdated(Guid.NewGuid(), Id, name, price, description));
        }

        public void DeleteProduct()
           => RaiseDomainEvent(new Constract.Services.V1.Product.DomainEvent.ProductDeleted(Guid.NewGuid(), Id));

    }
}
