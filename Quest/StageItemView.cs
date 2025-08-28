using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class StageItemView : RecyclableScrollSlot<StageDto>, IPointerClickHandler
{
    [Header("Refs")]
    [SerializeField] RectTransform header;
    [SerializeField] Button headerButton;
    [SerializeField] RectTransform details;
    [SerializeField] GameObject lockIcon;

    [Header("Texts")]
    [SerializeField] TMP_Text stageName, costTypeText, costValueText, moneyText, ticketText, dangerText;

    [Header("Layout")]
    [SerializeField] LayoutElement layout;

    public int BoundIndex { get; private set; }
    private System.Action<int, StageItemView> onRequestExpand;
    private bool expanded;
    private bool canOpen;
    private CanvasGroup detailsCg;   

    public bool Expanded => expanded;

    public override void Init()
    {
        if (details) details.gameObject.SetActive(false);

        if (!headerButton && header) headerButton = header.GetComponent<Button>();
        if (headerButton)
        {
            headerButton.onClick.RemoveAllListeners();
            headerButton.onClick.AddListener(() =>
            {
                if (!canOpen) return;
                onRequestExpand?.Invoke(BoundIndex, this);
            });
        }

        var img = GetComponent<Image>();
        if (!img) { img = gameObject.AddComponent<Image>(); img.color = new Color(0, 0, 0, 0); }
        img.raycastTarget = true;
    }

    public override void UpdateSlot(StageDto data)
    {
        var name = string.IsNullOrWhiteSpace(data.StageName) ? "(이름없음)" : data.StageName;
        stageName.SetText(name);
        costTypeText.SetText(data.Cost.CostType);
        costValueText.SetText(data.Cost.CostValue.ToString());
        moneyText.SetText(data.Reward.RewardMoney.ToString());
        ticketText.SetText(data.Reward.RewardTicket.ToString());

        switch (data.Danger)
        {
            case DangerLevel.Low: dangerText.SetText("주의"); break;
            case DangerLevel.Medium: dangerText.SetText("경계"); break;
            case DangerLevel.High: dangerText.SetText("심각"); break;
        }
    }

    public void Bind(int index, bool expanded, System.Action<int, StageItemView> onRequestExpand)
    {
        BoundIndex = index;
        this.onRequestExpand = onRequestExpand;
        SetExpanded(expanded, silent: true);
    }

    public void SetUnlockState(bool isUnlocked)
    {
        canOpen = isUnlocked;
        if (lockIcon) lockIcon.SetActive(!canOpen);
        if (headerButton) headerButton.interactable = canOpen;
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (!canOpen) return;
        onRequestExpand?.Invoke(BoundIndex, this);
    }

    public void SetExpanded(bool value, bool silent)
    {
        expanded = value;
        if (details) details.gameObject.SetActive(expanded);
        if (!silent) Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
    }

    // ---------- 애니메이션/측정 지원 ----------

    public void EnsureDetailsVisibleForAnim()
    {
        if (!details) return;
        if (!details.gameObject.activeSelf) details.gameObject.SetActive(true);

        if (!detailsCg)
        {
            detailsCg = details.GetComponent<CanvasGroup>();
            if (!detailsCg) detailsCg = details.gameObject.AddComponent<CanvasGroup>();
        }
        detailsCg.alpha = 0f; 
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);
    }

    public void SetDetailsAlpha(float a)
    {
        if (!details) return;
        if (!detailsCg)
        {
            detailsCg = details.GetComponent<CanvasGroup>();
            if (!detailsCg) detailsCg = details.gameObject.AddComponent<CanvasGroup>();
        }
        detailsCg.alpha = Mathf.Clamp01(a);
    }

    public void HideDetailsAfterAnim()
    {
        if (!expanded && details) details.gameObject.SetActive(false);
    }

    public float MeasurePreferredHeight()
    {
        foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            t.ForceMeshUpdate();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform);

        float hHeader = header ? Mathf.Max(header.rect.height, LayoutUtility.GetPreferredHeight(header)) : 0f;
        float hDetails = details ? Mathf.Max(details.rect.height, LayoutUtility.GetPreferredHeight(details)) : 0f;

        float h = hHeader + hDetails;
        if (layout && layout.preferredHeight > 0f) h = Mathf.Max(h, layout.preferredHeight);
        return Mathf.Max(h, 40f);
    }
}
