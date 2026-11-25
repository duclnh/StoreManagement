# Architecture rules

Khi tạo, sửa hoặc review backend module hoặc file C#, bắt buộc tuân thủ cấu trúc, trách nhiệm file,
naming và format trong [docs/csharp-module-standard.md](docs/csharp-module-standard.md).
Tài liệu này lấy source Identity làm mẫu và phân biệt rõ hiện trạng legacy với quy chuẩn cho code mới.

Identity là module tham chiếu cho Carter + CQRS/MediatR + Result + repository/UoW.
Không sao chép các phụ thuộc legacy của Identity hoặc dùng Identity làm thư viện dùng chung.
