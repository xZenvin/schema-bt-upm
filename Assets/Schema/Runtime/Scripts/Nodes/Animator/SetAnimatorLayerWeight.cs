﻿using Schema;
using UnityEngine;

[DarkIcon("c_Animator")]
[LightIcon("c_Animator")]
public class SetAnimatorLayerWeight : Action
{
    [Tooltip("The index of the layer")] [Min(0)]
    public int layerIndex;

    [Tooltip("The weight of the layer to set")] [Range(0f, 1f)]
    public float layerWeight;

    public override void OnInitialize(object nodeMemory, SchemaAgent agent)
    {
        SetAnimatorLayerWeightMemory memory = (SetAnimatorLayerWeightMemory)nodeMemory;

        memory.animator = agent.GetComponent<Animator>();
    }

    public override NodeStatus Tick(object nodeMemory, SchemaAgent agent)
    {
        SetAnimatorLayerWeightMemory memory = (SetAnimatorLayerWeightMemory)nodeMemory;

        memory.animator.SetLayerWeight(layerIndex, layerWeight);

        return NodeStatus.Success;
    }

    private class SetAnimatorLayerWeightMemory
    {
        public Animator animator;
    }
}