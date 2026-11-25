# Quy chuẩn module C# — lấy Identity làm mẫu

Tài liệu này là quy ước bắt buộc khi tạo module backend mới. Căn cứ là source Identity hiện tại, không phải bản mô tả login cũ trong `identity-architecture-agent.md`. Phân biệt rõ: **hiện trạng** giải thích code đang có; **quy chuẩn** xác định cách viết module mới. Không tạo project, framework hoặc adapter chỉ để đủ cây thư mục khi chưa có chức năng cần dùng.

## 1. Cấu trúc tổng thể

Thay `Identity` bằng tên module và `Identity` ở thư mục feature bằng tên nghiệp vụ tương ứng. Giữ cách viết `UserCases`, `Contract` số ít, `V1`, `Commands`, `Queries` như repo.

```text
src/Services/<Module>/
  <Module>.Api/
    Program.cs
    Abstractions/ApiEndpoint.cs
    Controllers/v1/<Feature>/<Feature>Api.cs
    Authorization/
    Middleware/
    DependencyInjection/Extensions/
    DependencyInjection/Options/
    Properties/launchSettings.json
    appsettings.json
    appsettings.Development.json
    <feature>.http
  <Module>.Contract/
    AssemblyReference.cs
    Services/V1/<Feature>/
      Command.cs
      Query.cs
      Response.cs
      DomainEvent.cs
      Validators/<Operation>Validator.cs
  <Module>.Application/
    AssemblyReference.cs
    UserCases/V1/Commands/<Feature>/<Operation>CommandHandler.cs
    UserCases/V1/Queries/<Feature>/<Operation>QueryHandler.cs
    Behaviors/
    Mapper/ServiceProfile.cs
    DependencyInjection/Extensions/ServiceCollectionExtensions.cs
  <Module>.Domain/
    AssemblyReference.cs
    Entities/<Feature>/
    Abstractions/Entities/
    Abstractions/Aggregates/
    Abstractions/IUnitOfWork.cs
    Repositories/I<Entity>Repository.cs
    Primitives/
    Enumerations/
    Exceptions/
  <Module>.Persistence/
    AssemblyReference.cs
    ApplicationDbContext.cs
    EFUnitOfWork.cs
    Configurations/<Entity>Configuration.cs
    Repositories/<Entity>Repository.cs
    Constants/TableNames.cs
    Interceptors/
    Outbox/
    Migrations/
    DependencyInjections/Extensions/ServiceCollectionExtensions.cs
    DependencyInjections/Options/
  <Module>.Infrastructure/
    AssemblyReference.cs
    DependencyInjections/Extensions/ServiceCollectionExtensions.cs
    DependencyInjections/Options/
    Authentication/
    Caching/
    Email/
    BackgroundJobs/
    Consumers/
    PipeObservers/
  <Module>.Test/
    Unit/
    Integration/
```

`Controllers` trong Identity chứa Carter modules, không phải MVC controllers. `DependencyInjection` và `DependencyInjections` là hai tên đang dùng ở các tầng; module mới giữ đúng mẫu của tầng tương ứng. Không tự thay cây này bằng một service CRUD tổng hợp hoặc một HTTP proxy dùng method/path/body tùy ý.

## 2. File trong Api

