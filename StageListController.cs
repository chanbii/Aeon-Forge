using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageListController : MonoBehaviour
{
    [SerializeField] private Tables tables;
    [SerializeField] private Transform content;
    [SerializeField] private StageItemView itemPrefab;   // 오타 수정
    [SerializeField] private int maxItems = 10;

    private readonly List<StageItemView> pool = new();
    private StageItemView currentOpen;

    // 구역 클릭
    public void ShowArea(string areaName)
    {
        if (tables == null || content == null || itemPrefab == null)
        {
            Debug.LogError("[StageList] Inspector 할당 누락(tables/content/itemPrefab).");
            return;
        }

        // maxItems 적용
        var stages = tables.GetStagesByArea(areaName, maxItems);
        Rebind(stages);
    }

    private void Rebind(List<StageDto> stages)
    {
        if (stages == null) stages = new List<StageDto>();

        // 풀 확장
        while (pool.Count < stages.Count)
        {
            var v = Instantiate(itemPrefab, content);
            v.OnExpandRequest += HandleExpandRequest;
            pool.Add(v);
        }

        // 바인딩 & 가시성
        for (int i = 0; i < pool.Count; i++)
        {
            bool active = i < stages.Count;
            var view = pool[i];
            view.gameObject.SetActive(active);
            if (!active) continue;

            var s = stages[i];

            // 보상/비용 1회 조회
            var rewards = tables.GetRewards(s.StageID);
            var costs = tables.GetCosts(s.StageID);

            view.Bind(s, rewards, costs);

            // 모두 접힌 상태로 시작 (즉시 레이아웃 반영)
            view.SetExpanded(false, immediate: true);
        }

        // 현재 열려있는 항목 초기화
        currentOpen = null;

        // 레이아웃 안정화
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
    }

    private void HandleExpandRequest(StageItemView requester)
    {
        if (requester == null) return;

        // 같은 아이템 다시 눌렀을 때 '접기'를 원하면 아래 블록 사용
        if (currentOpen == requester)
        {
            requester.SetExpanded(false);
            currentOpen = null;
            return;
        }

        // 다르면 기존 열린 것 닫고, 새로 연다
        if (currentOpen != null)
            currentOpen.SetExpanded(false);

        requester.SetExpanded(true);
        currentOpen = requester;
    }

    // (선택) 파괴 시 이벤트 해제
    private void OnDestroy()
    {
        foreach (var v in pool)
        {
            if (v != null) v.OnExpandRequest -= HandleExpandRequest;
        }
    }

}
