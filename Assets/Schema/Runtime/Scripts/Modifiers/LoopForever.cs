using System;
using UnityEngine;

namespace Schema.Builtin.Modifiers
{
    [AllowOne]
    [DisableIfTypes(typeof(Loop), typeof(LoopUntil))]
    [DarkIcon("Modifiers/LoopForever")]
    public class LoopForever : Modifier
    {
        public override Message Modify(object modifierMemory, SchemaAgent agent, NodeStatus status)
        {
            return Message.Repeat;
        }
        public enum ForcedStatus
        {
            Success,
            Failure
        }
    }
}