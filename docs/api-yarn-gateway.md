# ApiYarnGateway

YARP reverse proxy, .NET 8, port local `5100`.

```powershell
dotnet run --project src/Services/ApiYarnGateway/ApiYarnGateway.csproj
```

Build and run the gateway container from the repository root:

```powershell
docker compose up --build api-yarn-gateway
```

The gateway is available at `http://localhost:5100`. The same Compose file also builds Identity and Organization and routes to them by service name. SQL Server, Redis, RabbitMQ, and MongoDB are started as dependencies. Set `IDENTITY_DB_CONNECTION` and `ORGANIZATION_DB_CONNECTION` in a root `.env` file to override the development database connection strings. Apply the service migrations before using database-backed endpoints.

| Gateway route | Destination mặc định |
| --- | --- |
| `/api/v1/auths/**`, `/api/v1/admin/**` | Identity `http://localhost:5168/` |
| `/api/v1/tenants/**`, `/api/v1/organizations/**`, `/api/v1/branches/**`, `/api/v1/warehouses/**`, `/api/v1/pos/**` | Organization `http://localhost:5102/` |
| `/health` | Health của process gateway |

Route giữ nguyên path, query, body và Authorization header. Identity/Organization tiếp tục xác thực JWT, session, scope và permission; gateway không tạo authentication riêng. Không có route catch-all đến backend, không expose `/internal`, Swagger hay health backend. Header `X-Service-Key` và các forwarded headers do client gửi bị loại bỏ trước khi chuyển tiếp. Không bật trust forwarded headers ở backend chỉ vì có gateway; cấu hình trusted proxies riêng nếu cần lấy client IP.

Đổi destination khi deploy bằng environment variables:

```text
ReverseProxy__Clusters__identity__Destinations__primary__Address=https://identity.internal/
ReverseProxy__Clusters__organization__Destinations__primary__Address=https://organization.internal/
```

Gateway là host hạ tầng, không tạo Domain/Application/Persistence rỗng. Program chỉ map pipeline; DI ở `DependencyInjection/Extensions`, routes/clusters ở appsettings. Health gateway không xác nhận backend sẵn sàng; backend unavailable được proxy trả lỗi. TLS có thể terminate tại ingress hoặc cấu hình Kestrel HTTPS theo môi trường. Gateway không tự thêm retry cho mutation.
