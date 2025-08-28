using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;

public sealed class DatabaseService : IDisposable
{
    private static DatabaseService _instance;
    public static DatabaseService Instance => _instance ??= new DatabaseService();

    private string _dbPath;
    private string _connStr;
    private bool _initialized;

    private DatabaseService() { }

    public void Initialize(string dbFileName = "game.db")
    {
        if (_initialized) return;

        _dbPath = Path.Combine(Application.persistentDataPath, dbFileName);
        _connStr = $"URI=file:{_dbPath}";

        using (var conn = new Mono.Data.Sqlite.SqliteConnection(_connStr))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_keys = ON;"; cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA journal_mode = WAL;"; cmd.ExecuteNonQuery();
                cmd.CommandText = "PRAGMA synchronous = NORMAL;"; cmd.ExecuteNonQuery();
            }
            conn.Close();
        }

        _initialized = true;
        Debug.Log($"[DB] Path = {_dbPath}");
    }

    public bool IsInitialized => _initialized;
    public string DbPath => _dbPath;

    public SqliteConnection Open()
    {
        if (!_initialized) Initialize("game.db");
        var conn = new SqliteConnection(_connStr);
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys=ON;"; cmd.ExecuteNonQuery();  
            cmd.CommandText = "PRAGMA busy_timeout=3000;"; cmd.ExecuteNonQuery();
        }
        return conn;
    }

    private bool ColumnExists(SqliteConnection conn, string table, string column)
    {
        using (var c = conn.CreateCommand())
        {
            c.CommandText = $"SELECT 1 FROM pragma_table_info('{table}') WHERE name='{column}' LIMIT 1;";
            return c.ExecuteScalar() != null;
        }
    }
    public void EnsureSchema()
    {
        using (var conn = Open())
        using (var cmd = conn.CreateCommand())
        {
            // 1) stages 
            cmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS stages(
            id         INTEGER PRIMARY KEY AUTOINCREMENT,
            StageID    TEXT    NOT NULL,
            StageName  TEXT    NOT NULL,
            Area       TEXT    NOT NULL,
            StageNum   INTEGER NOT NULL,
            Danger     TEXT,
            Level      INTEGER NOT NULL
          );";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "CREATE UNIQUE INDEX IF NOT EXISTS ux_stages_StageID ON stages(StageID);";
            cmd.ExecuteNonQuery();

            // 2) stage_costs
            cmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS stage_costs(
            Id        INTEGER PRIMARY KEY AUTOINCREMENT,
            StageID   TEXT    NOT NULL,     -- stages.StageID 참조
            CostType  TEXT,     
            CostValue    INTEGER,
            FOREIGN KEY(StageID) REFERENCES stages(StageID)
          );";
            cmd.ExecuteNonQuery();

            cmd.CommandText =
                "CREATE UNIQUE INDEX IF NOT EXISTS ux_stage_costs ON stage_costs(StageID, CostType);";
            cmd.ExecuteNonQuery();

            // 3) stage_rewards
            cmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS stage_rewards(
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            StageID    TEXT    NOT NULL,    -- stages.StageID 참조
            RewardMoney INTEGER, 
            RewardTicket INTEGER,   
            FOREIGN KEY(StageID) REFERENCES stages(StageID)
          );";
            cmd.ExecuteNonQuery();

            // 4) stage_progress: 잠금/클리어 상태
            cmd.CommandText =
            @"CREATE TABLE IF NOT EXISTS stage_progress(
            StageID   TEXT PRIMARY KEY,
            Unlocked  INTEGER NOT NULL DEFAULT 0,
            Cleared   INTEGER NOT NULL DEFAULT 0,
            FOREIGN KEY(StageID) REFERENCES stages(StageID)
);";
            cmd.ExecuteNonQuery();


            // 복합 유니크
            cmd.CommandText =
                "CREATE UNIQUE INDEX IF NOT EXISTS ux_stage_rewards ON stage_rewards(StageID, RewardMoney);";
            cmd.ExecuteNonQuery();

            // --- 마이그레이션 ---
            if (!ColumnExists(conn, "stage_rewards", "RewardTicket"))
            {
                cmd.CommandText = "ALTER TABLE stage_rewards ADD COLUMN RewardTicket INTEGER NOT NULL DEFAULT 0;";
                cmd.ExecuteNonQuery();
            }

            // 다른 컬럼도 동일하게 점검/추가
            if (!ColumnExists(conn, "stage_rewards", "RewardMoney"))
            {
                // 새 테이블로 교체 
                cmd.CommandText = @"
                BEGIN TRANSACTION;
                CREATE TABLE stage_rewards_new(
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StageID TEXT NOT NULL,
                RewardMoney INTEGER NOT NULL,
                RewardTicket INTEGER NOT NULL
                );
                INSERT INTO stage_rewards_new (StageID, RewardMoney, RewardTicket)
                SELECT StageID,
                COALESCE(RewardMoney, 0),
                COALESCE(RewardTicket, 0)
                FROM stage_rewards;
                DROP TABLE stage_rewards;
                ALTER TABLE stage_rewards_new RENAME TO stage_rewards;
                CREATE UNIQUE INDEX IF NOT EXISTS ux_stage_rewards ON stage_rewards(StageID, RewardMoney);
                COMMIT;";
                cmd.ExecuteNonQuery();
            }
        }
    }

    public void EnsureProgressRows(IEnumerable<string> stageIds)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        foreach (var id in stageIds)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO stage_progress(StageID,Unlocked,Cleared) VALUES(@id,0,0);";
            cmd.Parameters.Add(new SqliteParameter("@id", id));
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }
    public bool IsUnlocked(string stageId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Unlocked FROM stage_progress WHERE StageID=@id;";
        cmd.Parameters.Add(new SqliteParameter("@id", stageId));
        var o = cmd.ExecuteScalar();
        return o != null && Convert.ToInt32(o) != 0;
    }

    public void Unlock(string stageId)
    {
        using var conn = Open();
        using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = "INSERT OR IGNORE INTO stage_progress(StageID,Unlocked,Cleared) VALUES(@id,1,0);";
        cmd1.Parameters.Add(new SqliteParameter("@id", stageId));
        cmd1.ExecuteNonQuery();

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "UPDATE stage_progress SET Unlocked=1 WHERE StageID=@id;";
        cmd2.Parameters.Add(new SqliteParameter("@id", stageId));
        cmd2.ExecuteNonQuery();
    }

    public void MarkCleared(string stageId)
    {
        using var conn = Open();
        using var cmd1 = conn.CreateCommand();
        cmd1.CommandText = "INSERT OR IGNORE INTO stage_progress(StageID,Unlocked,Cleared) VALUES(@id,1,1);";
        cmd1.Parameters.Add(new SqliteParameter("@id", stageId));
        cmd1.ExecuteNonQuery();

        using var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "UPDATE stage_progress SET Cleared=1, Unlocked=1 WHERE StageID=@id;";
        cmd2.Parameters.Add(new SqliteParameter("@id", stageId));
        cmd2.ExecuteNonQuery();
    }



    public void SeedStagesIfEmpty(IEnumerable<StageDto> stages)
    {
        using (var conn = Open())
        {
            // 존재 여부
            using (var check = conn.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(1) FROM stages;";
                var count = Convert.ToInt64(check.ExecuteScalar());
                if (count > 0) return;
            }

            using (var tx = conn.BeginTransaction())
            {
                foreach (var s in stages)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText =
                          @"INSERT INTO stages (StageID, StageName, Area, StageNum, Danger, Level)
                            VALUES (@StageID, @StageName, @Area, @StageNum, @Danger, @Level);";
                        cmd.Parameters.Add(new SqliteParameter("@StageID", s.StageID));
                        cmd.Parameters.Add(new SqliteParameter("@StageName", s.StageName));
                        cmd.Parameters.Add(new SqliteParameter("@Area", s.Area));
                        cmd.Parameters.Add(new SqliteParameter("@StageNum", s.StageNum));
                        cmd.Parameters.Add(new SqliteParameter("@Danger", s.Danger.ToString()));
                        cmd.Parameters.Add(new SqliteParameter("@Level", s.Level));
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
        }
    }

    public void SeedStageCostsIfEmpty(IEnumerable<StageCostDto> costs)
    {
        if (costs == null) return;

        using (var conn = Open())
        {
            using (var check = conn.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(1) FROM stage_costs;";
                var count = Convert.ToInt64(check.ExecuteScalar());
                if (count > 0) return;
            }

            using (var tx = conn.BeginTransaction())
            {
                foreach (var c in costs)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText =
                          @"INSERT INTO stage_costs (StageID, CostType, CostValue)
                        VALUES (@StageID, @CostType, @CostValue);";
                        cmd.Parameters.Add(new SqliteParameter("@StageID", c.StageID));
                        cmd.Parameters.Add(new SqliteParameter("@CostType", c.CostType));
                        cmd.Parameters.Add(new SqliteParameter("@CostValue", c.CostValue));
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
        }
    }

    public void SeedStageRewardsIfEmpty(IEnumerable<StageRewardDto> rewards)
    {
        if (rewards == null) return;

        using (var conn = Open())
        {
            using (var check = conn.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(1) FROM stage_rewards;";
                var count = Convert.ToInt64(check.ExecuteScalar());
                if (count > 0) return;
            }

            using (var tx = conn.BeginTransaction())
            {
                foreach (var r in rewards)
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText =
                          @"INSERT INTO stage_rewards (StageID, RewardMoney, RewardTicket)
                        VALUES (@StageID, @RewardMoney, @RewardTicket);";
                        cmd.Parameters.Add(new SqliteParameter("@StageID", r.StageID));
                        cmd.Parameters.Add(new SqliteParameter("@RewardMoney", r.RewardMoney));
                        cmd.Parameters.Add(new SqliteParameter("@RewardTicket", r.RewardTicket));
                        cmd.ExecuteNonQuery();
                    }
                }
                tx.Commit();
            }
        }
    }


    public void Dispose() { _initialized = false; }
}
