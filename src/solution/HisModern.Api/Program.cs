using HisModern.Api.Infrastructure;
using HisModern.Application;
using HisModern.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Controllers。停用自動 400 ModelState 過濾器，
// 改由各服務 / 驗證器產出與 legacy 一致的 { ok:false, msg } 回應，維持對外契約。
builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// RFC 7807 ProblemDetails + 全域例外處理 (取代 legacy 吞例外)。
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 各層各自的 DI 註冊。
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

// 非預期錯誤 -> ProblemDetails。
app.UseExceptionHandler();

// 不分環境一律開 Swagger (方便 lab 操作，對齊 legacy)。
app.UseSwagger();
app.UseSwaggerUI();

// 預設載入 wwwroot/index.html (demo UI)。
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();

// 供整合測試 (WebApplicationFactory) 引用之用。
public partial class Program;
