namespace Proposal.Models
{
    public class CalculationHistory
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string FormulaType { get; set; }
        public string InputDetails { get; set; }
        public string ResultContent { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}