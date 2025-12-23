using System.Collections.Concurrent;
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

        // 使用 ConcurrentQueue 收集所有玩家的即時輸入，等待 GameEngine 處理
        public ConcurrentQueue<Input> PendingInputs { get; } = new();

        // 地圖上的資源清單
        public List<Resource> Resources { get; private set; } = new();

        // ----------------------------------------------------

        public CellState()
        {
            // 初始生成一些資源
            Resources.Add(new Resource(100, 100, 50));
            Resources.Add(new Resource(300, 400, 80));
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
    }
}