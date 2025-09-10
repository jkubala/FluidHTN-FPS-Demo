namespace FPSDemo.NPC
{
    public enum AIWorldState
    {
        AwareOfEnemy,
        HasEnemyInSight,
        IsPursuingEnemy,
        WeaponState,
        IsShooting,
        IsReloading,
        
        // Tactical positioning states
        HasBetterCoverAvailable,     // bool - better position exists than current
        CurrentPositionCompromised,   // bool - current position unsafe
        InEffectiveCoverPosition,    // bool - current position provides good cover
        BestCoverDistance,           // byte - distance to best cover (0-255 discretized)
        CoverQualityScore,          // byte - tactical value of best available position
        RequiresRepositioning,       // bool - immediate repositioning needed
        FlankingOpportunityAvailable // bool - flanking position available
    }
}