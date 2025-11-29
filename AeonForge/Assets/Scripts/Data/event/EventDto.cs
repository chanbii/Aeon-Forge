using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum Turn
{
    Explore = 0,
    Aeon = 1
}

public enum Event
{
    Zone = 0,
    Monster = 1
}
public class EventDto
{
    public EventDto() { }
    public string EventID;
    public string Area;
    public DangerLevel Danger;
    public Turn TurnType;
    public Event EventType;
    public int EventRV;
    public int EventHP;
    public string Penalty;
    public string Explain;
    public int Probability;
}
