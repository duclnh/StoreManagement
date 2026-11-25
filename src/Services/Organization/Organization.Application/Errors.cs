using BuildingBlocks.Contracts.Shared;

namespace Organization.Application
{
    internal static class Errors
    {
        public static readonly Error Forbidden = new("Organization.Forbidden", "Access to this scope is denied.");
        public static readonly Error NotFound = new("Organization.NotFound", "The resource does not exist in this scope.");
        public static readonly Error Conflict = new("Organization.Conflict", "Code or membership already exists in this scope.");
    }
}
