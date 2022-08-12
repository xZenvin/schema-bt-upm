﻿using UnityEngine;

namespace Schema.Builtin.Nodes
{
    [LightIcon("Light/Wait")]
    [DarkIcon("Dark/Wait")]
    [Description("Waits a given number of seconds, then resumes execution of the Behavior Tree")]
    public class Wait : Action
    {
        public BlackboardEntrySelector<float> seconds = new(0.001f);

        private void OnValidate()
        {
            seconds.inspectorValue = seconds.inspectorValue > 0.001f ? seconds.inspectorValue : 0.001f;
        }

        public override void OnNodeEnter(object nodeMemory, SchemaAgent agent)
        {
            WaitMemory mem = (WaitMemory)nodeMemory;
            mem.startTime = Time.time;
        }

        public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
        {
            WaitMemory memory = (WaitMemory)nodeMemory;

            if (Time.time - memory.startTime >= seconds.value)
                return NodeStatus.Success;
            return NodeStatus.Running;
        }

        private class WaitMemory
        {
            public float startTime;
        }
    }
}