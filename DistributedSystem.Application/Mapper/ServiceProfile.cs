using AutoMapper;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Services.V1.Product;
using DistributedSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Application.Mapper
{
    public class ServiceProfile : Profile
    {

        public ServiceProfile()
        {
            #region V1
            CreateMap<Product, Response.ProductResponse>();
            CreateMap<PageResult<Product>, PageResult<Response.ProductResponse>>();
            #endregion
        }
    }
}
