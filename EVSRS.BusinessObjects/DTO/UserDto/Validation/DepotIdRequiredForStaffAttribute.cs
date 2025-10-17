using System.ComponentModel.DataAnnotations;
using EVSRS.BusinessObjects.Enum;

namespace EVSRS.BusinessObjects.DTO.UserDto.Validation
{
    public class DepotIdRequiredForStaffAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is UserRequestDto userDto)
            {
                // If role is STAFF, DepotId is required
                if (userDto.Role == Role.STAFF)
                {
                    return !string.IsNullOrWhiteSpace(userDto.DepotId);
                }
                // For other roles, DepotId is optional
                return true;
            }
            
            return true;
        }

        public override string FormatErrorMessage(string name)
        {
            return "DepotId is required for staff members.";
        }
    }
}