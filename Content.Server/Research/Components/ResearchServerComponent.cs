namespace Content.Server.Research.Components
{
    [RegisterComponent]
    public sealed class ResearchServerComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] public string ServerName => _serverName;

        [DataField("servername")]
        private string _serverName = "RDSERVER";

        /// <summary>
        /// Tracks the different kinds of research points available to the server
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] //[DataField("points")]
        public Dictionary<string,int> SpecialisationPoints = new Dictionary<string, int>() { { "points", 0 } };

        /// <summary>
        /// To encourage people to spend points,
        /// will not accept passive points gain above this number for each source.
        /// </summary>
        [DataField("passiveLimitPerSource")]
        public int PassiveLimitPerSource = 30000;
        [ViewVariables(VVAccess.ReadOnly)] public int Id { get; set; }

        [ViewVariables(VVAccess.ReadOnly)]
        public List<ResearchPointSourceComponent> PointSources { get; } = new();

        [ViewVariables(VVAccess.ReadOnly)]
        public List<ResearchClientComponent> Clients { get; } = new();
    }
}
