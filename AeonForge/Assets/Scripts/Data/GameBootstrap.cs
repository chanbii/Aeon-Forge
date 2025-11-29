using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("CSV")]
    public TextAsset stagesCsv;
    public TextAsset stageCostsCsv;     
    public TextAsset stageRewardsCsv;
    public TextAsset eventCsv;
    public TextAsset penaltyCsv;

    [Header("Character CSVs")]
    public TextAsset attackCharacterCsv;
    public TextAsset supportCharacterCsv;
    public TextAsset techCharacterCsv;
    public StageRepository StageRepo { get; private set; }

    private void Awake()
    {
        DatabaseService.Instance.Initialize("game.db");
        DatabaseService.Instance.EnsureSchema();

        SeedDataIfAvailable<StageDto>(stagesCsv, DatabaseService.Instance.SeedStagesIfEmpty);
        SeedDataIfAvailable<StageCostDto>(stageCostsCsv, DatabaseService.Instance.SeedStageCostsIfEmpty);
        SeedDataIfAvailable<StageRewardDto>(stageRewardsCsv, DatabaseService.Instance.SeedStageRewardsIfEmpty);
        SeedDataIfAvailable<EventDto>(eventCsv, DatabaseService.Instance.SeedEventIfEmpty);
        SeedDataIfAvailable<PenaltyDto>(penaltyCsv, DatabaseService.Instance.SeedPenaltyIfEmpty);

        List<CharacterDto> allCharacters = new List<CharacterDto>();

        if (attackCharacterCsv != null)
            allCharacters.AddRange(CsvParser.Parse<CharacterDto>(attackCharacterCsv.text));

        if (supportCharacterCsv != null)
            allCharacters.AddRange(CsvParser.Parse<CharacterDto>(supportCharacterCsv.text));

        if (techCharacterCsv != null)
            allCharacters.AddRange(CsvParser.Parse<CharacterDto>(techCharacterCsv.text));

        if (allCharacters.Any())
        {
            DatabaseService.Instance.SeedCharacterIfEmpty(allCharacters);
        }

        Tables.Instance.LoadStagesFromDB();

        this.StageRepo = StageRepository.Instance;

        Debug.Log($"[DB OK] stages = {StageRepository.Instance.GetAllStages().Count}");

        StageRepository.Instance.EnsureInitialUnlocks();
    }

    private void SeedDataIfAvailable<T>(TextAsset csvAsset, System.Action<IEnumerable<T>> seedMethod)
        where T : new()
    {
        if (csvAsset != null)
        {
            var data = CsvParser.Parse<T>(csvAsset.text);
            if (data != null && data.Any())
            {
                seedMethod(data);
            }
            else
            {
                Debug.LogWarning($"[GameBootstrap] CSV Parsing returned empty for {typeof(T).Name}");
            }
        }
    }
}
