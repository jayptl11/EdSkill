# AGENT.md

Huong dan nay ap dung cho Codex khi lam viec trong repo EdSkill. Muc tieu la giu code dung Clean Architecture, dung pattern hien co va tranh dua chi tiet ha tang vao layer sai.

## Tong quan kien truc

Solution hien co gom cac layer:

- `src/EdSkill.Domain`: core domain. Chua entity, enum, logic domain thuan. Khong phu thuoc `Application`, `Infrastructure`, `API`.
- `src/EdSkill.Application`: use case layer. Chua CQRS/MediatR command, query, handler, validator, DTO, interface abstraction va `Result`. Chi phu thuoc `Domain`.
- `src/EdSkill.Infrastructure`: implement cac interface cua Application. Chua EF Core `AppDbContext`, migration, service JWT/email/password/OTP/Redis/Google, settings va DI ha tang.
- `src/EdSkill.API`: delivery layer. Chua controller, middleware, `Program.cs`, HTTP mapping, auth pipeline va swagger. Goi Application thong qua MediatR.
- `tests/EdSkill.UnitTests`: unit tests theo feature, uu tien test handler/validator/use case.

Dependency rule:

```text
Domain <- Application <- Infrastructure <- API
                     ^--------------- API
```

Layer ben trong khong duoc tham chieu layer ben ngoai. Neu can goi dich vu ngoai, tao interface trong `Application/Common/Interfaces`, implement trong `Infrastructure`.

## Quy tac bat buoc theo layer

### Domain

- Dat entity trong `src/EdSkill.Domain/Entities`.
- Dat enum trong `src/EdSkill.Domain/Enums`.
- Khong them dependency den ASP.NET Core, EF Core, MediatR, FluentValidation, Redis, JWT, email provider hoac config.
- Khong viet logic HTTP, persistence, cache, logging ha tang trong Domain.
- Entity nen giu invariant/domain behavior neu co; tranh bien Domain thanh noi chua DTO request/response.

### Application

- Moi use case dat theo feature:

```text
src/EdSkill.Application/Features/{Feature}/Commands/{UseCase}/
src/EdSkill.Application/Features/{Feature}/Queries/{UseCase}/
src/EdSkill.Application/Features/{Feature}/DTOs/
```

- Dung MediatR:
  - Command/query implement `IRequest<Result>` hoac `IRequest<Result<T>>`.
  - Handler implement `IRequestHandler<TRequest, Result>` hoac `IRequestHandler<TRequest, Result<T>>`.
- Validation dat trong `*Validator.cs` bang FluentValidation. Khong lap validation input trong controller neu da co validator.
- Handler chi dieu phoi use case: doc/ghi qua `IApplicationDbContext`, goi interface service, tao entity/domain object va tra `Result`.
- Khong inject truc tiep implementation cua Infrastructure vao handler. Chi inject interface tu `Application/Common/Interfaces`.
- Khong dung `HttpContext`, controller types, `IConfiguration`, settings concrete, Redis/JWT/email SDK trong Application.
- Dung `CancellationToken` cho async EF/service call.
- Loi nghiep vu tra ve `Result.Failure(errorCode, errorMessage)`, khong throw exception cho flow du kien.
- Neu can persistence moi, them DbSet vao `IApplicationDbContext` va implement o `AppDbContext`.

### Infrastructure

- Implement interface cua Application trong `src/EdSkill.Infrastructure/Services`.
- EF Core context/migration nam trong `src/EdSkill.Infrastructure/Persistence`.
- Settings nam trong `src/EdSkill.Infrastructure/Settings` va bind trong `DependencyInjection.cs`.
- Dang ky DI trong `src/EdSkill.Infrastructure/DependencyInjection.cs`.
- Chi Infrastructure duoc lam viec truc tiep voi EF Core provider, Redis, JWT, BCrypt, Google Auth, email provider, system clock implementation.
- Khong dat business use case trong Infrastructure. Neu service co logic nghiep vu lon, day rule ve Application/Domain va chi giu implementation ha tang tai day.