| File / thư mục mẫu Identity | Chức năng và quy tắc |
| --- | --- |
| `Program.cs` | Composition root: gọi DI extensions, lắp middleware, map Carter, Swagger, health, chạy host. Không viết truy vấn hoặc nghiệp vụ ở đây. |
| `Controllers/v1/Identity/AuthApi.cs` | Nhóm route auth; kế thừa ApiEndpoint, triển khai ICarterModule, nhận ISender và gửi request. |
| `Controllers/v1/Identity/UserAdminApi.cs` | Nhóm route quản trị; tổ chức endpoint theo feature, gắn policy, bind input và chuyển Result sang HTTP. Module mới dùng `<Feature>Api.cs`. |
| `Abstractions/ApiEndpoint.cs` | Hàm chung xử lý Result lỗi, tạo ProblemDetails. Không nhét repository hoặc business validation vào base class. |
| `Authorization/PermissionRequirement.cs` | Mô tả điều kiện quyền mà endpoint yêu cầu. |
| `Authorization/PermissionAuthorizationHandler.cs` | Đánh giá requirement từ user và scope đã xác thực. |
| `Authorization/ScopeBoundaryFilter.cs` | Kiểm tra boundary của route; không thay thế việc lọc scope trong repository. |
| `Middleware/ExceptionHandlingMiddleware.cs` | Chuyển exception bất ngờ thành HTTP lỗi và ghi log thích hợp. |
| `Middleware/FailedSecurityAuditMiddleware.cs` | Ghi nhận lỗi bảo mật; không ghi password, token hoặc request body nhạy cảm. |
| `DependencyInjection/Extensions/JwtExtensions.cs` | Đăng ký JWT, validation token/session/tenant và authorization. Module mới không copy nguyên implementation truy cập Identity database. |
| `SwaggerExtensions.cs`, `ConfigureSwaggerOption.cs` | Đăng ký tài liệu và cấu hình Swagger/API versions. |
| `IdentityBootstrapper.cs` | Khởi tạo dữ liệu Identity từ cấu hình môi trường; không biến bootstrap thành business endpoint. |
| `IdentityPermissionCatalogExtensions.cs` | Đăng ký catalog quyền riêng Identity. Module mới có catalog riêng khi cần. |
| `appsettings*.json` | Giá trị cấu hình mặc định và override môi trường; không commit secret thật. |
| `Properties/launchSettings.json` | Profile chạy local, port và environment; không dùng làm cấu hình production. |
| `*.http` | Ví dụ gọi API để kiểm tra thủ công, bao gồm authorization cần thiết. |

Route theo mẫu versioned API của Identity: `/api/v{version:apiVersion}/<resources>`, đăng ký `NewVersionedApi`, `HasApiVersion(1)` khi áp dụng cùng versioning stack. HTTP status chỉ quyết định tại Api: tạo mới 201, đọc 200, cập nhật không có body 204, validation 400, chưa đăng nhập 401, thiếu quyền 403, không tìm thấy 404, xung đột 409.

Hiện trạng `ApiEndpoint.HandlerFailure` mặc định dùng 400 cho phần lớn lỗi. Module mới phải mapping mã lỗi phù hợp, không mặc định mọi lỗi thành 400 hoặc suy status bằng cách so khớp nội dung message. Giữ response contract nhất quán trong một API; thay envelope hiện có cần xét tương thích client.

## 3. File trong Contract

| File | Nội dung |
| --- | --- |
| `Services/V1/<Feature>/Command.cs` | `public static class Command` chứa record cho thao tác ghi. Implement ICommand hoặc ICommand<TResponse>. |
| `Query.cs` | `public static class Query` chứa record cho thao tác đọc, implement IQuery<TResponse>. |
| `Response.cs` | `public static class Response` chứa DTO trả ra. Chỉ các trường client cần; không serialize entity/navigation property. |
| `DomainEvent.cs` | Contract sự kiện theo cách nhóm của Identity; phân biệt sự kiện nội bộ và integration event trước khi dùng liên module. Không đưa entity/DbContext vào payload. |
| `Validators/<Operation>Validator.cs` | AbstractValidator cho request: required, độ dài, format, range, quan hệ giữa các trường input. |
| `Abstraction/Message/*` trong Identity | ICommand, IQuery, handler interfaces, event abstractions: mô tả request/response cho MediatR. Đây là ứng viên shared, không để module mới reference Identity.Contract chỉ để lấy chúng. |
| `Enumerations/SortOrder.cs`, `Extensions/SortOrderExtension.cs` | Quy ước sort và chuyển đổi phục vụ contract. Không chứa truy vấn nghiệp vụ. |

Identity đang dùng Result/Error/ValidationResult từ `BuildingBlocks.Contracts.Shared`. Dùng lại implementation này; không tạo OperationResult có StatusCode, raw JSON hoặc một bộ Result khác. PageResult hiện vẫn ở Identity.Contract và có EF dependency; không coi nó là shared contract thuần trước khi tách phần thực thi truy vấn.

