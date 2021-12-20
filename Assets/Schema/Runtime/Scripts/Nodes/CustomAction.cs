using UnityEngine;
using UnityEngine.Events;
using Schema.Runtime;

[DarkIcon("Dark/CustomAction")]
[LightIcon("Light/CustomAction")]
public class CustomAction : Action
{
    [System.Serializable]
    private class NodeAction : UnityEvent<SchemaAgent> { }
    [SerializeField] private NodeAction customAction;
    public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
    {
        customAction.Invoke(agent);
        return NodeStatus.Success;
    }
}
