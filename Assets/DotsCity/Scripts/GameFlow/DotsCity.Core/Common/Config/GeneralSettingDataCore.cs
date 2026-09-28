using Spirit604.Attributes;
using Unity.Physics;
using UnityEngine;

namespace Spirit604.DotsCity.Core
{
    public class GeneralSettingDataCore : ScriptableObject
    {
        #region Other variables

        [HideInView]
        [Tooltip(
            "<b>DOTS</b> - simulation of traffic & pedestrians entirely in DOTS space\r\n\r\n" +
            "<b>Hybrid mono</b> - physics simulation run on Monobehaviour scripts, but input taken from DOTS entities simulation"
        )]
        [SerializeField] private WorldSimulationType worldSimulationType = WorldSimulationType.DOTS;

        [ShowIf(nameof(DOTSSimulation))]
        [SerializeField] private SimulationType physicsSimulationType = SimulationType.UnityPhysics;

        [Tooltip("On/off culling of the physics of dynamic objects that are far from the player")]
        [ShowIf(nameof(DOTSSimulation))]
        [SerializeField] private bool cullPhysics = true;

        [Tooltip("On/off culling of the physics of static objects that are far from the player")]
        [ShowIf(nameof(DOTSSimulation))]
        [SerializeField] private bool cullStaticPhysics = true;

        [Tooltip("Force enable built-in physics, otherwise built-in physics will be disabled when ragdoll is disabled")]
        [ShowIf(nameof(DOTSSimulation))]
        [SerializeField] private bool forceLegacyPhysics = false;

        #endregion

        public WorldSimulationType WorldSimulationType { get => worldSimulationType; set => worldSimulationType = value; }
        public SimulationType SimulationType { get => DOTSSimulation ? physicsSimulationType : SimulationType.NoPhysics; set => physicsSimulationType = value; }
        public bool CullPhysics { get => cullPhysics && DOTSSimulation; set => cullPhysics = value; }
        public bool CullStaticPhysics { get => cullStaticPhysics && DOTSSimulation; set => cullStaticPhysics = value; }
        public bool ForceLegacyPhysics { get => forceLegacyPhysics; set => forceLegacyPhysics = value; }

        public bool DOTSSimulation => WorldSimulationType == WorldSimulationType.DOTS;
        public bool DOTSPhysics => SimulationType == SimulationType.UnityPhysics;
        public bool PcPlatform => !Application.isMobilePlatform && !Application.isEditor;
    }
}