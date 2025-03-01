using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Unity.Mathematics;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager instance;
    public GameObject eventPrefab;
    public Transform eventParent;
    public List<EventSlot> eventSlots = new List<EventSlot>();
    public List<EventEntity> events = new List<EventEntity>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    void Start()
    {
        FakeData();
        Init();
    }

    private void Init()
    {
        UpdateEvent();
    }

    public void UpdateEvent()
    {
        Debug.Log("Update Event count: " + events.Count);
        if (events.Count > eventSlots.Count)
        {
            for (int i = eventSlots.Count; i < events.Count; i++)
            {
                GameObject slot = Instantiate(eventPrefab, eventParent);
                EventSlot eventSlot = slot.GetComponent<EventSlot>();
                eventSlot.slotIndex = i;
                eventSlots.Add(eventSlot);
            }
        }
        else if (events.Count < eventSlots.Count)
        {
            for (int i = eventSlots.Count - 1; i >= events.Count; i--)
            {
                Destroy(eventSlots[i].gameObject);
                eventSlots.RemoveAt(i);
            }
        }
        for (int i = 0; i < events.Count; i++)
        {
            eventSlots[i].SetEvent(events[i]);
        }
    }

    public void AddEvent(EventEntity eventEntity)
    {
        events.Add(eventEntity);
        UpdateEvent();
    }

    public void RemoveEvent(EventEntity eventEntity)
    {
        events.Remove(eventEntity);
        UpdateEvent();
    }

    public void SetEventFocus(int slotIndex)
    {
        for (int i = 0; i < eventSlots.Count; i++)
        {
            eventSlots[i].SetFocus(i == slotIndex);
        }
    }

    public EventEntity GetEvent(int slotIndex)
    {
        return events[slotIndex];
    }

    private void FakeData()
    {
        List<MyEventType> types = new List<MyEventType>() { MyEventType.None, MyEventType.New };
        for (int i = 0; i < 10; i++)
        {
            events.Add(new EventEntity("Event_" + i, "Event " + i + " Description", new System.DateTime(2025, 1, 1).ToString("yyyy-MM-dd"), new System.DateTime(2025, 2, 1).ToString("yyyy-MM-dd")).SetEventType(MyEventType.New).SetActivationStatus(true));
        }
    }
}