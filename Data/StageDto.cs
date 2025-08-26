using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum DangerLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}

[System.Serializable]
public class StageDto
{
    public string StageID;
    public string StageName;
    public string Area;
    public int StageNum;
    public DangerLevel Danger;
    public int Level;
    public StageCostDto Cost = new StageCostDto();
    public StageRewardDto Reward = new StageRewardDto();
}

