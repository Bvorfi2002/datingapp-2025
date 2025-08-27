using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class EmailVerificationDto
    {
        public string? Result { get; set; }
        public string? Reason { get; set; }
        public string? Disposable { get; set; }
        public string? Accept_all { get; set; }
        public string? Role { get; set; }
        public string? Free { get; set; }
        public string? Email { get; set; }
        public string? User { get; set; }
        public string? Domain { get; set; }
        public string? Safe_to_send { get; set; }
        public string? Did_you_mean { get; set; }
        public string? Success { get; set; }
        
    }
}