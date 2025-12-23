// Hubs/CellHub.cs
using Collective_Cell.Services;
using Microsoft.AspNetCore.SignalR;
using static Collective_Cell.Services.CellState;

namespace Collective_Cell.Hubs
{
    // Hub 負責處理客戶端的連線、斷線以及接收遊戲指令
    public class CellHub : Hub
    {
        private readonly CellState _cellState;

        // 注入 CellState
        public CellHub(CellState cellState)
        {
            _cellState = cellState;
        }

        // 客戶端呼叫此方法來貢獻能量或發出指令
        // inputType: "Absorb" 或 "Emit"
        // amount: 貢獻的數值
        public async Task ContributeEnergy(string inputType, double amount)
        {
            // 在這裡可以加上玩家 ID 驗證，但目前先只記錄貢獻
            // 伺服器記錄這個輸入，並等待 GameEngine 在下一個 Tick 處理
            _cellState.AddInput(inputType, amount);

            // 可選：回覆給單個玩家確認收到
            await Clients.Caller.SendAsync("InputReceived", inputType, amount);
        }

        // 玩家連線時
        public override async Task OnConnectedAsync()
        {
            // 剛連線時，先向該客戶端發送一次當前細胞的完整狀態
            await Clients.Caller.SendAsync("ReceiveCellStateUpdate", _cellState);
            await base.OnConnectedAsync();
        }

        public void ContributeMovement(string direction)
        {
            Vector2 movementVector = new Vector2();
            double moveSpeed = 20.0; // 每個按鈕貢獻的移動量

            // 根據玩家點擊的方向，計算向量
            switch (direction.ToLower())
            {
                case "up":
                    movementVector = new Vector2 { DX = 0, DY = -moveSpeed };
                    break;
                case "down":
                    movementVector = new Vector2 { DX = 0, DY = moveSpeed };
                    break;
                case "left":
                    movementVector = new Vector2 { DX = -moveSpeed, DY = 0 };
                    break;
                case "right":
                    movementVector = new Vector2 { DX = moveSpeed, DY = 0 };
                    break;
                default:
                    return; // 忽略無效輸入
            }

            // 將向量加入佇列
            _cellState.PendingMovements.Enqueue(movementVector);
        }
    }
}