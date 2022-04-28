using UnityEngine;
using Schema;

public class Subtract : Action
{
    public BlackboardEntrySelector<float> valueOne;
    public BlackboardEntrySelector<float> valueTwo;
    [WriteOnly] public BlackboardEntrySelector<float> result;
    public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
    {
        float v1 = valueOne.value;
        float v2 = valueTwo.value;
        float r = v1 - v2;

        result.value = r;

        return NodeStatus.Success;
    }
}