using System.Collections.Generic;

namespace NetworkTopologyTracker.Api.Entities
{
    public class Institution
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "Talas Belediyesi"
        public string? Description { get; set; }

        // İlişki: Bir kurumun birden fazla binası (Facility) olabilir.
        public ICollection<Facility> Facilities { get; set; } = new List<Facility>();
    }
}