using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class StageItemView : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] GameObject collapsedRoot;
    [SerializeField] GameObject expandedRoot;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleCollapsed; // 접힘용 타이틀
    [SerializeField] private TMP_Text titleExpanded;  // 펼침용 타이틀
    [SerializeField] TMP_Text dangerText;
    [SerializeField] TMP_Text rewardText;
    [SerializeField] TMP_Text costText;

    public bool IsExpanded {  get; private set; }

    public event Action<StageItemView> OnExpandRequest;

    private void Awake()
    {
        SetExpanded(false, immediate: true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnExpandRequest?.Invoke(this);
    }

    // 데이터 바인딩
    public void Bind(StageDto stage, IReadOnlyList<StageRewardDto> rewards, IReadOnlyList<StageCostDto> costs)
    {
        var title = $"{stage.StageName}";
        if (titleCollapsed) titleCollapsed.text = title;
        if (titleExpanded) titleExpanded.text = title;
        if (dangerText) dangerText.text = stage.Danger.ToString();

        if (rewardText) rewardText.text = FormatRewards(rewards);
        if (costText) costText.text = FormatCosts(costs);

        SetExpanded(false, immediate: true); // 풀 재사용 대비 기본 닫힘
    }

    public void SetExpanded(bool on, bool immediate = false)
    {
        IsExpanded = on;
        if (collapsedRoot) collapsedRoot.SetActive(!on);
        if (expandedRoot) expandedRoot.SetActive(on);

        // 레이아웃 즉시 갱신(스크롤 튐 방지)
        var self = (RectTransform)transform;
        if (immediate)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(self);
            if (self.parent is RectTransform p) LayoutRebuilder.ForceRebuildLayoutImmediate(p);
        }
        else
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(self);
            if (self.parent is RectTransform p2) LayoutRebuilder.ForceRebuildLayoutImmediate(p2);
        }
    }

    // ---- 표시용 요약 포맷 ----
    static string FormatRewards(IReadOnlyList<StageRewardDto> rs)
    {
        if (rs == null || rs.Count == 0) return "-";

        var parts = new List<string>();
        foreach (var r in rs)
        {
            if (r.RewardMoney != 0)
            {
                parts.Add($"{r.RewardMoney}");
            }
        }

        return parts.Count == 0 ? "-" : string.Join(" / ", parts);
    }

    static string FormatCosts(IReadOnlyList<StageCostDto> cs)
    {
        if (cs == null || cs.Count == 0) return "-";

        var parts = new List<string>();
        foreach (var c in cs)
        {
            parts.Add($"{c.CostType} x{c.CostValue}");
        }
        return string.Join(" / ", parts);
    }
}
