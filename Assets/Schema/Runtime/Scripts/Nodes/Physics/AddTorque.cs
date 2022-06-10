﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Schema;

namespace Schema.Builtin.Nodes
{
    [DarkIcon("d_Rigidbody Icon", true)]
    [LightIcon("Rigidbody Icon", true)]
    [Description("Add a torque to a rigidbody")]
    [Category("Physics")]
    public class AddTorque : Action
    {
        public ComponentSelector<Rigidbody> rigidbody;
        public BlackboardEntrySelector<Vector3> torque;
        public ForceMode forceMode;
        public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
        {
            Rigidbody r = agent.GetComponent(rigidbody);

            if (r != null)
            {
                r.AddTorque(torque.value, forceMode);
                return NodeStatus.Success;
            }
            else
            {
                return NodeStatus.Failure;
            }
        }
    }
}