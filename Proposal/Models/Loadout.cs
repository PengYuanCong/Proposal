namespace Proposal.Models
{
    public class Loadout
    {
        public int Id { get; set; }
        public string LoadoutName { get; set; }
        // 儲存六件裝備的 ID
        public int? Eq1_Id { get; set; }
        public int? Eq2_Id { get; set; }
        public int? Eq3_Id { get; set; }
        public int? Eq4_Id { get; set; }
        public int? Eq5_Id { get; set; }
        public int? Eq6_Id { get; set; }

        // 用於計算公式：加總後的總數值
        public int TotalHP { get; set; }
        public int TotalAttack { get; set; }
        public int TotalPrice { get; set; }
    }
}