using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class GridSceneView : GridSceneViewBase
    {
        private const string ColorParam = "_Color";

        private readonly int ColorID = Shader.PropertyToID(ColorParam);

        [SerializeField] private Transform gridPrefab;
        [SerializeField] private Transform removeCellPrefab;

        private Transform grid;
        private GameObject removeCellObject;
        private PlacingType placingType;

        private void Awake()
        {
            grid = Instantiate(gridPrefab, transform);
            grid.gameObject.SetActive(false);
            removeCellObject = Instantiate(removeCellPrefab, transform).gameObject;
            removeCellObject.gameObject.SetActive(false);
        }

        public override void SetPlacingType(PlacingType newPlacingType)
        {
            switch (placingType)
            {
                case PlacingType.None:
                    break;
                case PlacingType.Add:
                    //UnselectTile();
                    grid.gameObject.SetActive(false);
                    break;
                case PlacingType.Remove:
                    removeCellObject.gameObject.SetActive(false);
                    grid.gameObject.SetActive(false);
                    break;
            }

            placingType = newPlacingType;

            switch (placingType)
            {
                case PlacingType.None:
                    break;
                case PlacingType.Add:
                    //UnselectTile();
                    grid.gameObject.SetActive(true);
                    break;
                case PlacingType.Remove:
                    removeCellObject.gameObject.SetActive(true);
                    grid.gameObject.SetActive(true);
                    break;
            }
        }

        public override void SetPosition(Vector3 pos)
        {
            switch (placingType)
            {
                case PlacingType.None:
                    break;
                case PlacingType.Add:
                    grid.transform.position = pos;
                    break;
                case PlacingType.Remove:
                    grid.transform.position = pos;
                    removeCellObject.transform.position = pos;
                    break;
            }
        }

        public override void SetRemoveColor(bool isOverllaped)
        {
            var meshRenderer = removeCellObject.GetComponent<MeshRenderer>();

            if (isOverllaped)
            {
                meshRenderer.sharedMaterial.SetColor(ColorID, Color.red);
            }
            else
            {
                meshRenderer.sharedMaterial.SetColor(ColorID, Color.white);
            }
        }
    }
}
