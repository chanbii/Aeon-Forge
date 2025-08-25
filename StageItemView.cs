using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class StageItemView : MonoBehaviour, IPointerClickHandler
{
    [Header("Refs")]
    [SerializeField] RectTransform header;
    [SerializeField] RectTransform details;
    [SerializeField] GameObject lockIcon;
    [Header("Texts")]
    [SerializeField] TMP_Text stageName, costTypeText, costValueText, moneyText, ticketText, dangerText;
    [Header("Layout")]
    [SerializeField] LayoutElement layout;
    [SerializeField] float headerHeight = 80f;
    [SerializeField] float extraPadding = 12f;
    [SerializeField] float detailsFixedHeight = -1f;

    public string StageID { get; private set; }
    public bool IsLocked { get; private set; }
    public bool IsExpanded { get; private set; }
    public event Action<StageItemView> OnExpandRequest;

    void Awake()
    {
        if (!layout)
            layout = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        layout.flexibleHeight = 0;
        layout.preferredHeight = headerHeight;

        var img = GetComponent<Image>();
        if (!img)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.001f);
        }
        img.raycastTarget = true;

        foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            t.raycastTarget = false;
        foreach (var i in GetComponentsInChildren<Image>(true))
            if (i.gameObject != gameObject)
                i.raycastTarget = false;

        if (details)
            details.gameObject.SetActive(false);
    }
    public void Bind(StageDto s, StageRewardDto r, StageCostDto c, bool isLocked)
    {
        StageID = s.StageID;

        stageName?.SetText(s.StageName ?? "");
        dangerText?.SetText(s.Danger.ToString());

        moneyText?.SetText(r != null ? r.RewardMoney.ToString() : "-");
        ticketText?.SetText(r != null ? r.RewardTicket.ToString() : "-");

        costTypeText?.SetText(c != null ? c.CostType : "-");
        costValueText?.SetText(c != null ? c.CostValue.ToString() : "-");

        SetLocked(isLocked);
        SetExpanded(false, true);
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        if (lockIcon) lockIcon.SetActive(locked);
        var cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        cg.alpha = locked ? 0.6f : 1f;
        if (locked && IsExpanded) SetExpanded(false, true);
    }

    public void OnPointerClick(PointerEventData _) { if (!IsLocked) OnExpandRequest?.Invoke(this); }

    public void SetExpanded(bool expand, bool immediate = false)
    {
        IsExpanded = expand;
        if (details) details.gameObject.SetActive(expand);

        layout.preferredHeight = expand ? CalcExpandedHeight() : headerHeight;

        var self = (RectTransform)transform;
        if (immediate)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(self);
            if (self.parent is RectTransform p) LayoutRebuilder.ForceRebuildLayoutImmediate(p);
        }
        else LayoutRebuilder.MarkLayoutForRebuild(self);
    }

    float CalcExpandedHeight()
    {
        if (!details) return headerHeight;

        if (detailsFixedHeight > 0f)
            return headerHeight + detailsFixedHeight;

        Canvas.ForceUpdateCanvases();
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(details);
        float detailsHeight = bounds.size.y; 
        return Mathf.Max(headerHeight, headerHeight + detailsHeight + extraPadding);
    }
}
