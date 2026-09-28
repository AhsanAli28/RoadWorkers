using UnityEngine;

[ExecuteAlways]
public class AspectRatioWithinAnchors : MonoBehaviour
{
    [Tooltip("Aspect Ratio = Width / Height")]
    [SerializeField]
    private float m_AspectRatio = 1f;
    [SerializeField]
    private RectTransform m_RectTransform;
    [SerializeField]
    private RectTransform m_ParentRectTransform;

    private void OnValidate()
    {
        if (m_RectTransform == null)
        {
            Debug.LogError($"RectTransform not set on {gameObject.name}");
        }
        if (m_ParentRectTransform == null)
        {
            Debug.LogError($"ParentRectTransform not set on {gameObject.name}");
        }
    }

    private void Update()
    {
        if (m_RectTransform == null || m_ParentRectTransform == null)
        {
            return;
        }

        // Calculate the size of the area defined by the anchors
        Vector2 anchorMin = m_RectTransform.anchorMin;
        Vector2 anchorMax = m_RectTransform.anchorMax;

        Vector2 parentSize = m_ParentRectTransform.rect.size;

        if (parentSize.x <= 0 || parentSize.y <= 0)
        {
            Debug.LogWarning($"ParentRectTransform {m_ParentRectTransform.name} has zero size! Skipping aspect ratio calculations.");
            return;
        }

        Vector2 areaSize = new Vector2(
            (anchorMax.x - anchorMin.x) * parentSize.x,
            (anchorMax.y - anchorMin.y) * parentSize.y
        );

        // Determine the maximum size while maintaining aspect ratio
        float areaAspectRatio = areaSize.x / areaSize.y;

        Vector2 newSize = Vector2.zero;

        if (areaAspectRatio > m_AspectRatio)
        {
            // The area is wider relative to the desired aspect ratio
            newSize.y = areaSize.y;
            newSize.x = newSize.y * m_AspectRatio;
        }
        else
        {
            // The area is taller relative to the desired aspect ratio
            newSize.x = areaSize.x;
            newSize.y = newSize.x / m_AspectRatio;
        }

        // Apply the new size to the RectTransform
        m_RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newSize.x);
        m_RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newSize.y);

        // Optional: Center the UI element within the anchor area
        m_RectTransform.anchoredPosition = Vector2.zero;
    }
}
