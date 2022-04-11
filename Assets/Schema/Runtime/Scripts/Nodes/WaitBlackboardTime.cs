﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Schema;

[DarkIcon("Dark/WaitBlackboardTime")]
[LightIcon("Light/WaitBlackboardTime")]
public class WaitBlackboardTime : Action
{
    class WaitBlackboardTimeMemory
    {
        public float startTime;
    }
    public BlackboardEntrySelector<float> number;
    public override void OnNodeEnter(object nodeMemory, SchemaAgent agent)
    {
        WaitBlackboardTimeMemory memory = (WaitBlackboardTimeMemory)nodeMemory;
        memory.startTime = Time.time;
    }
    public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
    {
        WaitBlackboardTimeMemory memory = (WaitBlackboardTimeMemory)nodeMemory;

        if (string.IsNullOrEmpty(number.entryID)) return NodeStatus.Failure;

        if (Time.time - memory.startTime >= (float)number.value)
        {
            return NodeStatus.Success;
        }
        else
        {
            return NodeStatus.Running;
        }
    }
}
