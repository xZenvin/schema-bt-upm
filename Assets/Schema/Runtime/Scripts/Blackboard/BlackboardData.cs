using System;
using System.Collections.Generic;
using UnityEngine;
using Schema.Runtime;

public class BlackboardData
{
    public struct EntryData
    {
        public string name;
        public object value;
        public Type type;
        public EntryData(string name, object value, Type type)
        {
            this.name = name;
            this.value = value;
            this.type = type;
        }
    }
    private SchemaAgent agent;
    private Dictionary<string, EntryData> values = new Dictionary<string, EntryData>();
    public BlackboardData(Blackboard blackboard)
    {
        Initialize(blackboard);
    }
    private void Initialize(Blackboard blackboard)
    {
        foreach (BlackboardEntry entry in blackboard.entries)
        {
            object defaultValue = GetDefault(Type.GetType(entry.typeString));

            values.Add(entry.uID, new EntryData(entry.name, defaultValue, Type.GetType(entry.typeString)));
        }
    }
    private object GetDefault(Type t)
    {
        if (t.IsValueType)
        {
            return Activator.CreateInstance(t);
        }
        return null;
    }
    public object GetValue(BlackboardEntrySelector selector)
    {
        return GetValue(selector.entryID);
    }
    public Type GetType(BlackboardEntrySelector selector)
    {
        return GetValue(selector).GetType();
    }
    public object GetValue(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        if (values.ContainsKey(id))
        {
            return values[id].value;
        }
        else
        {
            return null;
        }
    }
    public T GetValue<T>(BlackboardEntrySelector selector)
    {
        if (selector == null || String.IsNullOrEmpty(selector.entryID)) return default(T);

        return GetValue<T>(selector.entryID);
    }
    public T GetValue<T>(string id)
    {
        if (values.ContainsKey(id))
        {
            return (T)(values[id].value);
        }
        else
        {
            return default(T);
        }
    }
    public void SetValue<T>(BlackboardEntrySelector selector, T value)
    {
        SetValue<T>(selector.entryID, value);
    }
    public void SetValue<T>(string id, T value)
    {
        EntryData v = values[id];
        if (value == null)
        {
            values[id] = new EntryData(v.name, null, v.type);
            return;
        }

        if (value.GetType() == typeof(T))
        {
            values[id] = new EntryData(v.name, value, v.type);
        }
        else
        {
            throw new UnityException($"Cannot set the value of blackboard key {id} because the types are not equivalent.");
        }
    }
    /// <summary>
    /// Gets the Type of the entry specified by the provided id
    /// </summary>
    /// <param name="id">The id of the entry to retrieve</param>
    /// <returns>The Type of the entry</returns>
    public Type GetEntryType(string id)
    {
        return values[id].type;
    }
    /// <summary>
    /// Get the EntryData for the entry with the specified id
    /// </summary>
    /// <param name="id">The id of the entry to retrieve</param>
    /// <returns>The EntryData for the entry</returns>
    public EntryData GetEntry(string id)
    {
        return values[id];
    }
}

internal static class GlobalBlackboard
{
    public static Dictionary<string, object> dict
    {
        get
        {
            if (!UnityEngine.Application.isPlaying)
                return null;

            if (_dict == null)
                _dict = new Dictionary<string, object>();

            return _dict;
        }
    }
    private static Dictionary<string, object> _dict;
}
