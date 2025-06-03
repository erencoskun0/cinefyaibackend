using System;
using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// AI öneri kayıtlarını temsil eder.
    /// </summary>
    public class AILog
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }

        public string Result { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}