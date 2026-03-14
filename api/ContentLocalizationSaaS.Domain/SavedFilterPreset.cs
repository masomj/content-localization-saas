namespace ContentLocalizationSaaS.Domain;

public sealed class SavedFilterPreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public required string Name { get; set; }
    public string Query { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
