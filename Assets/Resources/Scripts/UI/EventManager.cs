using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils.Save;
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
        TryLoadSampleEvents();
        Init();
    }

    private void TryLoadSampleEvents()
    {
        if (!DevData.IsActive)
        {
            DevData.LogSkipped(nameof(EventManager) + ".CreateSampleEvents");
            return;
        }

        events.AddRange(DevData.Current.CreateSampleEvents(10));
        Debug.Log($"[DEV-DATA] Loaded {events.Count} sample events (memory only).");
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

}