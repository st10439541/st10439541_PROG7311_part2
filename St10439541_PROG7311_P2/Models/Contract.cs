using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using St10439541_PROG7311_P2.Models.ContractStates;

namespace St10439541_PROG7311_P2.Models
{
    public enum ContractStatus
    {
        Draft,
        PendingClientSignature,
        Active,
        Expired,
        OnHold
    }

    public enum ServiceLevel
    {
        Standard,
        Premium,
        Enterprise
    }

    public class Contract
    {
        [Key]
        public int ContractId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        [Required]
        public ServiceLevel ServiceLevel { get; set; } = ServiceLevel.Standard;

        [StringLength(500)]
        public string? TermsAndConditions { get; set; }

        [Display(Name = "Signed Agreement")]
        public string? PdfFilePath { get; set; }

        [NotMapped]
        [Display(Name = "Upload Signed Agreement")]
        public IFormFile? PdfFile { get; set; }

        [Display(Name = "Signed By Client")]
        public bool IsSignedByClient { get; set; } = false;

        [Display(Name = "Signature Date")]
        [DataType(DataType.DateTime)]
        public DateTime? SignatureDate { get; set; }

        // Navigation properties
        [ForeignKey("ClientId")]
        public virtual Client? Client { get; set; }

        public virtual ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();

        
        // STATE PATTERN PROPERTY
        
        [NotMapped]
        public IContractState State => ContractStateFactory.GetState(Status);

        
        // BUSINESS RULE METHODS (Updated with State Pattern)
        

        public bool CanCreateServiceRequest()
        {
            return State.CanCreateServiceRequest() && IsSignedByClient;
        }

        public bool IsExpired()
        {
            return DateTime.Today > EndDate || Status == ContractStatus.Expired;
        }

        public bool CanBeSigned()
        {
            return State.CanBeSigned() && !IsSignedByClient;
        }

        // NEW: Check if contract can be edited
        public bool CanBeEdited()
        {
            return State.CanBeEdited();
        }

        // NEW: Get display name from state
        public string GetStateDisplayName()
        {
            return State.GetDisplayName();
        }
    }
}