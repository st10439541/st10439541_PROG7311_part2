using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace St10439541_PROG7311_P2.Models
{
    public enum RequestStatus
    {
        Pending,
        InProgress,
        Completed,
        Cancelled,
        Accepted,  // New status
        Denied     // New status
    }

    public class ServiceRequest
    {
        [Key]
        public int ServiceRequestId { get; set; }

        [Required]
        public int ContractId { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Service Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount (USD)")]
        public decimal AmountUSD { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount (ZAR)")]
        public decimal AmountZAR { get; set; }

        [Required]
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Exchange Rate Used")]
        [Column(TypeName = "decimal(18,4)")]
        public decimal ExchangeRateUsed { get; set; }

        [Display(Name = "Admin Response Date")]
        [DataType(DataType.DateTime)]
        public DateTime? AdminResponseDate { get; set; }

        [Display(Name = "Admin Comments")]
        [StringLength(500)]
        public string? AdminComments { get; set; }

        // Navigation property
        [ForeignKey("ContractId")]
        public virtual Contract? Contract { get; set; }
    }
}