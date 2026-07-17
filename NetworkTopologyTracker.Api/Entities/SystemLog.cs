using System;

namespace NetworkTopologyTracker.Api.Entities
{
    public class SystemLog
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty; 
        public string Action { get; set; } = string.Empty;   
        public string Details { get; set; } = string.Empty;  
        public string ActionType { get; set; } = string.Empty; 
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; 
    }
}