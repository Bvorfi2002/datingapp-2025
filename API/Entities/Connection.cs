using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Entities
{
    public class Connection(string connectionId, string userId)
    {

        public string ConnectionId { get; set; } = connectionId;
        public string UserId { get; set; } = userId;

        //nav prop
        public Group Group { get; set; } = null!;
    }
}