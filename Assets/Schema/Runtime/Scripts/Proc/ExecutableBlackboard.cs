using System;
using System.Collections.Generic;
using UnityEngine;

namespace Schema.Internal
{
    public class ExecutableBlackboard
    {
        private static Dictionary<BlackboardEntry, EntryData> globalValues;
        private readonly Dictionary<string, EntryData> dynamicValues = new();
        private readonly Dictionary<BlackboardEntry, EntryData> values = new();

        public ExecutableBlackboard(Blackboard blackboard)
        {
            for (int i = 0; i < blackboard.entries.Length; i++)
            {
                BlackboardEntry entry = blackboard.entries[i];

                if (!values.ContainsKey(entry))
                    values.Add(entry, new EntryData(entry));
            }

            if (globalValues == null)
            {
                globalValues = new Dictionary<BlackboardEntry, EntryData>();

                for (int i = 0; i < Blackboard.global.entries.Length; i++)
                {
                    BlackboardEntry entry = Blackboard.global.entries[i];

                    if (!globalValues.ContainsKey(entry))
                        globalValues.Add(entry, new EntryData(entry));
                }

            }
        }

        public object GetDynamic(string name)
        {
            ExecutionContext current = ExecutionContext.current;

            dynamicValues.TryGetValue(name, out EntryData data);

            if (data != null && (current.node.index < data.position.Item1 ||
                                 current.node.index >= data.position.Item1 + data.position.Item2))
            {
                dynamicValues.Remove(name);

                return null;
            }

            return data?.GetValue(current.agent.GetInstanceID());
        }

        public void SetDynamic(string name, object value)
        {
            ExecutionContext current = ExecutionContext.current;

            ExecutableNode parent = ExecutableTree.current.nodes[current.node.parent];

            Debug.LogFormat("{0} {1}", parent.index, parent.breadth);

            if (!dynamicValues.ContainsKey(name))
                dynamicValues[name] = new EntryData(value.GetType(), parent.index, parent.breadth);

            dynamicValues[name].SetValue(current.agent.GetInstanceID(), value);
        }

        public object Get(BlackboardEntry entry)
        {
            values.TryGetValue(entry, out EntryData data);

            if (data == null)
                globalValues.TryGetValue(entry, out data);

            return data?.GetValue(ExecutionContext.current.agent.GetInstanceID());
        }

        public void Set(BlackboardEntry entry, object value)
        {
            values.TryGetValue(entry, out EntryData data);

            if (data == null)
                globalValues.TryGetValue(entry, out data);

            data?.SetValue(ExecutionContext.current.agent.GetInstanceID(), value);
        }

        internal class EntryData
        {
            private readonly object defaultValue;
            private readonly Dictionary<int, object> values;
            public Tuple<int, int> position = new(-1, -1);

            public EntryData(BlackboardEntry entry)
            {
                Type mapped = EntryType.GetMappedType(entry.type);

                defaultValue = mapped.IsValueType ? Activator.CreateInstance(mapped) : null;
                values = new Dictionary<int, object>();
            }

            public EntryData(Type defaultValueType, int index, int breadth)
            {
                defaultValue = defaultValueType.IsValueType ? Activator.CreateInstance(defaultValueType) : null;

                position = new Tuple<int, int>(index, breadth);
                values = new Dictionary<int, object>();
            }

            public object GetValue(int pid)
            {
                values.TryGetValue(pid, out object value);

                return value ?? defaultValue;
            }

            public void SetValue(int pid, object v)
            {
                values[pid] = v;
            }
        }
    }
}