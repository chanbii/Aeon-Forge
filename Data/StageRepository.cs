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
ORDER BY s.Area, s.StageNum, s.Id;";

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
                // "Low/Medium/High" 또는 "0/1/2" 모두 허용
                if (Enum.TryParse<DangerLevel>(s, true, out var d)) return d;
                if (int.TryParse(s, out var n))
                    return n switch { 0 => DangerLevel.Low, 1 => DangerLevel.Medium, 2 => DangerLevel.High, _ => DangerLevel.Low };
                return DangerLevel.Low;
            default:
                return DangerLevel.Low;
        }
    }
}
