-- Run in SQLCMD mode with -v IdentityDatabase=... OrganizationDatabase=...
-- Stop Identity's legacy scope writes before running. No source rows are modified.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM [$(OrganizationDatabase)].[organization].[Tenants])
    THROW 51000, 'Target tenant scope must be empty. Review existing data instead of merging implicitly.', 1;

INSERT INTO [$(OrganizationDatabase)].[organization].[Tenants]
    (Id, TenantId, Code, Name, Status, ContactEmail, ContactPhone, ConfigurationJson, CreatedAt, UpdatedAt)
SELECT Id, Id, UPPER(LTRIM(RTRIM(Code))), Name, IsActive, ContactEmail, ContactPhone,
    ConfigurationJson, CreatedAtUtc, NULL
FROM [$(IdentityDatabase)].dbo.Tenants;

INSERT INTO [$(OrganizationDatabase)].[organization].[Organizations]
    (Id, TenantId, Code, Name, Status, BusinessType, Currency, TimeZone, Language, CreatedAt, UpdatedAt)
SELECT Id, TenantId, 'ORG-' + LEFT(REPLACE(CONVERT(varchar(36), Id), '-', ''), 28), Name, IsActive,
    'GENERAL_RETAIL', 'VND', 'Asia/Ho_Chi_Minh', 'vi', SYSUTCDATETIME(), NULL
FROM [$(IdentityDatabase)].dbo.Organizations;

INSERT INTO [$(OrganizationDatabase)].[organization].[Branches]
    (Id, TenantId, OrganizationId, Code, Name, Status, IsDefault, CreatedAt, UpdatedAt)
SELECT b.Id, o.TenantId, b.OrganizationId,
    'BR-' + LEFT(REPLACE(CONVERT(varchar(36), b.Id), '-', ''), 29), b.Name, b.IsActive, 0, SYSUTCDATETIME(), NULL
FROM [$(IdentityDatabase)].dbo.Branches b
JOIN [$(IdentityDatabase)].dbo.Organizations o ON o.Id = b.OrganizationId;

INSERT INTO [$(OrganizationDatabase)].[organization].[OrganizationMembers]
    (Id, TenantId, OrganizationId, UserId, BranchId, IsDefault, Status, CreatedAt, UpdatedAt)
SELECT NEWID(), assignments.TenantId, assignments.OrganizationId, assignments.UserId,
    assignments.BranchId, 0, 1, SYSUTCDATETIME(), NULL
FROM (
    SELECT DISTINCT o.TenantId, o.Id OrganizationId, a.UserId, CAST(NULL AS uniqueidentifier) BranchId
    FROM [$(IdentityDatabase)].dbo.UserRoleAssignments a
    JOIN [$(IdentityDatabase)].dbo.Organizations o ON o.Id = a.ScopeId
    JOIN [$(IdentityDatabase)].dbo.AppUsers u ON u.Id = a.UserId AND u.TenantId = o.TenantId
    WHERE a.ScopeType = 2
    UNION
    SELECT DISTINCT o.TenantId, b.OrganizationId, a.UserId, b.Id
    FROM [$(IdentityDatabase)].dbo.UserRoleAssignments a
    JOIN [$(IdentityDatabase)].dbo.Branches b ON b.Id = a.ScopeId
    JOIN [$(IdentityDatabase)].dbo.Organizations o ON o.Id = b.OrganizationId
    JOIN [$(IdentityDatabase)].dbo.AppUsers u ON u.Id = a.UserId AND u.TenantId = o.TenantId
    WHERE a.ScopeType = 3
) assignments;

IF (SELECT COUNT(*) FROM [$(OrganizationDatabase)].[organization].[Tenants])
    <> (SELECT COUNT(*) FROM [$(IdentityDatabase)].dbo.Tenants)
    THROW 51001, 'Tenant counts do not match.', 1;
IF (SELECT COUNT(*) FROM [$(OrganizationDatabase)].[organization].[Organizations])
    <> (SELECT COUNT(*) FROM [$(IdentityDatabase)].dbo.Organizations)
    THROW 51002, 'Organization counts do not match.', 1;
IF (SELECT COUNT(*) FROM [$(OrganizationDatabase)].[organization].[Branches])
    <> (SELECT COUNT(*) FROM [$(IdentityDatabase)].dbo.Branches)
    THROW 51003, 'Branch counts do not match.', 1;

COMMIT;
