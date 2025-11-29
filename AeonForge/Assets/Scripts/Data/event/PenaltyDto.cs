using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum Penalty
{
    Normal = 0,
    Special = 1
}
public enum Effect
{
    HP = 0,
    SP = 1,
    Poison = 2,
    Stun = 3
}

public enum TargetType
{
    Single = 0,
    AoE = 1,
    Aeonforge = 2
}
public class PenaltyDto
{
    public PenaltyDto() { }
    public string PenaltyID;
    public Penalty PenaltyType;
    public Effect EffectType;
    public int Value;
    public TargetType Target;
    public string Explain;
}
