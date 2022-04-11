using UnityEngine;
using Schema;

namespace Schema
{
    public abstract class Flow : Node
    {
        public virtual void OnInitialize(object flowMemory, SchemaAgent agent) { }
        public virtual void OnNodeEnter(object flowMemory, SchemaAgent agent) { }
        public virtual void OnNodeExit(object flowMemory, SchemaAgent agent) { }
        public abstract int Tick(NodeStatus status, int index);
    }
}