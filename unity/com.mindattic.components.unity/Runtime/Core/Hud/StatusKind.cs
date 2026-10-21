namespace MindAttic.Components.Unity.Core
{
    /// <summary>What a status pill (<c>MaStatusPill</c>) reports; picks its dot and outline colour (<see cref="ControlStyles.Status"/>).</summary>
    public enum StatusKind
    {
        /// <summary>Nothing to report either way (idle, offline by choice): a muted dot.</summary>
        Neutral,
        /// <summary>All good (live, hosting): success green.</summary>
        Ok,
        /// <summary>In progress or degraded (connecting, reconnecting, lagging): warning amber; pulses by default.</summary>
        Warn,
        /// <summary>Broken (offline, refused): danger red.</summary>
        Error,
    }
}
