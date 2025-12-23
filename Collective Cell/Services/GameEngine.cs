// Services/GameEngine.cs
using Collective_Cell.Services;
using Collective_Cell.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Collective_Cell.Services

{
    public class GameEngine : BackgroundService
    {
        private readonly IHubContext<CellHub> _hubContext;
        private readonly CellState _cellState;
        private readonly TimeSpan _tickInterval = TimeSpan.FromMilliseconds(50); // 每 50ms (20 FPS) 更新一次

        // 透過建構式注入 Hub Context 和 CellState
        public GameEngine(IHubContext<CellHub> hubContext, CellState cellState)
        {
            _hubContext = hubContext;
            _cellState = cellState;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var lastElapsed = stopwatch.Elapsed;

            while (!stoppingToken.IsCancellationRequested)
            {
                // 計算自上次更新以來經過的時間 (deltaTime)
                var currentElapsed = stopwatch.Elapsed;
                var deltaTime = (currentElapsed - lastElapsed).TotalSeconds;
                lastElapsed = currentElapsed;

                // --- 1. 更新核心狀態 ---
                _cellState.UpdateState(deltaTime);

                // --- 2. 廣播狀態給所有連線的客戶端 ---
                // "ReceiveCellStateUpdate" 是前端要監聽的方法名
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveCellStateUpdate",
                    _cellState,
                    stoppingToken
                );

                // 控制遊戲迴圈的間隔時間
                var nextTick = _tickInterval - (stopwatch.Elapsed - currentElapsed);
                if (nextTick > TimeSpan.Zero)
                {
                    await Task.Delay(nextTick, stoppingToken);
                }
            }
        }
    }
}