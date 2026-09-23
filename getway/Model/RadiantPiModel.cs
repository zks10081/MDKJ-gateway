namespace getway.Model
{
    internal class RadiantPiModel
    {
        public DateTime Timestamp { get; init; }
        public string RawMessage { get; init; } = string.Empty;
        public string ParsedStatus { get; init; } = string.Empty;
    }
}
