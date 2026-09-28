using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonSounds : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] bool onHover = true;
    [SerializeField] bool onClick = true;
    [SerializeField] Button button;

    [Header("Overrides")]
    [SerializeField] AudioClip onClickOverride;
    [SerializeField] AudioClip onHoverOverride;

    private void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(ClickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!onHover)
            return;

        if (onHoverOverride)
            AudioManager.Instance?.PlaySFX(onHoverOverride);
        else
            AudioManager.Instance?.Play_MenuSelection();
    }

    public void ClickSound()
    {
        if (!onClick)
            return;

        if (onClickOverride)
            AudioManager.Instance?.PlaySFX(onClickOverride);
        else
            AudioManager.Instance?.Play_ButtonPress();

    }
}
