# Organization ownership và triển khai

Organization sở hữu Tenant, Organization, Branch, Warehouse, PointOfSale và OrganizationMember. Identity tiếp tục sở hữu User, Role, credential, login session và role assignments. TenantId vẫn nằm trong User/Role/JWT. Không có FK hoặc join từ database Organization sang database Identity. ManagerUserId/UserId được kiểm tra qua authenticated Identity directory API.

## Cấu hình

| Service | Biến môi trường cần thiết |
| --- | --- |
| Organization | `ConnectionStrings__Organization`, `Services__Identity__BaseUrl`, `ServiceAuthentication__Key`, `JwtOptions__Issuer`, `JwtOptions__Audience`, `JwtOptions__SecretKey` |
| Identity | Connection string hiện tại, `Services__Organization__BaseUrl`, cùng `ServiceAuthentication__Key`, JWT options hiện tại |

Không commit secret. ServiceAuthentication key tối thiểu 32 ký tự, chỉ dùng cho directory endpoints nội bộ, phải truyền qua HTTPS ngoài loopback và giới hạn ingress nội bộ. Đây là credential giữa services; user authentication vẫn hoàn toàn do JWT/session của Identity. Không có login/token issuer mới trong Organization.

Organization dùng JWT validation parameters chung ở BuildingBlocks.AspNetCore và gọi Identity `/api/v1/auths/me` để kiểm tra revocation. Identity xác thực session, sau đó gọi Organization internal directory để refresh scope. Directory không gọi ngược authentication của service kia nên không tạo vòng lặp HTTP.

## Ownership và read models

Các bảng Tenants/Organizations/Branches cũ trong Identity được giữ làm **local read models cho authorization**, không còn route hoặc implementation quản trị ghi các bảng này. `OrganizationScopeProjection` cập nhật chúng từ directory của owner, dưới transaction và application lock theo tenant. FK nội bộ Identity vẫn trỏ đến read models nội bộ; chúng không phải cross-database FK.

Login, refresh và mỗi authenticated request của Identity kiểm tra owner trước khi dùng scope. Organization không khả dụng thì fail closed; không fallback về bản ghi active cũ. System Administrator được phép thao tác nhiều tenant, kể cả khi tenant gắn token đã bị vô hiệu hóa. User thường không được quyền đó. Request của Administrator có `X-Tenant-Id` được refresh thêm tenant được chọn cho các API quản trị Identity.

Không xóa TenantId khỏi JWT hoặc user/role. Không tiếp tục ghi nghiệp vụ vào bảng Identity scope bằng code SQL hay route cũ. Các mutation `/api/v1/admin/tenants...` trước đây đã bỏ; client cần chuyển sang các routes Organization `/api/v1/tenants`, `/organizations`, `/branches`.

## Quyền và scope

- `Administrator`: System Admin, quản lý mọi tenant. Tạo tenant chỉ dành cho role này.
- `TenantAdmin` hoặc `TenantAdministrator`: quản trị đúng TenantId trong JWT, không nhận tenant tùy ý từ header/body.
- `BranchManager`: chỉ sửa branch/warehouse/POS trong membership được cấp; không sửa thông tin organization, tạo branch hay đổi branch mặc định cấp organization.
- Permission claim `Organization.<Resource>.Manage` có thể cho phép thao tác ghi trên phạm vi membership tương ứng; claim quyền không mở rộng membership.
- User thường chỉ đọc metadata trong membership đang active. Organization-wide membership có BranchId null cho phép các branch thuộc organization đó; membership có BranchId chỉ cho branch đó. Optional organization_id/branch_id claims có tác dụng thu hẹp, không tự cấp quyền.
- Role assignments/permissions vẫn do Identity quản lý. Thêm membership không tự cấp role/quyền Identity; xóa membership lập tức mất quyền Organization cho phạm vi đó. Xóa membership organization xóa cả các liên kết branch của user trong organization đó.

