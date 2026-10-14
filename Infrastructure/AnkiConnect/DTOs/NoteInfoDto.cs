using System;

namespace Infrastructure.AnkiConnect.DTOs
{
    public class NoteInfoDto    
    {
        public long NoteId { get; set; }

        public Dictionary<string, AnkiFieldDto> Fields { get; set; } = null!;
    } 

    public class AnkiFieldDto
    {
        public string Value { get; set; } = null!;

        public int Order { get; set; }
    }
}