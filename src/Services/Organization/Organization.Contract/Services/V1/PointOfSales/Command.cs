using BuildingBlocks.Contracts.Message;

namespace Organization.Contract.Services.V1.PointOfSales
{
    public static class Command
    {
        public sealed record Data(
            string Code,
            string Name,
            string? Description,
            bool Status = true);

        public sealed record CreatePointOfSale(Guid BranchId, Data Data) : ICommand<Response.Detail>;
        public sealed record UpdatePointOfSale(Guid Id, Data Data) : ICommand<Response.Detail>;
        public sealed record SetPointOfSaleStatus(Guid Id, bool Status) : ICommand<Response.Detail>;
    }
}
