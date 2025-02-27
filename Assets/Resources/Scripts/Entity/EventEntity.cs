using System;
using UnityEngine;

[System.Serializable]
public class EventEntity
{
    public string eventName;
    public string eventDescription;
    public bool isActivated = true;
    public MyEventType eventType;
    public string startDate;
    public string endDate;

    public EventEntity() { }
    public EventEntity(string name, string description, string start, string end)
    {
        eventName = name;
        eventDescription = description;
        startDate = start;
        endDate = end;
    }
    public EventEntity SetEventType(MyEventType type)
    {
        eventType = type;
        return this;
    }

    public EventEntity SetActivationStatus(bool status)
    {
        isActivated = status;
        return this;
    }
}

public enum MyEventType
{
    None,
    New,
    Daily,
    Weekly,
    Monthly,
    Yearly
}