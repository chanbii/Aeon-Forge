using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class StageProgressDto
{
    public StageProgressDto() { }
    public string StageID;
    public bool IsUnlocked;
    public bool IsCleared;  
}
