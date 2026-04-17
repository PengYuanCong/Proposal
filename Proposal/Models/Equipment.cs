using System.ComponentModel.DataAnnotations;

namespace Proposal.Models
{
    public class Equipment
    {
        [Key] // 告訴系統這是主鍵 (Primary Key)
        public int Id { get; set; }

        [Required] // 必填欄位
        [Display(Name = "裝備名稱")]
        public string Name { get; set; }

        public int HP { get; set; }

        public int Attack { get; set; }

        public int MagicAttack { get; set; }

        public int PhysicalDefense { get; set; }

        public int MagicDefense { get; set; }

        public int Price { get; set; }
    }
}
