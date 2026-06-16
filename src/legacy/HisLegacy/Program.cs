using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HisLegacy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 因為 Model 都是 public 欄位 (field) 而非屬性 (property),
            // 早期有人為了讓 JSON 序列化/反序列化能動, 直接打開 IncludeFields。
            builder.Services
                .AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.IncludeFields = true);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // 不分環境一律開 Swagger (方便 lab 操作)
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseDefaultFiles();   // 預設載入 wwwroot/index.html
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
