using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Schema.Builtin.Nodes
{
    // [DarkIcon("c_SphereCollider")]
    // [LightIcon("c_SphereCollider")]
    [Category("Physics")]
    [Description("Gets the closest point on a specified collider to a position")]
    public class ClosestPoint : Action
    {
        [Tooltip("The point that you want to find the closest location to")] public BlackboardEntrySelector<Vector3> point;
        [Tooltip("The collider you want to find the closest point on")] public ComponentSelector<Collider> collider;
        [Tooltip("Position of the collider")] public BlackboardEntrySelector<Vector3> position;
        [Tooltip("Rotation of the collider")] public BlackboardEntrySelector<Quaternion> rotation;
        [Tooltip("Where to store the closest point"), WriteOnly] public BlackboardEntrySelector<Vector3> target;
        public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
        {
            Collider c = agent.GetComponent(collider);

            Vector3 closestPoint = Physics.ClosestPoint(point.value, c, position.value, rotation.value);

            target.value = closestPoint;

            return NodeStatus.Success;
        }
    }
}