DTO dùng `record`, tên PascalCase; input nullable phải thể hiện đúng dữ liệu được phép thiếu. ActorId, TenantId, ScopeId dùng để cấp quyền phải đến từ context đã xác thực hoặc được đối chiếu; không tin giá trị client gửi.

## 4. File trong Application

| File | Chức năng |
| --- | --- |
| `UserCases/V1/Commands/<Feature>/*CommandHandler.cs` | Một handler cho một use case ghi: đọc context, kiểm tra điều kiện nghiệp vụ, gọi domain/repository, cập nhật dữ liệu, trả Result. |
| `UserCases/V1/Queries/<Feature>/*QueryHandler.cs` | Một handler cho một use case đọc: query có scope, projection sang DTO, paging/sort. Không ghi dữ liệu dưới tên query ở module mới. |
| `Behaviors/ValidationPipelineBehavior.cs` | Chạy validators trước handler, tạo ValidationResult khi input sai. |
| `Behaviors/PerformancePipelineBehavior.cs` | Đo và log thời gian xử lý; không log credentials hoặc payload nhạy cảm. |
| `Behaviors/TransactionPipelineBehavior.cs` | Điều phối transaction cho command; phải thống nhất với nơi gọi SaveChangeAsync. |
| `Mapper/ServiceProfile.cs` | AutoMapper Profile chứa mapping thực sự được dùng; không đặt business rule vào mapper. |
| `DependencyInjection/Extensions/ServiceCollectionExtensions.cs` | Register handlers, validators, behaviors và mapper từ đúng assembly. |
| `AssemblyReference.cs` | Marker lấy Assembly của project cho registration/scan; không chứa business code. |

Tên handler theo mẫu `CreateRoleCommandHandler`, `GetUsersQueryHandler`; constructor injection vào `private readonly` fields; Handle nhận request và CancellationToken, trả `Task<Result<T>>` theo interface. Không nhận HttpContext/ClaimsPrincipal hoặc trả IResult/HTTP status trong handler. Port external service đặt trong Application hoặc Domain theo tầng sở hữu, implementation đặt Infrastructure.

Business validation cần dữ liệu (trùng code, không có tài nguyên, trạng thái không cho sửa) nằm ở handler/domain, không giả định FluentValidation thay thế database constraint. Truy cập repository thông qua interface Domain, không reference Persistence để lấy implementation.

## 5. File trong Domain

| File / nhóm | Chức năng |
| --- | --- |
| `Entities/<Feature>/<Entity>.cs` | Danh tính và trạng thái nghiệp vụ; methods bảo vệ invariant. Không chứa routing/EF query/HTTP response. |
| `Abstractions/Entities/IEntity.cs`, `Entity.cs` | Hợp đồng/base entity và Id. Chỉ dùng base đáp ứng đúng mô hình. |
| `IAuditTableEntity.cs` | Đánh dấu các trường audit mà interceptor cập nhật. |
| `Abstractions/Aggregates/AggregateRoot.cs` | Aggregate root và quản lý domain events. |
| `Primitives/ValueObject.cs` | So sánh value object theo giá trị, không theo identity. |
| `Repositories/IRepositoryBase.cs` | Hợp đồng repository tổng quát. Tránh mở IQueryable làm rò EF ra use case mới. |
| `Repositories/IAppUserRepository.cs` và các interface đặc thù | Các thao tác đọc/ghi phục vụ domain/use case; implementation ở Persistence. Module mới đặt tên theo entity/aggregate của nó. |
| `Abstractions/IUnitOfWork.cs` | SaveChangeAsync và các repository/transaction operations thuộc cùng module. Không gom repository nhiều module vào một UoW. |
| `Abstractions/IIdentityTenantContext.cs` | Context tenant/scope đã xác thực của Identity. Tách abstraction neutral khi module khác cần dùng chung. |
| `Enumerations/*` | Các tập giá trị nghiệp vụ; không dùng string tùy ý thay enum có ý nghĩa. |
| `Exceptions/*` | Exception có chủ đích của domain; lỗi nghiệp vụ dự kiến vẫn ưu tiên Result/Error ở application boundary. |

