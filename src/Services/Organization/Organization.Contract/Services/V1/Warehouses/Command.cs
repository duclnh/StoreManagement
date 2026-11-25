using BuildingBlocks.Contracts.Message;

namespace Organization.Contract.Services.V1.Warehouses
{
    public static class Command
    {
        public sealed record Data(
            string Code,
            string Name,
            string WarehouseType,
            string? Address,
            bool IsDefault,
            bool Status = true);

        public sealed record CreateWarehouse(Guid BranchId, Data Data) : ICommand<Response.Detail>;
        public sealed record UpdateWarehouse(Guid Id, Data Data) : ICommand<Response.Detail>;
        public sealed record SetWarehouseStatus(Guid Id, bool Status) : ICommand<Response.Detail>;
    }
}