Tenant/list/search dùng paging (`page`, `pageSize` tối đa 100) và `search` trên code/name. Organization creation nhận `tenantId` query để System Admin chọn tenant; mọi trường hợp khác được đối chiếu với context. API status dùng `{ "status": true/false }`. PUT trả DTO cập nhật (200); POST trả 201 với Location. Warehouse/POS có thêm GET detail và PATCH status bên cạnh các routes yêu cầu. Branch members có thêm DELETE để hỗ trợ remove branch assignment.

Code được trim/uppercase: tenant code duy nhất toàn hệ thống, organization code duy nhất trong tenant, branch code trong organization, warehouse/POS code trong branch. Composite FK gồm TenantId ngăn liên kết chéo tenant; membership FK gồm cả OrganizationId và BranchId ngăn gán branch không thuộc organization. Unique filtered index bảo đảm một branch/kho mặc định mỗi parent và một membership mặc định mỗi user/tenant. Thay default thực hiện trong transaction serializable của command.

## Chuyển đổi dữ liệu

Không tự áp migration hoặc xóa dữ liệu live trong task này. Dừng ghi vào các route quản trị scope cũ trước khi export. Sao lưu hai database và giữ các Id nguyên vẹn.

1. Sinh/review SQL và áp InitialOrganization cho database Organization:

```powershell
dotnet ef database update --project src/Services/Organization/Organization.Persistence
```

2. Khi hai database trên cùng SQL Server, dùng `scripts/organization/import-identity-scopes.sql` trong SQLCMD mode, truyền `IdentityDatabase` và `OrganizationDatabase`. Script một chiều, transaction, chỉ chạy trên target scope trống; không thay đổi source. Với server khác nhau, export/import staging tương đương rồi dùng cùng mapping và kiểm tra counts/Ids.
3. Script giữ Id, tên, trạng thái, contact, configuration; tạo code ổn định từ Id cho Organization/Branch vì source chưa có code. Không suy đoán ngành hàng: mặc định GENERAL_RETAIL, VND, Asia/Ho_Chi_Minh, vi. Membership chỉ backfill từ UserRoleAssignments rõ scope organization/branch; assignment tenant-wide không tự chuyển thành membership ở mọi branch.
4. Review counts và các user branch assignments trước cutover. Tenant mặc định bootstrap cũng phải được copy; cài mới phải tạo tenant trong Organization bằng tài khoản Administrator rồi dùng TenantId đó để tạo users qua Identity.
5. Áp migration ScopeReadModels của Identity để bỏ unique constraint theo tên, vì business owner dùng code làm khóa duy nhất:

```powershell
dotnet ef database update --project src/Services/Identity/Identity.Persistence
```

6. Cấu hình cả hai services, deploy cùng release, chạy Organization trước; kiểm tra directory nội bộ, health và login/admin flows. Health Organization kiểm tra database; dependency Identity được kiểm tra trực tiếp khi xác thực request.

Rollback trước khi mở ghi mới có thể quay về backup/version cũ. Sau khi có dữ liệu mới ở Organization, không chuyển writer về Identity hoặc chạy migration Down khi chưa reconcile dữ liệu; rollback tên unique có thể thất bại nếu tên trùng hợp lệ đã được tạo. Không drop bảng read model hoặc historical migrations của Identity.

## Kiểm chứng

```powershell
dotnet test src/Services/Organization/Organization.Test/Organization.Test.csproj -p:NuGetAudit=false
dotnet test src/Services/Identity/Identity.Test/Identity.Test.csproj -p:NuGetAudit=false
dotnet build src/StoreManagement.slnx -p:NuGetAudit=false
```

Organization tests chạy SQLite relational và ASP.NET TestServer với JWT thật, mock HTTP Identity dependency. Chúng không thay thế kiểm thử SQL Server về locking, unique indexes dưới concurrency, application locks và import dữ liệu thực. SQL scripts/migrations được generate/review mà không kết nối database production.