AppUser/AppRole phụ thuộc ASP.NET Identity là đặc thù Identity; không bắt mọi entity mới kế thừa IdentityUser hoặc mang dependency authentication. Tenant, Branch, Organization không phải building block chung chỉ vì nhiều module biết Id của chúng.

## 6. File trong Persistence

| File | Chức năng |
| --- | --- |
| `ApplicationDbContext.cs` | DbSets, model registration, EF context của module; không chứa HTTP authorization. |
| `Configurations/<Entity>Configuration.cs` | IEntityTypeConfiguration: table, key, required, độ dài, index, FK, delete behavior, concurrency token. Mỗi entity một configuration. |
| `Constants/TableNames.cs` | Tên bảng tập trung, được configurations sử dụng. |
| `Repositories/RepositoryBase.cs` | Cài đặt thao tác EF dùng chung trong module. |
| `Repositories/<Entity>Repository.cs` | Cài đặt interface Domain; lọc tenant/scope và thực hiện truy vấn. Không tự commit ở mỗi phương thức nếu use case cần transaction chung. |
| `Repositories/IdentityTenantContext.cs` | Adapter lấy context Identity; module mới không query bảng Identity trực tiếp để tạo context riêng. |
| `EFUnitOfWork.cs` | Cài đặt UoW bằng EF, SaveChanges và transaction policy. |
| `Interceptors/UpdateAuditableEntitiesInterceptor.cs` | Cập nhật audit fields lúc lưu. |
| `Interceptors/ConvertDomainEventsToOutboxMessagesInterceptor.cs` | Chuyển domain events thành outbox rows trong transaction lưu dữ liệu. |
| `Outbox/OutboxMessage.cs` | Dữ liệu outbox để worker phát sau commit. |
| `Migrations/<timestamp>_<Name>.cs` | Up/Down thay đổi schema; tạo bằng EF tooling, review SQL và ảnh hưởng dữ liệu. |
| `Migrations/*.Designer.cs`, `*ModelSnapshot.cs` | Metadata EF sinh; không sửa tay để che lệch model. |
| `DependencyInjections/Options/SqlServerRetryOptions.cs` | Options cho retry SQL; cấu hình ở DI, không hardcode từng repository. |
| `DependencyInjections/Extensions/ServiceCollectionExtensions.cs` | Đăng ký DbContext, provider, interceptors, repositories, UoW. |

Một use case ghi có một chủ sở hữu transaction/commit. Không vừa save trong endpoint, vừa trong repository, vừa trong pipeline. Transaction không complete khi Result là failure; propagate CancellationToken tới query/save; không dispose scoped UoW bằng pipeline khi scope DI còn sử dụng.

## 7. File trong Infrastructure

| Nhóm file Identity | Chức năng |
| --- | --- |
| `Authentication/JwtTokenService.cs` | Phát token của Identity; module nghiệp vụ thông thường chỉ xác thực token, không copy việc phát token. |
| `Caching/CacheService.cs` | Adapter cache theo interface; key/TTL và serialization có quy ước rõ. |
| `Email/SmtpIdentityEmailSender.cs` | Adapter gửi email; template và quy tắc xác nhận/reset thuộc nghiệp vụ Identity. |
| `BackgroundJobs/ProcessOutboxMessagesJob.cs` | Đọc/phát outbox, ghi trạng thái và retry. Consumer phải xử lý lặp an toàn. |
| `Consumers/*` | Consumers, document/projection model và Mongo repository phục vụ read model/event processing. Không thay SQL aggregate bằng document dùng chung tùy ý. |
| `PipeObservers/Logging*Observer.cs` | Logging các chặng send/receive/publish/consume của message bus. |
| `DependencyInjections/Options/*Options.cs` | POCO bind cấu hình JWT/cache/message bus/email; validate cấu hình bắt buộc khi khởi động. |
| `DependencyInjections/Extensions/ServiceCollectionExtensions.cs` | Đăng ký adapter, cache, messaging, scheduler, health checks theo nhu cầu module. |
| `NameFormatterExtension.cs` | Quy ước tên message bus. Không đổi tên queue/event production tùy tiện. |

