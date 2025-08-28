using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;

public class StageRepository
{
    public List<StageDto> GetAllStages()
    {
        var list = new List<StageDto>();

        using (var conn = DatabaseService.Instance.Open())
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT
  s.Id, s.StageId, s.StageName, s.Area, s.StageNum, s.Danger, s.Level,
  c.CostType, c.CostValue,
  r.RewardMoney, r.RewardTicket
FROM stages s
LEFT JOIN stage_costs   c ON c.StageId = s.StageId
LEFT JOIN stage_rewards r ON r.StageId = s.StageId
ORDER BY s.Id;  -- 테이블 삽입 순서 그대로";

            using (IDataReader r = cmd.ExecuteReader())
            {
                int oStageId = r.GetOrdinal("StageId");
                int oStageName = r.GetOrdinal("StageName");
                int oArea = r.GetOrdinal("Area");
                int oStageNum = r.GetOrdinal("StageNum");
                int oDanger = r.GetOrdinal("Danger");
                int oLevel = r.GetOrdinal("Level");
                int oCostType = r.GetOrdinal("CostType");
                int oCostValue = r.GetOrdinal("CostValue");
                int oRewardMoney = r.GetOrdinal("RewardMoney");
                int oRewardTicket = r.GetOrdinal("RewardTicket");

                while (r.Read())
                {
                    var dangerVal = r.IsDBNull(oDanger) ? null : r.GetValue(oDanger);

                    var dto = new StageDto
                    {
                        StageID = r.IsDBNull(oStageId) ? "" : r.GetString(oStageId),
                        StageName = r.IsDBNull(oStageName) ? "" : r.GetString(oStageName),
                        Area = r.IsDBNull(oArea) ? "" : r.GetString(oArea),
                        StageNum = r.IsDBNull(oStageNum) ? 0 : r.GetInt32(oStageNum),
                        Danger = ParseDanger(dangerVal),  
                        Level = r.IsDBNull(oLevel) ? 0 : r.GetInt32(oLevel),

                        Cost = new StageCostDto
                        {
                            CostType = r.IsDBNull(oCostType) ? "" : r.GetString(oCostType),
                            CostValue = r.IsDBNull(oCostValue) ? 0 : r.GetInt32(oCostValue),
                        },
                        Reward = new StageRewardDto
                        {
                            RewardMoney = r.IsDBNull(oRewardMoney) ? 0 : r.GetInt32(oRewardMoney),
                            RewardTicket = r.IsDBNull(oRewardTicket) ? 0 : r.GetInt32(oRewardTicket),
                        }
                    };

                    list.Add(dto);
                }
            }
        }

        return list;
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
        var list = new List<string>();
        using (var conn = DatabaseService.Instance.Open())
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"SELECT DISTINCT Area
                            FROM stages
                            WHERE Area IS NOT NULL AND TRIM(Area) <> ''
                            ORDER BY Area;";
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(r.GetString(0));
        }
        return list;
    }

    public List<StageDto> GetStagesByArea(string area)
    {
        var list = new List<StageDto>();
        using (var conn = DatabaseService.Instance.Open())
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT
  s.Id, s.StageId, s.StageName, s.Area, s.StageNum, s.Danger, s.Level,
  c.CostType, c.CostValue,
  r.RewardMoney, r.RewardTicket
FROM stages s
LEFT JOIN stage_costs   c ON c.StageId = s.StageId
LEFT JOIN stage_rewards r ON r.StageId = s.StageId
WHERE s.Area = @area
ORDER BY s.Id;";
            var p = cmd.CreateParameter(); p.ParameterName = "@area"; p.Value = area ?? ""; cmd.Parameters.Add(p);

            using (var r = cmd.ExecuteReader())
            {
                int oStageId = r.GetOrdinal("StageId");
                int oStageName = r.GetOrdinal("StageName");
                int oArea = r.GetOrdinal("Area");
                int oStageNum = r.GetOrdinal("StageNum");
                int oDanger = r.GetOrdinal("Danger");
                int oLevel = r.GetOrdinal("Level");
                int oCostType = r.GetOrdinal("CostType");
                int oCostValue = r.GetOrdinal("CostValue");
                int oRewardMoney = r.GetOrdinal("RewardMoney");
                int oRewardTicket = r.GetOrdinal("RewardTicket");

                while (r.Read())
                {
                    var name = r.IsDBNull(oStageName) ? "" : r.GetString(oStageName);
                    if (string.IsNullOrWhiteSpace(name) || name == "ㅡ" || name == "-")
                        name = $"{r.GetString(oArea)}-{r.GetInt32(oStageNum)}";

                    list.Add(new StageDto
                    {
                        StageID = r.IsDBNull(oStageId) ? "" : r.GetString(oStageId),
                        StageName = name,
                        Area = r.IsDBNull(oArea) ? "" : r.GetString(oArea),
                        StageNum = r.IsDBNull(oStageNum) ? 0 : r.GetInt32(oStageNum),
                        Danger = ParseDanger(r.IsDBNull(oDanger) ? null : r.GetValue(oDanger)),
                        Level = r.IsDBNull(oLevel) ? 0 : r.GetInt32(oLevel),
                        Cost = new StageCostDto
                        {
                            CostType = r.IsDBNull(oCostType) ? "" : r.GetString(oCostType),
                            CostValue = r.IsDBNull(oCostValue) ? 0 : r.GetInt32(oCostValue),
                        },
                        Reward = new StageRewardDto
                        {
                            RewardMoney = r.IsDBNull(oRewardMoney) ? 0 : r.GetInt32(oRewardMoney),
                            RewardTicket = r.IsDBNull(oRewardTicket) ? 0 : r.GetInt32(oRewardTicket),
                        }
                    });
                }
            }
        }
        return list;
    }
    public List<string> GetAreasInTableOrder()
    {
        var list = new List<string>();
        using (var conn = DatabaseService.Instance.Open())
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
SELECT Area
FROM stages
WHERE Area IS NOT NULL AND TRIM(Area) <> ''
GROUP BY Area
ORDER BY MIN(Id);";
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(r.GetString(0));
        }
        return list;
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
}
