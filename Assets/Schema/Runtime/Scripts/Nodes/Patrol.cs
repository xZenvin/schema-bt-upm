using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using Schema.Runtime;

[Category("Movement")]
[RequireAgentComponent(typeof(PatrolRoute))]
[DarkIcon("Dark/Patrol")]
[LightIcon("Light/Patrol")]
public class Patrol : Action
{
    class PatrolMemory
    {
        public NavMeshAgent agent;
        public PatrolRoute route;
        public int currentIndex = 0;
    }
    public float speed;
    public float acceptableRadius;
    public override void OnInitialize(object nodeMemory, SchemaAgent agent)
    {
        PatrolMemory memory = (PatrolMemory)nodeMemory;

        memory.route = agent.GetComponent<PatrolRoute>();
        memory.agent = agent.GetComponent<NavMeshAgent>();
    }
    public override void OnNodeEnter(object nodeMemory, SchemaAgent agent)
    {
        PatrolMemory memory = (PatrolMemory)nodeMemory;

        memory.agent.SetDestination(memory.route.points[memory.currentIndex]);
    }
    public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
    {
        PatrolMemory memory = (PatrolMemory)nodeMemory;

        memory.agent.speed = speed;
        memory.agent.stoppingDistance = acceptableRadius;

        if (!memory.agent.pathPending &&
            (memory.agent.remainingDistance <= memory.agent.stoppingDistance) &&
            (!memory.agent.hasPath || memory.agent.velocity.sqrMagnitude == 0f))
        {
            memory.currentIndex++;
            memory.currentIndex = memory.currentIndex > memory.route.points.Count - 1 ? 0 : memory.currentIndex;

            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }
}