Không bắt module mới bật Redis, RabbitMQ, Mongo hoặc Quartz nếu chưa sử dụng. External calls có timeout và cancellation; không bỏ xác thực, tự nâng quyền hoặc ghi secret vào log để làm integration chạy được.

## 8. Dependency và BuildingBlocks

```text
Api -> Application, Contract
Api -> Persistence, Infrastructure       (chỉ composition/DI)
Application -> Domain, Contract
Persistence -> Domain
Infrastructure -> Domain/Application     (các port cần triển khai)
Contract -> shared contracts
Domain -> shared domain primitives      (nếu cần)
BuildingBlocks -> không reference Services
```

Module không reference implementation của module khác hoặc đọc/ghi bảng của owner khác. Quyết định owner trước khi tạo entity/migration. Tích hợp qua typed contract/event/API adapter; adapter không thay thế application use cases bằng một request chuyển tiếp HTTP tổng quát.

Code cần dùng thật ở ít nhất hai module và độc lập nghiệp vụ có thể extract lên BuildingBlocks theo tầng. Chuyển implementation, cập nhật mọi consumer, xóa bản sao và kiểm chứng tương thích. Không reference Identity chỉ để dùng Result, base handler hoặc validator behavior. Không gom EF/ASP.NET/MassTransit vào assembly primitive bắt Domain phải phụ thuộc theo.

## 9. Naming và format C# bắt buộc cho file mới

- UTF-8, indent 4 spaces, không tab; một newline cuối file; không trailing whitespace.
- Theo mẫu Identity: namespace có block `{ }`; brace mở ở dòng mới cho namespace, type, method, constructor và control flow mới. Không nén cả class hoặc nhiều statements trên một dòng.
- Namespace theo project/thư mục; các namespace `Identity.Presentation.*` là hiện trạng lịch sử của Api. Module mới dùng `<Module>.Api.*` nhất quán, không tạo project Presentation chỉ vì namespace cũ.
- PascalCase cho type, method, property, enum; camelCase cho parameter/local; `_camelCase` cho private fields; interface bắt đầu `I`.
- File trùng tên type chính: `Product.cs`, `ProductConfiguration.cs`, `IProductRepository.cs`, `ProductRepository.cs`, `CreateProductCommandHandler.cs`. Ngoại lệ có chủ đích: nested records trong `Command.cs`, `Query.cs`, `Response.cs`, `DomainEvent.cs` theo Identity.
- Constructor injection tường minh và `private readonly` fields theo handler Identity; không dùng service locator trong business code. Fields → constructor → public methods → private helpers.
- `using` ở đầu file, bỏ unused/duplicate; nhóm System, thư viện ngoài, project namespaces, sau cùng `using static`/alias. Không copy những using template không sử dụng trong file cũ.
- Dùng `sealed` cho implementation không có nhu cầu kế thừa; `static` cho extension/catalog/container. Chỉ public các type cần expose qua assembly.
- Async I/O dùng `Task`/`Task<T>`, suffix `Async` trừ tên do framework quy định như `Handle`; parameter `cancellationToken` ở cuối và truyền tiếp. Không `.Result`, `.Wait()` hoặc `async void` trong nghiệp vụ.
- Bật nullable, khai báo `?` đúng nghĩa; không rải `!` để né cảnh báo. Chuỗi thời gian lưu UTC với tên `*Utc`, giữ kiểu thời gian nhất quán với domain hiện có.
- Error code ổn định theo `<Module>.<Resource>.<Reason>`; message dễ hiểu, không chứa SQL/stack trace/secret. Không dùng exception cho lỗi validation thông thường.
- Comment giải thích lý do, invariant hoặc giới hạn; không giữ code commented-out, banner region trang trí hoặc comment mô tả lại tên method.
- `.csproj` format nhiều dòng, PropertyGroup/ItemGroup rõ ràng, version package/framework tương thích baseline solution. Không tạo file project một dòng.

