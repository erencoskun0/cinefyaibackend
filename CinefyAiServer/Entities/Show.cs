using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CinefyAiServer.Entities
{
    /// <summary>
    /// Seansları temsil eder. Bir salonda, belirli bir saatte, bir film oynar.
    /// </summary>
    public class Show
    {
        public Guid Id { get; set; }
        public Guid MovieId { get; set; }
        public Movie Movie { get; set; }

        public Guid HallId { get; set; }
        public Hall Hall { get; set; }

        [Required]
        public DateOnly ShowDate { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }
        [Required]
        public TimeOnly EndTime { get; set; }

        public ICollection<Ticket> Tickets { get; set; }
    }
}