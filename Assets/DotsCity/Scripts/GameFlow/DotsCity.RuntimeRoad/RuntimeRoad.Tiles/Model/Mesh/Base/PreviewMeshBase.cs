using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public abstract class PreviewMeshBase : IRoadTile
    {
        private Color32 initialColor;

        public virtual Vector3 Position { get; set; }

        public virtual Quaternion Rotation { get; set; }

        public virtual float TileSize { get; set; }

        public virtual RecalculationType RecalculationType { get; set; }

        public virtual Vector2Int CellSize { get; set; }

        public virtual ConnectDirection[] ConnectionFlagArray { get; set; }

        public virtual bool IsEnabled { get; set; } = true;

        public virtual bool IsVisible { get; set; } = true;

        public virtual Color AvailableColor { get; set; } = Color.white;

        public virtual Color NotAvailableColor { get; set; } = Color.red;

        public virtual bool IsRendered => IsVisible;

        public Vector2Int CurrentCellSize => !IsRotated ? CellSize : new Vector2Int(CellSize.y, CellSize.x);

        public ConnectDirection CurrentFlags
        {
            get
            {
                var flags = ConnectDirection.None;

                for (int i = 0; i < ConnectionFlagArray?.Length; i++)
                {
                    flags |= ConnectionFlagArray[i];
                }

                return flags;
            }
        }

        public bool IsRotated => Mathf.RoundToInt(Rotation.eulerAngles.y) % 180 != 0;

        public Vector2Int LeftBottom => CellUtils.GetLeftBottomCellFromObjectPosition(Position, CurrentCellSize, TileSize);

        public RecalculationType Type => RecalculationType;

        public int Page { get; set; }

        protected abstract int CurrentColorID { get; }

        public virtual void SetMaterial(Material mat)
        {
            SetMaterialInternal(mat);
            initialColor = mat.GetColor(CurrentColorID);
        }

        public virtual void SwitchAvailableState(bool available)
        {
            if (IsEnabled == available) return;

            IsEnabled = available;

            Color32 color = available ? AvailableColor : NotAvailableColor;

            color.a = initialColor.a;

            SetColorInternal(color);
        }

        public virtual void SwitchVisibleState(bool isVisible)
        {
            IsVisible = isVisible;
        }

        public void Rotate(Vector3 eulerRotation)
        {
            Rotation = Rotation * Quaternion.Euler(eulerRotation);
        }

        public void Rotate(Quaternion rotation)
        {
            Rotation = Rotation * rotation;
        }

        public void SetY(float value)
        {
            Position = new Vector3(Position.x, value, Position.z);
        }

        protected virtual void SetMaterialInternal(Material mat) { }

        protected virtual void SetColorInternal(Color32 color) { }
    }
}