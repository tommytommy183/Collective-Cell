namespace Collective_Cell.Models
{
    public class CellState
    {
        // 細胞的即時生命值/能量
        public double Health { get; set; } = 1000;
        // 細胞的體積/大小（影響畫面呈現）
        public double Size { get; set; } = 50;
        // 當前累積的微觀輸入（由所有玩家貢獻）
        public Dictionary<string, int> AccumulatedInputs { get; set; } = new();
        // 細胞的當前主要突變類型 (例如：Attack, Defense, Speed)
        public string CurrentMutationType { get; set; } = "Neutral";
        // 地圖上的資源點位置（例如：食物粒子）
        //public List<Resource> Resources { get; set; } = new();
    }
}
