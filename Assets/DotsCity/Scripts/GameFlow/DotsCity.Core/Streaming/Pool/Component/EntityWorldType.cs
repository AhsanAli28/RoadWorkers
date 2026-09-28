namespace Spirit604.DotsCity.Core
{
    public enum EntityWorldType
    {
        /// <summary>
        /// Entities work entirely in the DOTS space.
        /// </summary>
        PureEntity,

        /// <summary>
        /// Entities that combine DOTS entities and default GameObjects (game objects are tied by position to an entity).
        /// </summary>
        HybridEntity,

        /// <summary>
        /// Entities that combine DOTS entities and default GameObjects & child GameObjects that linked to parent i.e. wagons (game objects are tied by position to an entity).
        /// </summary>
        LinkedHybridEntity,

        /// <summary>
        /// Entities that combine DOTS entities and default GameObjects (game objects are tied by position to an entity) & registered in the RuntimeHybridEntityService.
        /// </summary>
        HybridRuntimeEntity
    }
}
