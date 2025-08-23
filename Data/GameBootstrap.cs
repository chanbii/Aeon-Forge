using System.Collections.Generic;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("CSV")]
    public TextAsset stagesCsv;
    public TextAsset stageCostsCsv;     // 있으면 할당
    public TextAsset stageRewardsCsv;   // 있으면 할당

    public StageRepository StageRepo { get; private set; }

    private void Awake()
    {
        DatabaseService.Instance.Initialize("game.db");
        DatabaseService.Instance.EnsureSchema();

        // 1) CSV → DTO 파싱 (네 CsvParser 그대로 사용)
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
