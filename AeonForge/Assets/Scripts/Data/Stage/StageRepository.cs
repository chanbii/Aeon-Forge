using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using System.Linq;

public class StageRepository
{

    private static StageRepository _instance;

    private StageRepository()
    {
        // 리포지토리 초기화
    }

    public static StageRepository Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new StageRepository();
            }
            return _instance;
        }
    }

    public List<StageDto> GetAllStages()
    {
        if (Tables.Instance == null || Tables.Instance.StageById == null)
        {
            return new List<StageDto>();
        }
        return Tables.Instance.StageById.Values.ToList();
    }
    private static DangerLevel ParseDanger(object value)
    {
        if (value == null || value is DBNull) return DangerLevel.Low;

        switch (value)
        {
            case long l:
                return ((int)l) switch { 0 => DangerLevel.Low, 1 => DangerLevel.Medium, 2 => DangerLevel.High, _ => DangerLevel.Low };
            case int i:
                return i switch { 0 => DangerLevel.Low, 1 => DangerLevel.Medium, 2 => DangerLevel.High, _ => DangerLevel.Low };
            case string s:
                if (Enum.TryParse<DangerLevel>(s, true, out var d)) return d;
                if (int.TryParse(s, out var n))
                    return n switch { 0 => DangerLevel.Low, 1 => DangerLevel.Medium, 2 => DangerLevel.High, _ => DangerLevel.Low };
                return DangerLevel.Low;
            default:
                return DangerLevel.Low;
        }
    }

    public List<string> GetAreas()
    {
        if (Tables.Instance == null || Tables.Instance.Areas == null) return new List<string>();
        return new List<string>(Tables.Instance.Areas);
    }

    public List<StageDto> GetStagesByArea(string area)
    {
        if (Tables.Instance == null || Tables.Instance.StagesByArea == null) return new List<StageDto>();

        if (Tables.Instance.StagesByArea.TryGetValue(area, out var list))
        {
            return new List<StageDto>(list);
        }
        return new List<StageDto>();
    }
    public List<string> GetAreasInTableOrder()
    {
        return GetAreas();
    }

    // 진행 맵 로드
    public Dictionary<string, StageProgressDto> GetProgressMap()
    {
        var map = new Dictionary<string, StageProgressDto>();
        using var conn = DatabaseService.Instance.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT StageID AS StageId, Unlocked, Cleared FROM stage_progress;";
        using var r = cmd.ExecuteReader();

        int oId = r.GetOrdinal("StageId");
        int oU = r.GetOrdinal("Unlocked");
        int oC = r.GetOrdinal("Cleared");

        while (r.Read())
        {
            var p = new StageProgressDto
            {
                StageID = r.GetString(oId), 
                IsUnlocked = r.GetInt32(oU) != 0,
                IsCleared = r.GetInt32(oC) != 0
            };
            map[p.StageID] = p;
        }
        return map;
    }


    // 최초 해금: 각 Area의 첫 스테이지만 해금
    public void EnsureInitialUnlocks()
    {
        using var conn = DatabaseService.Instance.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;

        cmd.CommandText = @"SELECT StageId FROM stages ORDER BY Id LIMIT 1;";
        var first = cmd.ExecuteScalar() as string;
        if (!string.IsNullOrEmpty(first))
            UpsertProgress_UnlockedCleared(cmd, first, unlocked: true, cleared: false);

        tx.Commit();
    }


    // 클리어 처리: 현재 스테이지 클리어 + 같은 Area의 다음 스테이지 해금
    public void MarkClearedAndUnlockNext(string stageId)
    {
        using var conn = DatabaseService.Instance.Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;

        // 현재 행의 Id(int) 찾기 
        cmd.CommandText = @"SELECT Id FROM stages WHERE StageId=@sid;";
        cmd.Parameters.Clear();
        var p0 = cmd.CreateParameter();
        p0.ParameterName = "@sid";
        p0.Value = stageId;                
        cmd.Parameters.Add(p0);

        int curId = -1;
        using (var r = cmd.ExecuteReader())
            if (r.Read()) curId = r.GetInt32(0);
        if (curId < 0) { tx.Rollback(); throw new Exception("Unknown StageId: " + stageId); }

        // 현재 스테이지 클리어
        UpsertProgress_UnlockedCleared(cmd, stageId, unlocked: true, cleared: true);

        // 해금
        cmd.CommandText = @"SELECT StageId FROM stages WHERE Id > @id ORDER BY Id LIMIT 1;";
        cmd.Parameters.Clear();
        var p1 = cmd.CreateParameter();
        p1.ParameterName = "@id";
        p1.Value = curId;          
        cmd.Parameters.Add(p1);

        var next = cmd.ExecuteScalar() as string; 
        if (!string.IsNullOrEmpty(next))
            UpsertProgress_UnlockedCleared(cmd, next, unlocked: true, cleared: false);

        tx.Commit();
    }
    private void UpsertProgress_UnlockedCleared(SqliteCommand cmd, string stageId, bool unlocked, bool cleared)
    {
        cmd.CommandText = @"UPDATE stage_progress
                        SET Unlocked = (Unlocked OR @u),
                            Cleared  = (Cleared  OR @c)
                        WHERE StageID=@sid;";
        cmd.Parameters.Clear();
        cmd.Parameters.Add(new SqliteParameter("@u", unlocked ? 1 : 0));
        cmd.Parameters.Add(new SqliteParameter("@c", cleared ? 1 : 0));
        cmd.Parameters.Add(new SqliteParameter("@sid", stageId)); // ← string
        int n = cmd.ExecuteNonQuery();

        if (n == 0)
        {
            cmd.CommandText = @"INSERT INTO stage_progress (StageID, Unlocked, Cleared)
                            VALUES (@sid, @u, @c);";
            cmd.ExecuteNonQuery();
        }
    }

    public List<StageDto> GetAllStagesFromDB()
    {
        var list = new List<StageDto>();
        using var conn = DatabaseService.Instance.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT StageID, StageName, Area, StageNum, Danger, Level FROM stages;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(new StageDto
            {
                StageID = r.GetString(0),
                StageName = r.GetString(1),
                Area = r.GetString(2),
                StageNum = r.GetInt32(3),
                Danger = Enum.TryParse<DangerLevel>(r.GetString(4), out var d) ? d : DangerLevel.Low,
                Level = r.GetInt32(5)
            });
        }
        return list;
    }

    public List<StageCostDto> GetAllStageCostsFromDB()
    {
        var list = new List<StageCostDto>();
        using var conn = DatabaseService.Instance.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT StageID, CostType, CostValue FROM stage_costs;";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            list.Add(new StageCostDto
            {
                StageID = r.GetString(0),
                CostType = r.GetString(1),
                CostValue = r.GetInt32(2)
            });
        }
        return list;
    }

    public List<StageRewardDto> GetAllStageRewardsFromDB()
    {
        var list = new List<StageRewardDto>();
        using var conn = DatabaseService.Instance.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT StageID, RewardMoney, RewardTicket FROM stage_rewards;";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            list.Add(new StageRewardDto
            {
                StageID = r.GetString(0),
                RewardMoney = r.GetInt32(1),
                RewardTicket = r.GetInt32(2)
            });
        }
        return list;
    }

    public List<string> GetAreasFromDB()
    {
        var list = new List<string>();
        using var conn = DatabaseService.Instance.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT DISTINCT Area FROM stages ORDER BY Area;";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            list.Add(r.GetString(0));
        }
        return list;
    }
}
