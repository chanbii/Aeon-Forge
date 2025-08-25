using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Tables : MonoBehaviour
{
    [Header("CSV TextAssets")]
    public TextAsset StageCsv;
    public TextAsset StageRewardCsv;
    public TextAsset StageCostCsv;

    // 원본
    public List<StageDto> Stages;
    public List<StageRewardDto> StageRewards;
    public List<StageCostDto> StageCosts;

    // 빠른 조회용 인덱스
    public Dictionary<string, StageDto> StageById { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<StageRewardDto>> RewardsByStageId { get; private set;} = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<StageCostDto>> CostsByStageId { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<StageDto>> StagesByArea { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Areas { get; private set; } = new();
    public string FirstAreaOrNull => Areas.Count > 0 ? Areas[0] : null;

    private void Awake()
    {
        // CSV -> DTO
        Stages = CsvParser.Parse<StageDto>(StageCsv);
        StageRewards = CsvParser.Parse<StageRewardDto>(StageRewardCsv);
        StageCosts = CsvParser.Parse<StageCostDto>(StageCostCsv);

        // 인덱싱
        StageById.Clear();
        foreach(var s in Stages)
        {
            if (string.IsNullOrWhiteSpace(s.StageID))
                continue;
            StageById[s.StageID] = s;
        }

        RewardsByStageId.Clear();
        foreach (var r in StageRewards)
        {
            if (string.IsNullOrWhiteSpace(r.StageID))
                continue;
            if(!RewardsByStageId.TryGetValue(r.StageID, out var list))
                list = RewardsByStageId[r.StageID] = new List<StageRewardDto>();
            list.Add(r);
        }

        CostsByStageId.Clear();
        foreach(var c in StageCosts)
        {
            if(string.IsNullOrWhiteSpace(c.StageID))
                continue;
            if (!CostsByStageId.TryGetValue(c.StageID, out var list))
                list = CostsByStageId[c.StageID] = new List<StageCostDto>();
            list.Add(c);
        }

        StagesByArea.Clear();
        foreach (var s in Stages)
        {
            var area = s.Area?.Trim();
            if (string.IsNullOrEmpty(area)) continue;

            if (!StagesByArea.TryGetValue(area, out var list))
                list = StagesByArea[area] = new List<StageDto>();
            list.Add(s);
        }

        Areas.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in Stages)
        {
            var area = s.Area?.Trim();
            if (string.IsNullOrEmpty(area)) continue;
            if (seen.Add(area)) Areas.Add(area);
        }

        // (디버그) 실제 키 확인
        foreach (var a in Areas)
            Debug.Log($"[Tables] Area='{a}', count={StagesByArea[a].Count}");


        /*
        // (디버그) 검증용 로그
        var known = new HashSet<string>(StageById.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var id in RewardsByStageId.Keys)
        {
            if (!known.Contains(id))
                Debug.LogWarning($"[Tables] Reward만 있고 Stage 없음: {id}");
        }
        foreach(var id in CostsByStageId.Keys)
        {
            if(!known.Contains(id))
                Debug.LogWarning($"[Tables] Cost만 있고 Stage 없음: {id}");
        }

        // (디버그) 성공시 로그
        Debug.Log($"[Tables] Stages={Stages.Count}, Rewards={StageRewards.Count}, Costs={StageCosts.Count}");
        */
    }

    public StageDto GetStage(string stageID)
        => StageById.TryGetValue(stageID, out var s) ? s : null;
    
    public IReadOnlyList<StageRewardDto> GetRewards(string stageID)
        => RewardsByStageId.TryGetValue(stageID, out var list) ? list : Array.Empty<StageRewardDto>();

    public IReadOnlyList<StageCostDto> GetCosts(string stageId)
        => CostsByStageId.TryGetValue(stageId, out var list) ? list : Array.Empty<StageCostDto>();

    public List<StageDto> GetStagesByArea(string area, int limit = 10)
    {
        if(!StagesByArea.TryGetValue(area, out var list) || list == null)
        {
            return new List<StageDto>();
        }
        if(limit <= 0 || list.Count <= limit)
            return new List<StageDto>(list);
        return list.GetRange(0, limit);
    }
}
