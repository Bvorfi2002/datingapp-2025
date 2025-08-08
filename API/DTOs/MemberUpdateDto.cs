using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class MemberUpdateDto
    {
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }

    }
}