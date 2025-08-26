using UnityEngine;

public abstract class RecyclableScrollSlot<T> : MonoBehaviour
{
    [SerializeField] protected RectTransform rectTransform;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }
    }

    public float Height => RectTransform.rect.height;
    public float Width => RectTransform.rect.width;

    protected virtual void Awake()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }

    protected virtual void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
    }
#endif

    public abstract void Init();
    public abstract void UpdateSlot(T data);
}
