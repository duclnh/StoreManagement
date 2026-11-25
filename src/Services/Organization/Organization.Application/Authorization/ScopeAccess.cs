using Organization.Application.Abstractions;
using Organization.Domain.Abstractions;
using Organization.Domain.Repositories;
using TenantEntity = global::Organization.Domain.Entities.Tenants.Tenant;
using OrganizationEntity = global::Organization.Domain.Entities.Organizations.Organization;
using BranchEntity = global::Organization.Domain.Entities.Branches.Branch;
using WarehouseEntity = global::Organization.Domain.Entities.Warehouses.Warehouse;
using PointOfSaleEntity = global::Organization.Domain.Entities.PointOfSales.PointOfSale;

namespace Organization.Application.Authorization
{
    public sealed class ScopeAccess : IScopeAccess
    {
        private readonly ITenantContext _context;
        private readonly IOrganizationContext _organizationContext;
        private readonly IRepository<TenantEntity> _tenants;
        private readonly IRepository<OrganizationEntity> _organizations;
        private readonly IRepository<BranchEntity> _branches;
        private readonly IRepository<WarehouseEntity> _warehouses;
        private readonly IRepository<PointOfSaleEntity> _pointOfSales;
        private readonly IMemberRepository _members;

        public ScopeAccess(ITenantContext context, IOrganizationContext organizationContext,
            IRepository<TenantEntity> tenants, IRepository<OrganizationEntity> organizations,
            IRepository<BranchEntity> branches, IRepository<WarehouseEntity> warehouses,
            IRepository<PointOfSaleEntity> pointOfSales, IMemberRepository members)
        {
            _context = context;
            _organizationContext = organizationContext;
            _tenants = tenants;
            _organizations = organizations;
            _branches = branches;
            _warehouses = warehouses;
            _pointOfSales = pointOfSales;
            _members = members;
        }

        public async Task<bool> CanCreateAsync(string resource, Guid? parentId, CancellationToken cancellationToken)
        {
            if (_context.IsSystemAdministrator)
            {
                return true;
            }

            if (resource == "Tenant" || parentId is not Guid parent)
            {
                return false;
            }

            if (resource == "Organization")
            {
                return _context.IsTenantAdministrator && _context.TenantId == parent;
            }

            if (resource == "Branch")
            {
                return _context.IsTenantAdministrator && await CanAccessAsync("Organization", parent, true, cancellationToken);
            }

            return await CanAccessAsync("Branch", parent, true, cancellationToken);
        }

        public async Task<bool> CanListAsync(string resource, Guid? parentId, CancellationToken cancellationToken)
        {
            if (_context.IsSystemAdministrator)
            {
                return true;
            }

            if (_context.TenantId is null || _context.UserId is null)
            {
                return false;
            }

            return resource switch
            {
                "Tenant" => _context.IsTenantAdministrator,
                "Organization" => parentId is null || parentId == _context.TenantId,
                "Branch" => parentId is Guid organization && await CanAccessAsync("Organization", organization, false, cancellationToken),
                _ => parentId is Guid branch && await CanAccessAsync("Branch", branch, false, cancellationToken)
            };
        }

        public async Task<bool> CanAccessAsync(string resource, Guid id, bool write, CancellationToken cancellationToken)
        {
            if (_context.IsSystemAdministrator)
            {
                return true;
            }

            if (_context.TenantId is not Guid tenantId || _context.UserId is not Guid userId)
            {
                return false;
            }

            if (resource == "Tenant")
            {
                return id == tenantId && _context.IsTenantAdministrator;
            }

            Guid organizationId;
            Guid? branchId = null;
            if (resource == "Organization")
            {
                var organization = await _organizations.GetAsync(id, cancellationToken);
                if (organization is null)
                {
                    return false;
                }

                organizationId = organization.Id;
                if (write && !_context.IsTenantAdministrator)
                {
                    return false;
                }
            }
            else
            {
                branchId = id;
                if (resource == "Warehouse")
                {
                    branchId = (await _warehouses.GetAsync(id, cancellationToken))?.BranchId;
                }
                else if (resource == "PointOfSale")
                {
                    branchId = (await _pointOfSales.GetAsync(id, cancellationToken))?.BranchId;
                }

                if (branchId is not Guid branch)
                {
                    return false;
                }

                var branchEntity = await _branches.GetAsync(branch, cancellationToken);
                if (branchEntity is null)
                {
                    return false;
                }

                organizationId = branchEntity.OrganizationId;
            }

            if (_context.IsTenantAdministrator)
            {
                return true;
            }

            var parentOrganization = await _organizations.GetAsync(organizationId, cancellationToken);
            if (parentOrganization is null || !parentOrganization.Status)
            {
                return false;
            }

            // The scoped organization repository already limits this metadata to assigned branches.
            if (resource == "Organization" && !write)
            {
                return true;
            }

            if (write && !_context.IsBranchManager && !_context.HasPermission("Organization." + resource + ".Manage"))
            {
                return false;
            }

            if (_organizationContext.OrganizationId is Guid selectedOrganization && selectedOrganization != organizationId
                || branchId is Guid selectedBranch && _organizationContext.BranchId is Guid contextBranch && selectedBranch != contextBranch)
            {
                return false;
            }

            return await _members.HasAccessAsync(tenantId, organizationId, branchId, userId, cancellationToken);
        }
    }
}
