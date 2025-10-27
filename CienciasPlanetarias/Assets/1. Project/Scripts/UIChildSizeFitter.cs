using UnityEngine;

public class UIChildSizeFitter : MonoBehaviour
{
    public RectTransform rectTransform;
    public RectTransform childRectTransform;
    public Vector2 offset;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (transform.childCount > 0)
        {
            childRectTransform = transform.GetChild(0).GetComponent<RectTransform>();
        }
    }

    void OnValidate()
    {
        UpdateSize();
    }

    void Update()
    {
        UpdateSize();
    }
    
    private void UpdateSize()
    {
        if (childRectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(childRectTransform.rect.width + offset.x, childRectTransform.rect.height + offset.y);
        }
    }
}
