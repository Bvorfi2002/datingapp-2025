using System.Diagnostics.CodeAnalysis; // Make sure this is added
using System.ComponentModel.DataAnnotations;

namespace API.Entities
{
    public class Group
    {
        // Add the traditional constructor here
        [SetsRequiredMembers]
        public Group(string name)
        {
            Name = name;
        }

        [Key]
        public required string Name { get; set; }

        public ICollection<Connection> Connections { get; set; } = [];
    }
}