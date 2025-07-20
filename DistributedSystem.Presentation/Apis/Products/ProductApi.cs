using Carter;
using DistributedSystem.Presentation.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using CommandV1 = DistributedSystem.Constract.Services.V1.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributedSystem.Constract.Abstraction.Shared;
using DistributedSystem.Constract.Enumerations;
using DistributedSystem.Constract.Extensions;

namespace DistributedSystem.Presentation.Apis.Products
{
    public class ProductApi : ApiEndpoint, ICarterModule
    {
        private const string BaseUrl = "/api/v{version:apiVersion}/products";
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group1 = app.NewVersionedApi("products")
                .MapGroup(BaseUrl)
                .HasApiVersion(1)
                .RequireAuthorization();

            group1.MapPost(string.Empty, CreateProductsV1);
            group1.MapGet(string.Empty, GetProductsV1);
            group1.MapGet("{productId}", GetProductByIdV1);
            group1.MapDelete("{productId}", DeleteProductV1);
            group1.MapPut("{productId}", UpdateProductsV1);


            //var group2 = app.NewVersionedApi("products")
            //   .MapGroup(BaseUrl).HasApiVersion(2);

            //group2.MapPost(string.Empty, () => "");
            //group2.MapGet(string.Empty, () => "");
            //group2.MapGet("{productId}", () => "");
            //group2.MapDelete("{productId}", () => "");
            //group2.MapPut("{productId}", () => "");
        }

        public static async Task<IResult> CreateProductsV1(ISender sender, [FromBody] CommandV1.Command.CreateProductCommand createProductCommand)
        {
            var result = await sender.Send(createProductCommand);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }

        public static async Task<IResult> GetProductsV1(ISender sender, 
            string? searchTerm = null,
            string? sortColumn = null, 
            string? sortOrder = null, 
            string sortColumnAndOrder = null,
            int pageIndex = 1,
            int pageSize = 10)
        {

            var result = await sender.Send(new CommandV1.Query.GetProductsQuery(searchTerm, sortColumn, SortOrderExtension.ConvertStringToSortOrder(sortOrder)
                , SortOrderExtension.ConvertStringToSortOrderV2(sortColumnAndOrder),
                pageIndex,
                pageSize));

            return Results.Ok(result);
        }

        public static async Task<IResult> GetProductByIdV1(ISender sender, Guid productId)
        {
            var result = await sender.Send(new CommandV1.Query.GetProductByIdQuery(productId));

            return Results.Ok(result);
        }

        public static async Task<IResult> DeleteProductV1(ISender sender, Guid productId)
        {
            var result = await sender.Send(new CommandV1.Command.DeleteProductCommand(productId));
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }


        public static async Task<IResult> UpdateProductsV1(ISender sender, [FromRoute] Guid productId, [FromBody] CommandV1.Command.UpdateProductCommand updateProductCommand)
        {
            updateProductCommand = new CommandV1.Command.UpdateProductCommand(
                productId,
                updateProductCommand.Name,
                updateProductCommand.Price,
                updateProductCommand.Description
            );
            var result = await sender.Send(updateProductCommand);
            if (result.IsFailure)
                return HandlerFailure(result);

            return Results.Ok(result);
        }
    }
}
