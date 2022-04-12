using System;
using System.Collections.Generic;
using System.Reflection;

internal static class BlackboardDataContainer
{
    public static void Initialize(Blackboard blackboard)
    {
        foreach (BlackboardEntry entry in blackboard.entries)
        {
            if (!values.ContainsKey(entry.uID))
                values.Add(entry.uID, new EntryData(entry));
        }
    }
    private static Dictionary<string, EntryData> values = new Dictionary<string, EntryData>();
    public static object Get(string id, int pid)
    {
        values.TryGetValue(id, out EntryData data);

        return data?.GetValue(pid);
    }
    public static void Set(string id, int pid, object value)
    {
        values.TryGetValue(id, out EntryData data);

        data?.SetValue(pid, value);
    }
    internal class EntryData
    {
        public List<object> value = new List<object>();
        private object defaultValue;
        public BlackboardEntry.EntryType type;
        public EntryData(BlackboardEntry entry)
        {
            type = entry.entryType;
            defaultValue = entry.type.IsValueType ? Activator.CreateInstance(entry.type) : null;
            value.Add(defaultValue);
        }
        public object GetValue(int pid)
        {
            if (type == BlackboardEntry.EntryType.Local)
            {
                if (pid > value.Count - 1)
                {
                    while (pid > value.Count - 1)
                    {
                        value.Add(defaultValue);
                    }
                    return defaultValue;
                }

                return value[pid];
            }
            else
            {
                return value[0];
            }
        }
        public void SetValue(int pid, object v)
        {
            if (type == BlackboardEntry.EntryType.Local)
            {
                if (pid > value.Count - 1)
                {
                    while (pid > value.Count - 2)
                    {
                        value.Add(defaultValue);
                    }

                    value.Add(v);

                    return;
                }

                value[pid] = v;
            }
            else
            {
                value[0] = v;
            }
        }
    }
}