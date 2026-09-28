using Spirit604.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Spirit604.DotsCity.TestScene.UI
{
    public class VehicleSpawnerViewAdapter : MonoBehaviour
    {
        [SerializeField]
        private VehicleCustomSpawner vehicleCustomSpawner;

        [SerializeField]
        private Button switchVehicleButton;

        [SerializeField]
        private Button switchPointButton;

        private void Awake()
        {
            switchVehicleButton.onClick.AddListener(() => vehicleCustomSpawner.SpawnNextVehicle());
            switchPointButton.onClick.AddListener(() => vehicleCustomSpawner.SpawnNext());
            vehicleCustomSpawner.OnSpawned += VehicleCustomSpawner_OnSpawned;
        }

        private void Update()
        {
            if (InputExtension.GetKeyDown(KeyCode.Tab))
            {
                vehicleCustomSpawner.SpawnNext();
            }

            if (InputExtension.GetKeyDown(KeyCode.F1))
            {
                vehicleCustomSpawner.SpawnNextVehicle();
            }

            if (InputExtension.GetKeyDown(KeyCode.Backspace))
            {
                vehicleCustomSpawner.ResetPosition();
            }
        }

        private void VehicleCustomSpawner_OnSpawned(bool sceneVehicle)
        {
            if (sceneVehicle)
            {
                switchVehicleButton.gameObject.SetActive(false);
            }
        }
    }
}
