using System.Collections.Concurrent;
using System.Numerics;
namespace Collective_Cell.Services
{
    // 代表地圖上的資源點
    public record Resource(double X, double Y, double Energy);

    // 代表單個玩家的貢獻
    public record Input(string Type, double Amount);

    public class CellState
    {
        // 鎖物件，用於確保多執行緒寫入時的同步性
        private readonly object _stateLock = new();

        // --- 主要狀態參數 ---
        public double Health { get; private set; } = 1000.0;
        public double Size { get; private set; } = 50.0;
        public string CurrentMutationType { get; private set; } = "Neutral";
        public DateTime LastUpdate { get; private set; } = DateTime.UtcNow;
        public double X { get; set; } = 400; // 預設中心 X 座標 (假設畫布寬度為 800)
        public double Y { get; set; } = 300; // 預設中心 Y 座標 (假設畫布高度為 600)

        // 使用 ConcurrentQueue 收集所有玩家的即時輸入，等待 GameEngine 處理
        public ConcurrentQueue<Input> PendingInputs { get; } = new();
        public ConcurrentQueue<Vector2> PendingMovements { get; } = new();
        // 地圖上的資源清單
        public List<Resource> Resources { get; private set; } = new();

        // ----------------------------------------------------

        public CellState()
        {
            // 初始生成一些資源
            Resources.Add(new Resource(100, 100, 50));
            Resources.Add(new Resource(300, 400, 80));
        }

        public struct Vector2
        {
            public double DX { get; set; } // X 方向的變化
            public double DY { get; set; } // Y 方向的變化
        }

        // 供 GameEngine 呼叫，執行遊戲邏輯更新
        public void UpdateState(double deltaTime)
        {
            lock (_stateLock)
            {
                // 1. 消耗：細胞體積越大，消耗越快
                Health -= Size * deltaTime * 0.05;

                // 2. 處理玩家輸入
                ProcessPendingInputs();

                // 3. 檢查突變
                CheckForMutation();

                // 4. 移動
                ProcessPendingMovements();

                // 確保 Health 不會小於 0
                Health = Math.Max(0, Health);
                LastUpdate = DateTime.UtcNow;
            }
        }

        // 供 Hubs 呼叫，玩家貢獻輸入
        public void AddInput(string type, double amount)
        {
            PendingInputs.Enqueue(new Input(type, amount));
        }

        private void ProcessPendingInputs()
        {
            // 簡化處理：假設 'Absorb' 增加 Health, 'Emit' 減少 Health
            // 這是您未來擴展複雜邏輯的地方！
            while (PendingInputs.TryDequeue(out var input))
            {
                if (input.Type.Equals("Absorb", StringComparison.OrdinalIgnoreCase))
                {
                    Health += input.Amount;
                }
                else if (input.Type.Equals("Emit", StringComparison.OrdinalIgnoreCase))
                {
                    Health -= input.Amount * 0.5; // 排放物有較小影響
                }
            }
        }

        private void CheckForMutation()
        {
            // 示例突變邏輯：當 Health 突破特定閾值時，細胞發生突變
            if (Health > 1500 && CurrentMutationType != "Giant")
            {
                CurrentMutationType = "Giant";
                Size *= 1.5; // 變大
                Health = 1000; // 重設健康值
            }
            else if (Health < 500 && CurrentMutationType != "Struggling")
            {
                CurrentMutationType = "Struggling";
            }
        }
        private void ProcessPendingMovements()
        {
            if (PendingMovements.IsEmpty)
                return;

            // 1. 收集所有輸入向量
            List<Vector2> allMovements = new List<Vector2>();
            while (PendingMovements.TryDequeue(out var input))
            {
                allMovements.Add(input);
            }

            if (!allMovements.Any())
                return;

            // 2. 計算平均向量
            double totalDX = allMovements.Sum(v => v.DX);
            double totalDY = allMovements.Sum(v => v.DY);
            int inputCount = allMovements.Count;

            // 3. 縮放平均向量（確保移動不過快）
            // 這裡我們將平均貢獻除以輸入數量，並根據 deltaTime 縮放。
            double averageDX = (totalDX / inputCount) * 0.1; // 0.1 是縮放係數
            double averageDY = (totalDY / inputCount) * 0.1;

            // 4. 更新細胞位置 (需要鎖定)
            lock (_stateLock)
            {
                X += averageDX;
                Y += averageDY;

                // 邊界檢查 (防止細胞移出畫布)
                X = Math.Max(Size, Math.Min(800 - Size, X)); // 假設畫布寬度 800
                Y = Math.Max(Size, Math.Min(600 - Size, Y)); // 假設畫布高度 600
            }
        }
    }
}