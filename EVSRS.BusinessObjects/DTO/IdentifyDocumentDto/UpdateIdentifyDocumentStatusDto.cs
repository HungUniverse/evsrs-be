using EVSRS.BusinessObjects.Enum;
using System.ComponentModel.DataAnnotations;

namespace EVSRS.BusinessObjects.DTO.IdentifyDocumentDto
{
    public class UpdateIdentifyDocumentStatusDto
    {
        [Required(ErrorMessage = "Status is required")]
        public IdentifyDocumentStatus Status { get; set; }
        
        public string? Note { get; set; }
    }
}