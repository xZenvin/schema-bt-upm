
using UnityEngine;
using Schema;
using System.Collections.Generic;
using System.Linq;

namespace Schema.Builtin.Nodes
{
    [DarkIcon("d_math")]
    [LightIcon("math")]
    [Category("Math")]
    [Description("Get the sine of an angle")]
    public class Sin : Action
    {
        [Tooltip("Input for the sine function")] public BlackboardEntrySelector<float> value;
        [Tooltip("Selector to store the sine in"), WriteOnly] public BlackboardEntrySelector<float> result;
        [Tooltip("Input is degrees instead of radians")] public bool degrees;
        public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
        {
            float angle = degrees ? value.value * Mathf.Deg2Rad : value.value;

            result.value = Mathf.Sin(angle);

            return NodeStatus.Success;
        }
    }
}