namespace Spirit604
{
    public static class ProjectConstants
    {
        /// <summary>
        /// Helper rate value to convert kilometer per hour to meter per second speed.
        /// </summary>
        public const float KmhToMs_RATE = 3.6f;

        /// <summary>
        /// Default lane speed in km/h.
        /// </summary>
        public const float DefaultLaneSpeed = 60f;

        /// <summary>
        /// Global direction of all lanes.
        /// 1 right-hand; -1 left-hand direction.
        /// Changing this parameter will only affect newly created scenes. 
        /// Old scenes with a different lane direction will not work.
        /// </summary>
        public const int LaneHandDirection = 1;

        /// <summary>Index of the physics world when physics is disabled for the entity. </summary>
        public const int NoPhysicsWorldIndex = 10;

        #region Layers

        // Road object layers

        /// <summary>TrafficNode layer. </summary>
        public const int TRAFFIC_NODE_LAYER_INDEX = 20;

        /// <summary>PedestrianNode layer. </summary>
        public const int PEDESTRIAN_NODE_LAYER_INDEX = 21;

        public const string TRAFFIC_NODE_LAYER_NAME = "TrafficNode";
        public const string PEDESTRIAN_NODE_LAYER_NAME = "PedestrianNode";

        // Object layers

        /// <summary>Ground surface layer. </summary>
        public const int GroundLayer = 18;

        /// <summary>Ragdoll collider layer. </summary>
        public const int RagdollLayer = 19;

        /// <summary>Static object collider layer (e.g. house, fence). </summary>
        public const int PhysicsShapeLayer = 22;

        /// <summary>Layer of streaming 3D chunks in the chunk subscenes. </summary>
        public const int ChunkLayer = 23;

        /// <summary>Collider layer of props (e.g. traffic light, hydrant, mailbox). </summary>
        public const int PropsLayer = 24;

        /// <summary>Layer of combined meshes into the one by 3rd party combining tool. </summary>
        public const int CombinedLayer = 25;

        public const string GROUND_NAME = "Ground";
        public const string RAGDOLL_NAME = "Ragdoll";
        public const string PHYSICS_SHAPE_NAME = "StaticPhysicsShape";
        public const string CHUNK_NAME = "Chunk";
        public const string PROPS_LAYER_NAME = "Props";
        public const string COMBINED_NAME = "Combined";

        // Misc layers

        /// <summary>Layer of the player's car when using Hybrid Mono for the player. </summary>
        public const int SYNC_PLAYER_LAYER_VALUE = 13;

        /// <summary>Player car layer. </summary>
        public const int PLAYER_LAYER_VALUE = 14;

        /// <summary>Police car layer. </summary>
        public const int POLICE_LAYER_VALUE = 15;

        /// <summary>Traffic car layer. </summary>
        public const int DEFAULT_TRAFFIC_LAYER_VALUE = 16;

        /// <summary>Player & custom NPCs layer. </summary>
        public const int NPC_LAYER_VALUE = 17;

        /// <summary>Collider trigger player layer. </summary>
        public const int TRIGGER_LAYER_VALUE = 26;

        #endregion
    }
}