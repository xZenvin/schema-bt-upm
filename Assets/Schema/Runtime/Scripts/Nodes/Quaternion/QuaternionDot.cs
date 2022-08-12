using UnityEngine;

namespace Schema.Builtin.Nodes
{
    [DarkIcon("d_Transform Icon", true)]
    [LightIcon("Transform Icon", true)]
    [Description("Get the dot product of two quaternion rotations")]
    public class QuaternionDot : Action
    {
        [Tooltip("Quaternion A")] public BlackboardEntrySelector<Quaternion> quaternionOne;

        [Tooltip("Quaternion B")] public BlackboardEntrySelector<Quaternion> quaternionTwo;

        [Tooltip("Blackboard variable to store the dot product in")] [WriteOnly]
        public BlackboardEntrySelector<float> product;

        public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
        {
            product.value = Quaternion.Dot(quaternionOne.value, quaternionTwo.value);

            return NodeStatus.Success;
        }
    }
}