using Schema;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Schema.Builtin.Conditionals
{
    [DarkIcon("Conditionals/IsNull")]
    [LightIcon("Conditionals/IsNull")]
    public class IsNull : Conditional
    {
        [Tooltip("Entry to check for null")] public BlackboardEntrySelector entry = new BlackboardEntrySelector();
        class IsNullMemory
        {
            public bool doReturn;
        }
        protected override void OnObjectEnable()
        {
            entry.ApplyAllFilters();
        }
        public override void OnInitialize(object decoratorMemory, SchemaAgent agent)
        {
            IsNullMemory memory = (IsNullMemory)decoratorMemory;

            memory.doReturn = entry.entry.type.IsValueType;

            Debug.Log(entry.entry.type);
        }
        public override bool Evaluate(object decoratorMemory, SchemaAgent agent)
        {
            IsNullMemory memory = (IsNullMemory)decoratorMemory;

            if (memory.doReturn)
                return true;

            return entry.value != null;
        }
        public override GUIContent GetConditionalContent()
        {
            StringBuilder sb = new StringBuilder();

            if (entry.isDynamic)
                sb.Append("If dynamic variable ");
            else
                sb.Append("If variable ");

            sb.AppendFormat("<color=red>${0}</color> ", String.IsNullOrEmpty(entry.name) ? "null" : entry.name);

            if (invert)
                sb.Append("is not null");
            else
                sb.Append("is null");

            return new GUIContent(sb.ToString());
        }
    }
}