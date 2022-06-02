using System;
using System.Collections.Generic;
using System.Linq;
using Schema.Builtin.Nodes;
using UnityEngine;
using UnityEditor;

namespace SchemaEditor.Editors.Nodes
{
    [CustomEditor(typeof(VectorAngle)), CanEditMultipleObjects]
    public class VectorAngleEditor : Editor
    {
        SerializedProperty a;
        SerializedProperty b;
        SerializedProperty signed;
        SerializedProperty axis;
        SerializedProperty angle;
        SerializedProperty overrideAxis;
        SerializedProperty dir;
        void OnEnable()
        {
            a = serializedObject.FindProperty("vectorOne");
            b = serializedObject.FindProperty("vectorTwo");
            signed = serializedObject.FindProperty("signed");
            axis = serializedObject.FindProperty("axis");
            angle = serializedObject.FindProperty("angle");
            overrideAxis = serializedObject.FindProperty("overrideAxis");
            dir = serializedObject.FindProperty("direction");
        }
        public override void OnInspectorGUI()
        {
            VectorAngle vectorAngle = (VectorAngle)target;

            serializedObject.Update();

            EditorGUILayout.PropertyField(a);
            EditorGUILayout.PropertyField(b);

            EditorGUILayout.PropertyField(signed);

            if (vectorAngle.vectorOne.entryType == typeof(Vector3) ||
                vectorAngle.vectorTwo.entryType == typeof(Vector3))
            {
            }

            if (signed.boolValue &&
                (vectorAngle.vectorOne.entryType == typeof(Vector3) ||
                vectorAngle.vectorTwo.entryType == typeof(Vector3))
            )
            {
                EditorGUILayout.PropertyField(overrideAxis);

                if (overrideAxis.boolValue)
                    EditorGUILayout.PropertyField(axis);
                else
                    EditorGUILayout.PropertyField(dir);
            }

            EditorGUILayout.PropertyField(angle);

            serializedObject.ApplyModifiedProperties();
        }
    }
}