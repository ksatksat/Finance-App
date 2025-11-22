using System.ComponentModel.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace FinanceApp.Models
{
    public class Expense
    {
        public int Id { get; set; }
        [Required]
        public string Description { get; set; } = null!;
        [Required]
        //[Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", 
            ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
        [Required]
        public string Category { get; set; } = null!;
        /*each time when user create Expense it will put the actual time*/
        public DateTime Date { get; set; } = DateTime.Now;
        //new for Authorization and Authentication
        public string? UserId { get; set; }
    }
}