### API

- Controller chi lam cac viec:
  - map route/body/query/header sang command/query;
  - goi `_sender.Send(...)`;
  - map `Result` sang HTTP response.
- Khong dat business rule, truy van EF truc tiep, hash password, tao token, gui email trong controller.
- Middleware chi xu ly cross-cutting HTTP concern nhu exception, auth, blacklist, banned user.
- Dang ky Application/Infrastructure trong `Program.cs`; tranh dang ky service use case rieng le trong API neu da co extension DI.
- Response HTTP nen nhat quan voi `Result`: success -> `Ok`/`Created`, known error code -> status code phu hop, fallback -> `BadRequest`.

## Naming va file layout

- Namespace phai khop folder, vi du:

```csharp
namespace EdSkill.Application.Features.Auth.Commands.Register;
```

- Dat ten class theo pattern:
  - `{UseCase}Command.cs`
  - `{UseCase}CommandHandler.cs`
  - `{UseCase}CommandValidator.cs`
  - `{UseCase}Query.cs`
  - `{UseCase}QueryHandler.cs`
  - `{UseCase}QueryValidator.cs` neu can
  - `{Feature}Dtos.cs` hoac DTO rieng khi file qua lon
- Request/response DTO dung cho API/Application DTO, khong dat trong Domain.
- Error code viet uppercase snake case, vi du `EMAIL_EXISTS`, `OTP_RATE_LIMITED`.

## Cach them mot use case moi

1. Tao command/query trong `EdSkill.Application/Features/{Feature}/Commands|Queries/{UseCase}`.
2. Tao validator bang FluentValidation neu co input tu user.
3. Tao handler va chi inject abstraction can thiet.
4. Neu can service/persistence ngoai:
   - tao interface trong `Application/Common/Interfaces`;
   - implement trong `Infrastructure/Services`;
   - dang ky trong `Infrastructure/DependencyInjection.cs`.
5. Neu can endpoint, them action trong controller API tuong ung va goi MediatR.
6. Them unit test cho validator va handler trong `tests/EdSkill.UnitTests/Features/{Feature}`.
7. Chay build/test lien quan truoc khi ket thuc.

## Testing

- Framework hien co: xUnit, FluentAssertions, Moq.
- Unit test handler nen mock `IApplicationDbContext` va cac service interface.
- Validator test nen cover input hop le, required fields, format, length/rule quan trong.
- Khong goi database/Redis/email/JWT provider that trong unit test.
- Ten test theo pattern dang co:

```csharp
Handle_WhenCondition_ReturnsExpectedResult()
```

- Lenh hay dung:

```powershell
dotnet build EdSkill.slnx
dotnet test tests/EdSkill.UnitTests/EdSkill.UnitTests.csproj
```

## Quy tac khi sua code

- Doc pattern hien co truoc khi them abstraction moi.
- Giu thay doi nho, dung feature/layer lien quan; khong refactor lan rong neu khong can.
- Khong sua migration thu cong tru khi co ly do ro rang. Neu thay doi model EF, tao migration moi.
- Khong dua secret, connection string that, API key vao source.
- Khong rollback hoac xoa thay doi cua nguoi dung neu khong duoc yeu cau.
- Neu phat hien code hien co vi pham Clean Architecture, chi sua khi can cho task; neu khong, neu ro trong phan ket qua.

## Checklist truoc khi hoan thanh

- Layer dependency van dung: Domain khong biet Application/Infrastructure/API; Application khong biet Infrastructure/API.
- Controller khong co business logic.
- Handler khong dung implementation ha tang truc tiep.
- Interface moi o Application da co implementation va DI o Infrastructure neu can.
- Async call co truyen `CancellationToken`.
- Validator/test da duoc them hoac cap nhat theo rui ro thay doi.
- `dotnet build` va test lien quan da chay, hoac ghi ro ly do khong chay duoc.
