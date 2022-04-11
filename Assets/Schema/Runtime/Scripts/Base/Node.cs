using UnityEngine;
using System.Linq;
using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;

namespace Schema
{
    /// <summary>
    /// Base class for all Schema nodes.
    /// </summary>

    [Serializable]
    public abstract class Node : ScriptableObject
    {
        /// <summary>
        /// The parent of this node
        /// </summary>
        public Node parent { get { return m_parent; } private set { m_parent = value; } }
        [SerializeField, HideInInspector] private Node m_parent;
        /// <summary>
        /// An array containing the children of this node
        /// </summary>
        public Node[] children { get { return m_children; } private set { m_children = value; } }
        [SerializeField, HideInInspector] private Node[] m_children = Array.Empty<Node>();
        /// <summary>
        /// An array containing the decorators for this node
        /// </summary>
        public Decorator[] decorators { get { return m_decorators; } private set { m_decorators = value; } }
        [SerializeField, HideInInspector] private Decorator[] m_decorators = Array.Empty<Decorator>();
        /// <summary>
        /// The GUID for the node
        /// </summary>
        public string uID { get { return m_uID; } private set { m_uID = value; } }
        [SerializeField, HideInInspector] private string m_uID;
        /// <summary>
        /// Position of the Node in the graph
        /// </summary>
        public Vector2 position { get { return m_position; } set { m_position = value; } }
        [SerializeField, HideInInspector] private Vector2 m_position;
        /// <summary>
        /// Priority for the node
        /// </summary>
        public int priority { get { return m_priority; } internal set { m_priority = value; } }
        [SerializeField, HideInInspector] private int m_priority;
        /// <summary>
        /// Graph for this node
        /// </summary>
        public Graph graph { get { return m_graph; } internal set { m_graph = value; } }
        [SerializeField, HideInInspector] private Graph m_graph;
        /// <summary>
        /// Comment for this node
        /// </summary>
        public string comment { get { return m_comment; } internal set { m_comment = value; } }
        [SerializeField, HideInInspector, TextArea] private string m_comment;
        /// <summary>
        /// Whether to allow the status indicator for this node in the editor
        /// </summary>
        public bool enableStatusIndicator { get { return m_enableStatusIndicator; } private set { m_enableStatusIndicator = value; } }
        [Tooltip("Toggle the status indicator for this node"), HideInInspector, SerializeField] private bool m_enableStatusIndicator;
        private string _description;
        private bool didGetDescriptionAttribute;
        /// <summary>
        /// Description for this node, given by the Description attribute
        /// </summary>
        public string description
        {
            get
            {
                if (!didGetDescriptionAttribute)
                {
                    didGetDescriptionAttribute = true;
                    _description = GetType().GetCustomAttribute<DescriptionAttribute>()?.description;
                }

                return _description;
            }
        }
        internal Type GetMemoryType()
        {
            Type[] types = GetType().GetTypeInfo().DeclaredNestedTypes.ToArray();

            if (types.Length == 0)
            {
                return null;
            }

            return types[0];
        }
        /// <summary>
        /// Determine whether the Node can have more children attached to it
        /// </summary>
        public bool CanHaveChildren()
        {
            return maxChildren > 0 && m_children.Length < maxChildren;
        }
        /// <summary>
        ///	Override to allow for Gizmo visualization in the scene view. This will be called only for the currently selected SchemaAgent. 
        /// </summary>
        public virtual void DrawGizmos(SchemaAgent agent) { }
        /// <summary>
        /// Whether a parent node is allowed for this node:w
        /// </summary>
        public virtual bool canHaveParent { get { return true; } }
        /// <summary>
        /// The maximum allowed number of children for this node
        /// </summary>
        public virtual int maxChildren { get { return Int32.MaxValue; } }
        void OnEnable()
        {
            if (String.IsNullOrEmpty(name))
                name = String.Concat(this.GetType().Name.Select(x => Char.IsUpper(x) ? " " + x : x.ToString())).TrimStart(' ');

            if (string.IsNullOrEmpty(uID)) uID = Guid.NewGuid().ToString("N");
        }
        /// <summary>
        /// Add a connection to another node
        /// </summary>
        /// <param name="to">Node to connect to</param>
        /// <param name="actionName">Name of the undo action</param>
        /// <param name="undo">Whether to register this operation in the undo stack</param>
        public void AddConnection(Node to, string actionName = "Add Connection", bool undo = true)
        {
            if (undo)
            {
                Undo.RegisterCompleteObjectUndo(this, actionName);
                Undo.RegisterCompleteObjectUndo(to, actionName);
            }

            if (!m_children.Contains(to))
                ArrayUtility.Add(ref m_children, to);
            to.parent = this;
        }
        /// <summary>
        /// Disconnect from another child node
        /// </summary>
        /// <param name="from">Node to disconnect from. Must be a child of this node</param>
        /// <param name="actionName">Name of the undo action</param>
        /// <param name="undo">Whether to register this operation in the undo stack</param>
        public void RemoveConnection(Node from, string actionName = "Remove Connection", bool undo = true)
        {
            if (undo)
            {
                Undo.RegisterCompleteObjectUndo(this, actionName);
                Undo.RegisterCompleteObjectUndo(from, actionName);
            }

            if (m_children.Contains(from))
                ArrayUtility.Remove(ref m_children, from);
            from.parent = null;
        }
        /// <summary>
        /// Remove the connection between this node and its parent
        /// </summary>
        /// <param name="actionName">Name of the undo action</param>
        /// <param name="undo">Whether to register this operation in the undo stack</param>
        public void RemoveParent(string actionName = "Remove Parent Connection", bool undo = true)
        {
            if (undo)
            {
                Undo.RegisterCompleteObjectUndo(this, actionName);
                Undo.RegisterCompleteObjectUndo(parent, actionName);
            }

            parent.RemoveConnection(this, actionName, undo);
        }
        /// <summary>
        /// Remove connections between this node and its children
        /// </summary>
        /// <param name="actionName">Name of the undo action</param>
        /// <param name="undo">Whether to register this operation in the undo stack</param>
        public void RemoveChildren(string actionName = "Remove Child Connections", bool undo = true)
        {
            if (undo)
            {
                Undo.RegisterCompleteObjectUndo(this, actionName);

                foreach (Node child in children)
                    Undo.RegisterCompleteObjectUndo(child, actionName);
            }

            foreach (Node child in children)
                RemoveConnection(child, actionName, undo);
        }
        /// <summary>
        /// Breaks connections between this node and its parents and children
        /// </summary>
        public void BreakConnections()
        {
            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();

            parent.RemoveConnection(this, actionName: "");

            foreach (Node child in children)
                RemoveConnection(child, actionName: "");

            Undo.SetCurrentGroupName("Break Connections");
            Undo.CollapseUndoOperations(groupIndex);
        }
        /// <summary>
        /// Add a decorator to this node
        /// </summary>
        /// <param name="decoratorType">Type of decorator to add. Must inherit from type Decorator</param>
        /// <param name="undo">Whether to register this operation in the undo stack</param>
        /// <returns>Created decorator</returns>
        /// <exception cref="ArgumentException">decoratorType does not inherit from Decorator</exception>
        public Decorator AddDecorator(Type decoratorType, bool undo = true)
        {
            if (!typeof(Decorator).IsAssignableFrom(decoratorType))
                throw new ArgumentException("decoratorType does not inherit from type Node");

            Decorator decorator = (Decorator)ScriptableObject.CreateInstance(decoratorType);
            decorator.hideFlags = HideFlags.HideInHierarchy;
            decorator.node = this;

            string path = AssetDatabase.GetAssetPath(this);

            if (!String.IsNullOrEmpty(path))
                AssetDatabase.AddObjectToAsset(decorator, path);

            if (undo)
            {
                Undo.RegisterCreatedObjectUndo(decorator, "Decorator Created");
                Undo.RegisterCompleteObjectUndo(this, "Decorator Added");
                ArrayUtility.Add(ref m_decorators, decorator);
            }
            else
            {
                ArrayUtility.Add(ref m_decorators, decorator);
            }

            return decorator;
        }
        public void RemoveDecorator(Decorator decorator, string actionName = "Remove Decorator", bool undo = true)
        {
            if (!ArrayUtility.Contains(m_decorators, decorator))
            {
                Debug.LogWarning($"Decorator {decorator.name} does not exit on node {name}");
                return;
            }

            if (undo)
            {
                Undo.RegisterCompleteObjectUndo(this, actionName);
                ArrayUtility.Remove(ref m_decorators, decorator);
                Undo.DestroyObjectImmediate(decorator);
            }
            else
            {
                ArrayUtility.Remove(ref m_decorators, decorator);
                ScriptableObject.DestroyImmediate(decorator, true);
            }
        }
        /// <summary>
        /// The current errors for this node
        /// </summary>
        /// <returns>A list of errors to display in the editor</returns>
        public virtual List<Error> GetErrors() { return new List<Error>(); }
        /// <summary>
        /// Where Schema should load the dark mode icon within a resources folder
        /// </summary>
        [System.AttributeUsage(AttributeTargets.Class)]
        protected class DarkIconAttribute : System.Attribute
        {
            public string location;
            /// <summary>
            /// Where Schema should load the dark mode icon within a resources folder
            /// </summary>
            /// <param name="location">Location of the icon to be loaded with Resources.Load</param>
            public DarkIconAttribute(string location)
            {
                this.location = location;
            }
        }
        /// <summary>
        /// Where Schema should load the light mode icon within a resources folder
        /// </summary>
        [System.AttributeUsage(AttributeTargets.Class)]
        protected class LightIconAttribute : System.Attribute
        {
            public string location;
            /// <summary>
            /// Where Schema should load the light mode icon within a resources folder
            /// </summary>
            /// <param name="location">Location of the icon to be loaded with Resources.Load</param>
            public LightIconAttribute(string location)
            {
                this.location = location;
            }
        }
        /// <summary>
        /// Attribute for adding a description to a node in the Editor
        /// </summary>
        [System.AttributeUsage(AttributeTargets.Class)]
        protected class DescriptionAttribute : System.Attribute
        {
            public string description;
            /// <summary>
            /// Attribute for adding a description to a node in the Editor
            /// </summary>
            /// <param name="description">Description for the node</param>
            public DescriptionAttribute(string description)
            {
                this.description = description;
            }
        }
#if UNITY_EDITOR
        private Texture2D _icon;
        private string _darkIconLocation;
        private string _lightIconLocation;
        /// <summary>
        /// Icon of the node (editor only)
        /// </summary>
        public Texture2D icon
        {
            get
            {
                if (String.IsNullOrEmpty(_darkIconLocation))
                {
                    DarkIconAttribute attribute = (DarkIconAttribute)Attribute.GetCustomAttribute(GetType(), typeof(DarkIconAttribute));

                    if (attribute == null)
                        _darkIconLocation = "NOT FOUND";
                    else
                        _darkIconLocation = attribute.location;
                }
                if (String.IsNullOrEmpty(_lightIconLocation))
                {
                    LightIconAttribute attribute = (LightIconAttribute)Attribute.GetCustomAttribute(GetType(), typeof(LightIconAttribute));

                    if (attribute == null)
                        _lightIconLocation = "NOT FOUND";
                    else
                        _lightIconLocation = attribute.location;
                }

                //if icon is null or the skin has changed
                if (_icon == null || usingProTextures != EditorGUIUtility.isProSkin)
                {
                    //Use dark texture
                    if (EditorGUIUtility.isProSkin && !String.IsNullOrEmpty(_darkIconLocation) && !_darkIconLocation.Equals("NOT FOUND"))
                    {
                        _icon = Resources.Load<Texture2D>(_darkIconLocation);
                    }
                    else if (!String.IsNullOrEmpty(_lightIconLocation) && !_lightIconLocation.Equals("NOT FOUND"))
                    {
                        _icon = Resources.Load<Texture2D>(_lightIconLocation);
                    }

                    usingProTextures = EditorGUIUtility.isProSkin;
                }

                return _icon;
            }
        }
        private bool usingProTextures;
#endif
    }
}