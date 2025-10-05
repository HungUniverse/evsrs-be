using EVSRS.BusinessObjects.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVSRS.BusinessObjects.Entity
{
    public class Notification : BaseEntity
    {
        public string? UserId { get; set; }
        public string? NotificationTitle { get; set; }
        public string? NotificationContent { get; set; }
        public string? Type { get; set; }
        public string? ActionUrl { get; set; }
        public string? MetaData { get; set; }
        public bool IsRead { get; set; } = false;


        public ApplicationUser? Receiver { get; set; }
    }
}
