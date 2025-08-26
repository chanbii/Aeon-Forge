using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class StageItemView : RecyclableScrollSlot<StageDto>
{
    [Header("Refs")]
    [SerializeField] RectTransform header;
    [SerializeField] RectTransform details;
    [SerializeField] GameObject lockIcon;
    [Header("Texts")]
    [SerializeField] TMP_Text stageName, costTypeText, costValueText, moneyText, ticketText, dangerText;
    [Header("Layout")]
    [SerializeField] LayoutElement layout;

    public override void Init()
    {
        
    }

    public override void UpdateSlot(StageDto data)
    {
        stageName.SetText(data.StageName);
        costTypeText.SetText(data.Cost.CostType);
        costValueText.SetText(data.Cost.CostValue.ToString());
        moneyText.SetText(data.Reward.RewardMoney.ToString());
        ticketText.SetText(data.Reward.RewardTicket.ToString());
        switch (data.Danger)
        {
            case DangerLevel.Low:
                dangerText.SetText("주의");
                break;
            case DangerLevel.Medium:
                dangerText.SetText("경계");
                break;
            case DangerLevel.High:
                dangerText.SetText("심각");
                break;
            default:
                break;
        }
    }
}