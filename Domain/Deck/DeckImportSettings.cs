using System;

namespace Domain.Deck
{
    public class DeckImportSettings
    {
        public Guid UserId { get; set; }
        public long DeckId { get; set; }
        public string DeckName { get; set; } = null!;
        public List<DeckImportFieldMapping> FieldMappings { get; set; } = new();
    }

    public class DeckImportFieldMapping
    {
        public Guid UserId { get; set; }
        public int Id { get; set; }
        public long DeckId { get; set; }
        public string FrontKey { get; set; } = null!;
        public string BackKey { get; set; } = null!;
    }
}