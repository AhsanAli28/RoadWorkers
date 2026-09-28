using UnityEngine;
using TMPro;
using DG.Tweening;

public class IncomePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private float floatDistance = 50f;
    [SerializeField] private float duration = 3f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void ShowPopup(int amount)
    {
        amountText.text = $"+${amount.ToString("N0")}";
        RectTransform rectTransform = GetComponent<RectTransform>();

        Vector3 endPosition = rectTransform.anchoredPosition + new Vector2(0, floatDistance);

        float delayBeforeFade = 2f; // seconds to wait before fading
        float fadeDuration = duration; // same or different than move duration

        Sequence sequence = DOTween.Sequence();
        sequence.Append(rectTransform.DOAnchorPos(endPosition, duration).SetEase(Ease.OutCubic));
        sequence.Insert(delayBeforeFade, canvasGroup.DOFade(0.1f, fadeDuration));
        sequence.OnComplete(() => Destroy(gameObject));

        // Use unscaled delta time
        sequence.SetUpdate(true);
    }
}
