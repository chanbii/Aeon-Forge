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
            cmd.CommandText =
              @"SELECT Id, StageId, StageName, Area, StageNum, Danger, Level
                FROM stages
                ORDER BY Area, StageNum, Id;";

            using (IDataReader r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    var dangerStr = r.IsDBNull(5) ? "Low" : r.GetString(5);
                    var parsed = System.Enum.TryParse<DangerLevel>(dangerStr, out var d)
                                 ? d : DangerLevel.Low;

                    list.Add(new StageDto
                    {
                        StageID = r.GetString(1),
                        StageName = r.GetString(2),
                        Area = r.GetString(3),
                        StageNum = r.GetInt32(4),
                        Danger = parsed,    // TEXT ¡æ enum
                        Level = r.GetInt32(6),
                    });
                }
            }
        }
        return list;
    }
}