Đây là quy chuẩn cho phần mới; Identity có file cũ chưa tuân thủ hết. Không reformat toàn repo hoặc đổi public contract chỉ để đồng nhất style khi task không yêu cầu.

## 10. Mẫu file handler

Ví dụ hình thức cho module Catalog; các interface/contract phải được tạo đúng tầng trước khi dùng. Không import interface từ Identity để làm ví dụ compile.

```csharp
using BuildingBlocks.Contracts.Shared;
using BuildingBlocks.Contracts.Message;
using Catalog.Domain.Repositories;
using static Catalog.Contract.Services.V1.Products.Query;
using static Catalog.Contract.Services.V1.Products.Response;

namespace Catalog.Application.UserCases.V1.Queries.Products
{
    public sealed class GetProductQueryHandler : IQueryHandler<GetProduct, ProductDetail>
    {
        private readonly IProductRepository _productRepository;

        public GetProductQueryHandler(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<Result<ProductDetail>> Handle(
            GetProduct request,
            CancellationToken cancellationToken)
        {
            // Repository áp dụng scope từ context đã xác thực.
            var product = await _productRepository.GetByIdAsync(
                request.ProductId,
                cancellationToken);

            if (product is null)
            {
                return Result.Failure<ProductDetail>(new Error(
                    "Catalog.Product.NotFound",
                    "Product was not found."));
            }

            return Result.Success(new ProductDetail(product.Id, product.Name));
        }
    }
}
```

## 11. Các điểm Identity hiện có không được nhân rộng

| Hiện trạng | Quy tắc module mới |
| --- | --- |
| Application reference Persistence | Chỉ reference Domain/Contract và shared abstraction; bỏ using implementation không cần thiết. |
| Transaction behavior nhận diện command bằng tên kết thúc `Command`, trong khi nhiều record tên `CreateRole`, `Register` | Nhận diện bằng marker/interface command; kiểm thử transaction thực sự chạy, chỉ complete khi thành công. |
| Handler và pipeline cùng có SaveChangeAsync | Chọn một cơ chế commit rõ ràng theo use case; tránh double-save và commit khi lỗi. |
| Login/logout/revoke có thao tác ghi nhưng một số nằm trong Queries | Phân loại module mới theo tác động dữ liệu; request đọc không thực hiện mutation. |
| Một số handler/validator được gom chung file | Module mới dùng một handler/use case và một validator chính/file để dễ tìm; Contract containers vẫn là ngoại lệ chuẩn. |
| ApiEndpoint mặc định nhiều lỗi thành 400 | Mapping Error sang HTTP đúng loại, giữ validation details. |
| Namespace, using, brace và file cũ chưa đồng nhất | Theo mục format cho code mới, không lấy một file cũ bất kỳ làm chuẩn định dạng. |

## 12. Quy trình tạo một feature mới

1. Xác định module owner, aggregate, quyền, tenant/scope và schema.
2. Tạo Contract Command/Query/Response cùng validators.
3. Tạo entity/invariants và repository interface Domain theo nhu cầu.
4. Tạo configuration, repository và UoW implementation Persistence; migration khi schema thay đổi.
5. Tạo handler đúng thư mục Commands/Queries, mapping DTO và Error codes.
6. Đăng ký assembly, validators, pipeline, repository, UoW và adapter ở DI extension từng tầng.
7. Tạo Carter endpoint mỏng, gắn version/authorization, gửi ISender và map Result.
8. Thêm project vào solution, cập nhật `.http` và cấu hình cần thiết.
9. Build; test validation, thành công/thất bại nghiệp vụ, scope/quyền, persistence/transaction nếu có thay đổi liên quan. Không chỉ test handler trả lại đúng dữ liệu mock.

Không coi feature hoàn tất khi thiếu DI registration, reference sai chiều, pipeline chưa chạy, tenant filter chưa áp dụng hoặc migration chưa được kiểm chứng.
