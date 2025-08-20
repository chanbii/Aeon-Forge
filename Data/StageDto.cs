using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public enum DangerLevel
{
    Low,
    Medium,
    High
}

[System.Serializable]
public class StageDto
{
    public string StageID;
    public string Area;
    public int StageNum;
    public DangerLevel Danger;
    public int MinLevel;
    public int MaxLevel;
}

