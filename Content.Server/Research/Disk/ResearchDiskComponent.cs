namespace Content.Server.Research.Disk
{
    [RegisterComponent]
    public sealed class ResearchDiskComponent : Component
    {
        [DataField("points")]
        public Dictionary<string, int> Points { get; } = new();
        [DataField("technology")]
        public string Technology = "";
    }
}
