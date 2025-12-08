using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor.PackageManager;
using UnityEngine;

public class Tables : MonoBehaviour
{
    private static Tables _instance;

    public static Tables Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindAnyObjectByType<Tables>();

                if(_instance == null) {
                    GameObject go = new GameObject(nameof(Tables));
                    _instance = go.AddComponent<Tables>();
                }

                DontDestroyOnLoad(_instance.gameObject);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAndIndexData();
        }
        else if(_instance != this) {
            Destroy(gameObject);
            return;
        }
    }

    private void LoadAndIndexData()
    {
        // CSV -> DTO
        Stages = CsvParser.Parse<StageDto>(StageCsv.text);
        StageRewards = CsvParser.Parse<StageRewardDto>(StageRewardCsv.text);
        StageCosts = CsvParser.Parse<StageCostDto>(StageCostCsv.text);
        Events = CsvParser.Parse<EventDto>(EventCsv.text);
        Penalties = CsvParser.Parse<PenaltyDto>(PenaltyCsv.text);
        AttackCharacters.Clear();
        SupportCharacters.Clear();
        TechCharacters.Clear();
        CharacterById.Clear();

        LoadAndClassifyCharacters(AttackCharacterCsv);
        LoadAndClassifyCharacters(SupportCharacterCsv);
        LoadAndClassifyCharacters(TechCharacterCsv);

        // 인덱싱
        StageById.Clear();
        foreach (var s in Stages)
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
            if (!RewardsByStageId.TryGetValue(r.StageID, out var list))
                list = RewardsByStageId[r.StageID] = new List<StageRewardDto>();
            list.Add(r);
        }

        CostsByStageId.Clear();
        foreach (var c in StageCosts)
        {
            if (string.IsNullOrWhiteSpace(c.StageID))
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

        RewardsByStageId.Clear();
        foreach (var r in StageRewards)
        {
            if (string.IsNullOrWhiteSpace(r.StageID))
                continue;
            if (!RewardsByStageId.TryGetValue(r.StageID, out var list))
                list = RewardsByStageId[r.StageID] = new List<StageRewardDto>();
            list.Add(r);
        }

        CostsByStageId.Clear();
        foreach (var c in StageCosts)
        {
            if (string.IsNullOrWhiteSpace(c.StageID))
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
        seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in Stages)
        {
            var area = s.Area?.Trim();
            if (string.IsNullOrEmpty(area)) continue;
            if (seen.Add(area)) Areas.Add(area);
        }

        StageById.Clear();
        foreach (var s in Stages)
        {
            if (string.IsNullOrWhiteSpace(s.StageID)) continue;
            StageById[s.StageID] = s;
        }

        EventById.Clear();
        foreach (var e in Events)
        {
            if (string.IsNullOrWhiteSpace(e.EventID)) continue;
            EventById[e.EventID] = e;
        }

        PenaltyById.Clear();
        foreach (var p in Penalties)
        {
            if (string.IsNullOrWhiteSpace(p.PenaltyID)) continue;
            PenaltyById[p.PenaltyID] = p;
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

    private void LoadAndClassifyCharacters(TextAsset csvAsset)
    {
        if(csvAsset == null) return;

        var characters = CsvParser.Parse<CharacterDto>(csvAsset.text);

        foreach (var character in characters)
        {
            if (string.IsNullOrWhiteSpace(character.CharID)) continue;

            // ID가 A(attack), S(support), T(tech)인지 확인
            char prefix = character.CharID[0];

            List<CharacterDto> targetList = null;
            switch(prefix)
            {
                case 'A':
                    targetList = AttackCharacters;
                    break;
                case 'S':
                    targetList = SupportCharacters;
                    break;
                case 'T':
                    targetList = TechCharacters;
                    break;
                default:
                    Debug.LogWarning($"[Tables] Unknown Character ID prefix: {character.CharID}");
                    break;
            }

            if (targetList != null)
            {
                targetList.Add(character);
            }

            if(CharacterById.ContainsKey(character.CharID)) {
                Debug.LogError($"[Tables] Duplicate Character ID: {character.CharID}. Skip.");
                continue;
            }
            CharacterById[character.CharID] = character;
        }
    }

    [Header("CSV TextAssets")]
    public TextAsset StageCsv;
    public TextAsset StageRewardCsv;
    public TextAsset StageCostCsv;
    public TextAsset EventCsv;
    public TextAsset PenaltyCsv;
    public TextAsset AttackCharacterCsv;
    public TextAsset SupportCharacterCsv; 
    public TextAsset TechCharacterCsv;

    // 원본
    public List<StageDto> Stages;
    public List<StageRewardDto> StageRewards;
    public List<StageCostDto> StageCosts;
    public List<EventDto> Events;
    public List<PenaltyDto> Penalties;
    public List<CharacterDto> AttackCharacters { get; private set; } = new();
    public List<CharacterDto> SupportCharacters { get; private set; } = new();
    public List<CharacterDto> TechCharacters { get; private set; } = new();
    // 빠른 조회용 인덱스
    public Dictionary<string, StageDto> StageById { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<StageRewardDto>> RewardsByStageId { get; private set;} = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<StageCostDto>> CostsByStageId { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<StageDto>> StagesByArea { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, EventDto> EventById { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, PenaltyDto> PenaltyById { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, CharacterDto> CharacterById { get; private set; } = new(StringComparer.OrdinalIgnoreCase); public List<string> Areas { get; private set; } = new();
    public string FirstAreaOrNull => Areas.Count > 0 ? Areas[0] : null;


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
    public EventDto GetEvent(string eventID)
        => EventById.TryGetValue(eventID, out var e) ? e : null;

    public PenaltyDto GetPenalty(string penaltyID) 
        => PenaltyById.TryGetValue(penaltyID, out var p) ? p : null;

    public CharacterDto GetCharacterById(string characterID)
        => CharacterById.TryGetValue(characterID, out var c) ? c : null;

    public IReadOnlyList<CharacterDto> GetAllAttackCharacters() => AttackCharacters;
    public IReadOnlyList<CharacterDto> GetAllSupportCharacters() => SupportCharacters;
    public IReadOnlyList<CharacterDto> GetAllTechCharacters() => TechCharacters;

    public void LoadStagesFromDB()
    {
        var allStages = StageRepository.Instance.GetAllStagesFromDB(); // DB 직접 읽기
        StageById = allStages.ToDictionary(s => s.StageID, s => s, StringComparer.OrdinalIgnoreCase);
        StagesByArea = allStages.GroupBy(s => s.Area)
                                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        Areas = StagesByArea.Keys.ToList();
    }

}
