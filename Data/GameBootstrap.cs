using System.Collections.Generic;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("CSV")]
    public TextAsset stagesCsv;
    public TextAsset stageCostsCsv;     
    public TextAsset stageRewardsCsv;  

    public StageRepository StageRepo { get; private set; }

    private void Awake()
    {
        DatabaseService.Instance.Initialize("game.db");
        DatabaseService.Instance.EnsureSchema();

        // 1) CSV → DTO 파싱 
        if (stagesCsv != null)
        {
            IEnumerable<StageDto> stages = CsvParser.Parse<StageDto>(stagesCsv.text);
            DatabaseService.Instance.SeedStagesIfEmpty(stages);
        }

        if (stageCostsCsv != null)
        {
            var costs = CsvParser.Parse<StageCostDto>(stageCostsCsv.text);
            DatabaseService.Instance.SeedStageCostsIfEmpty(costs);
        }

        if (stageRewardsCsv != null)
        {
            var rewards = CsvParser.Parse<StageRewardDto>(stageRewardsCsv.text);
            DatabaseService.Instance.SeedStageRewardsIfEmpty(rewards);
        }

        // 2) Repo
        StageRepo = new StageRepository();

        // 3) 확인 로그
        Debug.Log($"[DB OK] stages = {StageRepo.GetAllStages().Count}");
    }
}
