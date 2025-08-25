using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageListController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Tables tables;
    [SerializeField] private Transform content;
    [SerializeField] private StageItemView itemPrefab;

    [Header("Options")]
    [SerializeField] private int maxItems = 10;
    [SerializeField] bool autoSelectOnEnable = true;
    [SerializeField] UnityEngine.UI.Button[] areaButtons;

    [Header("Progress")]
    [Tooltip("SQLite의 stage_progress 사용")]
    [SerializeField] private bool useDatabaseProgress = true;

    private readonly List<StageItemView> pool = new();
    private StageItemView currentOpen;
    private string currentArea;

    void Start()
    {
        if (!tables || !content || !itemPrefab)
        {
            Debug.LogError("[StageList] Inspector 할당 누락");
            return;
        }

        BindAreaButtons();

        if (autoSelectOnEnable && tables.Areas.Count > 0)
            ShowArea(tables.FirstAreaOrNull);
    }

    void BindAreaButtons()
    {
        if (areaButtons == null) return;

        for (int i = 0; i < areaButtons.Length; i++)
        {
            var btn = areaButtons[i];
            if (!btn) continue;

            if (i < tables.Areas.Count)
            {
                string area = tables.Areas[i];
                btn.gameObject.SetActive(true);

                var tmp = btn.GetComponentInChildren<TMPro.TMP_Text>();
                if (tmp) tmp.text = area;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => ShowArea(area));
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    // 구역 클릭
    public void ShowArea(string areaName)
    {
        if (string.IsNullOrEmpty(areaName)) return;
        currentArea = areaName;

        var stages = tables.GetStagesByArea(areaName, maxItems) ?? new List<StageDto>();
        Debug.Log($"[StageList] ShowArea '{areaName}' -> {stages.Count}");

        EnsureAreaHasFirstUnlocked(stages);
        Rebind(stages);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
    }

    public void OnAreaButtonClickedByIndex(int index)
    {
        if (tables == null || index < 0 || index >= tables.Areas.Count) return;
        ShowArea(tables.Areas[index]);
    }

    private void Rebind(List<StageDto> stages)
    {
        while (pool.Count < stages.Count)
        {
            var v = Instantiate(itemPrefab, content);
            v.OnExpandRequest += HandleExpandRequest;
            pool.Add(v);
        }

        StageItemView firstUnlocked = null;

        for (int i = 0; i < pool.Count; i++)
        {
            bool active = i < stages.Count;
            var view = pool[i];
            view.gameObject.SetActive(active);
            if (!active) continue;

            var s = stages[i];

            var rewardsList = tables.GetRewards(s.StageID);
            var costsList = tables.GetCosts(s.StageID);

            StageRewardDto r = (rewardsList != null && rewardsList.Count > 0) ? rewardsList[0] : null;
            StageCostDto c = (costsList != null && costsList.Count > 0) ? costsList[0] : null;

            bool isLocked = IsStageLocked(s.StageID);

            view.Bind(s, r, c, isLocked);    
            view.SetExpanded(false, immediate: true);

            if (!isLocked && firstUnlocked == null)
                firstUnlocked = view;
        }

        currentOpen = null;
        if (autoSelectOnEnable && firstUnlocked != null)
        {
            firstUnlocked.SetExpanded(true, immediate: true);
            currentOpen = firstUnlocked;
        }
    }

    private void HandleExpandRequest(StageItemView requester)
    {
        if (requester == null) return;

        if (currentOpen == requester)
        {
            requester.SetExpanded(false);
            currentOpen = null;
            return;
        }

        if (currentOpen != null)
            currentOpen.SetExpanded(false);

        requester.SetExpanded(true);
        currentOpen = requester;
    }

    // 진행도 관리
    private void EnsureAreaHasFirstUnlocked(List<StageDto> stages)
    {
        if (stages == null || stages.Count == 0) return;

        if (useDatabaseProgress)
        {
            DatabaseService.Instance.EnsureProgressRows(EnumerateIds(stages));

            // 첫 번째 자동 해제
            bool anyUnlocked = false;
            foreach (var s in stages)
                if (DatabaseService.Instance.IsUnlocked(s.StageID)) 
                { 
                    anyUnlocked = true; break; 
                }

            if (!anyUnlocked)
                DatabaseService.Instance.Unlock(stages[0].StageID);
        }
        else
        {
            // 메모리 관리
        }
    }

    private bool IsStageLocked(string stageId)
    {
        if (!useDatabaseProgress) return false;
        return !DatabaseService.Instance.IsUnlocked(stageId);
    }

    // 스테이지 클리어
    public void OnStageCleared(string stageId)
    {
        if (string.IsNullOrEmpty(stageId)) return;

        var cur = tables.GetStage(stageId);
        if (cur == null) return;

        var list = tables.GetStagesByArea(cur.Area, 0); 
        int idx = list.FindIndex(x => string.Equals(x.StageID, stageId, StringComparison.OrdinalIgnoreCase));

        if (useDatabaseProgress)
            DatabaseService.Instance.MarkCleared(stageId);

        // 다음 스테이지 언락
        if (idx >= 0 && idx + 1 < list.Count)
        {
            var next = list[idx + 1];
            if (useDatabaseProgress)
                DatabaseService.Instance.Unlock(next.StageID);
        }

        // 현재 구역 갱신
        if (!string.IsNullOrEmpty(currentArea))
            ShowArea(currentArea);
    }

    private IEnumerable<string> EnumerateIds(List<StageDto> list)
    {
        foreach (var s in list)
            if (!string.IsNullOrEmpty(s.StageID))
                yield return s.StageID;
    }

    private void OnDestroy()
    {
        foreach (var v in pool)
        {
            if (v != null) v.OnExpandRequest -= HandleExpandRequest;
        }
    }

}
