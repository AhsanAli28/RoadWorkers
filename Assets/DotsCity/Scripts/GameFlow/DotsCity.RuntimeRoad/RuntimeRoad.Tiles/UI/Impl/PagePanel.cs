using Spirit604.Extensions;
using System;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    [DisallowMultipleComponent]
    public class PagePanel : MonoBehaviour
    {
        [SerializeField] private PageButtonView pagePrefab;
        [SerializeField] private Transform pageParent;

        public event Action<int> OnPageClicked = delegate { };

        public void Init(int pageCount)
        {
            Clear();

            for (int i = 0; i < pageCount; i++)
            {
                var pageIndex = i;
                var page = Instantiate(pagePrefab, pageParent);

                page.Init(pageIndex);
                page.OnClicked += PageButtonView_OnClicked;
            }
        }

        private void Clear()
        {
            TransformExtensions.ClearChilds(pageParent);
        }

        private void PageButtonView_OnClicked(PageButtonView obj)
        {
            OnPageClicked(obj.Page);
        }
    }
}
