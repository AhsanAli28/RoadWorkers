using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public abstract class TileGridViewBase<T> : ViewButtonElementBase<T> where T : TileGridViewBase<T>
    {
        public int ID { get; private set; }

        public void Initialize(int id, Sprite icon = null)
        {
            this.ID = id;

            Init(icon);
        }
    }
}
