using System;
using System.Collections.Generic;

public enum JobType { Unknown=0, Attack, Support, Tech } // 공격/지원/기술

[Serializable]
public class CharacterDefinition
{
    public string id;            // CharID
    public string name;          // Name
    public JobType job = JobType.Unknown; // Job (공격/지원/기술)
    public int baseHP;           // BaseHP
    public int maxSP;            // MaxSP
    public int skillCount;       // SkillCount (없어도 동작)
    public string skillType;     // SkillType
    public List<string> skills = new(); // Skill1Desc~3Desc 또는 탐지된 모든 스킬 설명
    public Dictionary<string,string> extra =
        new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase); // 확장 컬럼
}

public static class JobMapper
{
    public static JobType FromString(string s)
    {
        return s switch
        {
            "공격" => JobType.Attack,
            "지원" => JobType.Support,
            "기술" => JobType.Tech,
            _ => JobType.Unknown
        };
    }
}
