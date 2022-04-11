using System;
using System.Collections.Generic;
using Schema;
using UnityEngine;

[DarkIcon("Dark/Selector")]
[LightIcon("Light/Selector")]
public class Selector : Flow
{
    public override int Tick(NodeStatus status, int index)
    {
        if (index + 1 > children.Length - 1 || status == NodeStatus.Success) return -1;

        return index + 1;
    }
}
