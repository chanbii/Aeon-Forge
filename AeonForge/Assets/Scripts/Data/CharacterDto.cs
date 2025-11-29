using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CharacterDto
{
    public CharacterDto() { }
    public string CharID;
    public string Name; 
    public string Job;
    public int BaseHP;
    public int MaxSP;
    public int SkillCount;
    public string SkillType;
    public string Skill1Desc;
    public string Skill2Desc;
    public string Skill3Desc;
}
