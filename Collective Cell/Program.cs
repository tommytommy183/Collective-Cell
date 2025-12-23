// Program.cs
using Collective_Cell.Services;
using Collective_Cell.Hubs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// --- 1. 配置服務 (Services) ---
// 註冊 SignalR 服務
builder.Services.AddSignalR();
// 註冊 CellState 為單例 (Singleton) 服務，確保所有連線共享同一份狀態
builder.Services.AddSingleton<CellState>();
// 註冊 GameEngine 背景服務 (我們的遊戲迴圈)
builder.Services.AddHostedService<GameEngine>();

// 為了讓前端能跨域連線 (如果您前端和後端不在同一埠)
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy =>
        {
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .WithOrigins("http://localhost:5500", "http://127.0.0.1:5500") // 替換為您的前端開發埠
                  .AllowCredentials();
        });
});


var app = builder.Build();

// --- 2. 配置中介軟體 (Middleware) ---
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("CorsPolicy");
app.UseDefaultFiles(); // 啟用 index.html
app.UseStaticFiles();  // 啟用靜態檔案（例如您的 HTML, CSS, JS）

// --- 3. 配置 Hub 路由 ---
// 將我們的 Hubs 映射到一個 URL 路徑
app.MapHub<CellHub>("/cellHub");

app.Run();