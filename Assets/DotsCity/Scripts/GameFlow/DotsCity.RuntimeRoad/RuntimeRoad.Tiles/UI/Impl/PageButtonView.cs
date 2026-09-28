using TMPro;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class PageButtonView : ViewButtonElementBase<PageButtonView>
    {
        [SerializeField] private TextMeshProUGUI indexText;

        public int Page { get; set; }

        public void Init(int page, Sprite sprite = null)
        {
            if (indexText)
            {
                indexText.text = page.ToString();
            }

            Page = page;
            Init(sprite);
        }
    }
}
