using System;
using System.Collections.Generic;
using Schema;
using UnityEngine;

[DarkIcon("Dark/Sequence")]
[LightIcon("Light/Sequence")]
[Description("Executes a series of nodes one after another")]
public class Sequence : Flow
{
    public override int Tick(NodeStatus status, int index)
    {
        if (index + 1 > children.Length - 1 || status == NodeStatus.Failure) return -1;
        else return index + 1;
    }
}