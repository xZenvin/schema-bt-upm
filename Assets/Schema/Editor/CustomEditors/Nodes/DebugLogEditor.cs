using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace SchemaEditor.Editors.Nodes
{
    [CustomEditor(typeof(Schema.Builtin.Nodes.DebugLog)), CanEditMultipleObjects]
    public class DebugLogEditor : Editor
    {
        SerializedProperty message;
        GUIStyle boxStyle;
        void OnEnable()
        {
            message = serializedObject.FindProperty("message");
        }
        public override void OnInspectorGUI()
        {
            Schema.Builtin.Nodes.DebugLog debugLog = (Schema.Builtin.Nodes.DebugLog)target;

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(EditorStyles.helpBox);
                boxStyle.richText = true;
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(message);

            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(message.stringValue, boxStyle);

            serializedObject.ApplyModifiedProperties();
        }
    }
}