using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Schema;
using Schema.Internal;
using Schema.Utilities;
using SchemaEditor.Editors;
using SchemaEditor.Internal;
using SchemaEditor.Internal.ComponentSystem;
using SchemaEditor.Internal.ComponentSystem.Components;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SchemaEditor
{
    public partial class NodeEditor
    {
        public Rect window;
        private SchemaAgent activeAgent;
        private Editor blackboardEditor;
        public ComponentCanvas canvas;
        private Editor defaultDecoratorEditor;
        private Editor defaultNodeEditor;
        private List<Type> distinctTypes;
        private bool drawBox;
        private bool editingPaused;
        private Editor editor;
        private Func<bool> isDockedFunc;
        private bool needsPan;

        private Matrix4x4 prevMatrix;

        //Focus of the search box needs to be delayed by one frame, or else the keyboard shortcut triggering the search box will type in the box
        private bool searchWantsFocus;
        private bool shouldFocusSearch;
        public float tabHeight => isDocked() ? 19.0f : 21.0f;
        private Func<bool> isDocked => isDockedFunc ??= this.GetIsDockedDelegate();

        private void OnGUI()
        {
            windowInfo.mousePosition = Event.current.mousePosition;

            CalculateWindow();

            RebuildComponentTree();

            canvas.Draw();

            if (target != null && canvas != null)
            {
                CreateEditors();

                DrawInspector();
                DrawToolbar();

                Blackboard.instance = target.blackboard;
            }

            if (windowInfo != null && Event.current.type == EventType.Repaint)
                windowInfo.timeLastFrame = Time.realtimeSinceStartup;

            Repaint();
        }

        private void CreateEditors()
        {
            if (blackboardEditor == null)
                blackboardEditor = Editor.CreateEditor(target.blackboard, typeof(BlackboardEditor));

            List<Object> targets = new List<Object>();

            IEnumerable<Object> editableComponents;

            if (canvas == null)
            {
                editableComponents = Enumerable.Empty<Object>();
            }
            else
            {
                editableComponents = canvas.selected
                    .Where(x => x is IEditable)
                    .Cast<IEditable>()
                    .Where(x => x.IsEditable())
                    .Select(x => x.GetEditable());
            }

            targets = editableComponents.Where(x => x != null).ToList();
            distinctTypes = targets.Select(x => x.GetType()).Distinct().ToList();

            if (distinctTypes.Count > 1) return;

            if (editor == null)
            {
                if (targets.Count > 0)
                    editor = Editor.CreateEditor(targets.ToArray());
            }
            else if (!editor.targets.SequenceEqual(targets))
            {
                DestroyImmediate(editor);

                if (targets.Count > 0)
                    editor = Editor.CreateEditor(targets.ToArray());
            }

            if (targets.All(x => x is Node))
            {
                if (defaultNodeEditor == null)
                {
                    if (targets.Count > 0)
                        defaultNodeEditor = Editor.CreateEditor(targets.ToArray(), typeof(DefaultNodeEditor));
                }
                else if (!defaultNodeEditor.targets.SequenceEqual(targets))
                {
                    DestroyImmediate(defaultNodeEditor);

                    if (targets.Count > 0)
                        defaultNodeEditor = Editor.CreateEditor(targets.ToArray(), typeof(DefaultNodeEditor));
                }
            }
        }

        // ///<summary>
        // ///Draws links between nodes and their children and parents
        // ///</summary>
        // private void DrawConnections()
        // {
        //     BeginZoomed(window, windowInfo.zoom, tabHeight);

        //     Vector2 mousePos = Event.current.mousePosition;

        //     Rect windowGrid = WindowToGridRect(window);

        //     foreach (Node node in target.nodes)
        //     {
        //         Vector2 nodeSize = GetArea(node, false);

        //         foreach (Node child in node.children)
        //         {
        //             if (orphanNode == child) continue;

        //             Vector2 childSize = GetArea(child, false);

        //             Vector2 gridFrom = new Vector2(
        //                 node.graphPosition.x + (nodeSize.x + GUIData.nodePadding * 2) * windowInfo.zoom / 2f,
        //                 node.graphPosition.y + (nodeSize.y + GUIData.nodePadding * 2 + 9f) * windowInfo.zoom);
        //             Vector2 gridTo = new Vector2(
        //                 child.graphPosition.x + (childSize.x + GUIData.nodePadding * 2) * windowInfo.zoom / 2f,
        //                 child.graphPosition.y - 8f * windowInfo.zoom);

        //             Vector2 from = GridToWindowPositionNoClipped(new Vector2(
        //                 node.graphPosition.x + (nodeSize.x + GUIData.nodePadding * 2) / 2f,
        //                 node.graphPosition.y + nodeSize.y + GUIData.nodePadding * 2 + 9f));
        //             Vector2 to = GridToWindowPositionNoClipped(new Vector2(
        //                 child.graphPosition.x + (childSize.x + GUIData.nodePadding * 2) / 2f,
        //                 child.graphPosition.y - 8f));

        //             if (!windowGrid.Contains(gridFrom) && !windowGrid.Contains(gridTo))
        //                 continue;

        //             Vector2 p0 = from;
        //             Vector2 p1 = from + Vector2.up * 50f;
        //             Vector2 p2 = to - Vector2.up * 50f;
        //             Vector2 p3 = to;

        //             Bezier bezier = new Bezier(p0, p1, p2, p3);

        //             // Node active = activeAgent?.GetRunningNode();
        //             Node active = null;

        //             bool isActiveConnection = (EditorApplication.isPlaying
        //                 || EditorApplication.isPaused)
        //                 && active != null
        //                 && IsSubTreeOf(node, active)
        //                 && (child == active || IsSubTreeOf(child, active));

        //             bool intersect = false;

        //             Node selected = null;

        //             if (windowInfo.selected.Count == 1)
        //                 selected = windowInfo.selected[0];

        //             if (selected != null && selected.CanHaveParent() && selected.CanHaveChildren() && selected.parent != node && selected != node)
        //                 intersect = bezier.Intersect(
        //                     new Rect(
        //                         GridToWindowPositionNoClipped(selected.graphPosition),
        //                         GetAreaWithPadding(selected, false)
        //                     )
        //                 );

        //             if (intersect && windowInfo.hoveredConnection != child && windowInfo.shouldCheckConnectionHover)
        //                 windowInfo.hoveredConnection = child;
        //             else if (!intersect && windowInfo.hoveredConnection == child && windowInfo.shouldCheckConnectionHover)
        //                 windowInfo.hoveredConnection = null;

        //             Color connectionColor = isActiveConnection ? Prefs.highlightColor : (windowInfo.hoveredConnection == child ? Prefs.selectionColor : Prefs.connectionColor);

        //             Handles.DrawBezier(p0, p3, p1, p2, connectionColor, Styles.curve, 5f * windowInfo.zoom);

        //             // Handles.DrawBezier(
        //             //     p0,
        //             //     p3,
        //             //     p1,
        //             //     p2,
        //             //     isActiveConnection ? NodeEditorPrefs.highlightColor : (windowInfo.hoveredConnection == child ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f)),
        //             //     null,
        //             //     5f * windowInfo.zoom
        //             // );

        //             if (isActiveConnection)
        //             {
        //                 const float fac = 1.5f;
        //                 const int points = 4;

        //                 for (int i = 0; i < points; i++)
        //                 {
        //                     float t = (float)((EditorApplication.timeSinceStartup % fac) / fac);
        //                     t += 1f / (float)points * i;
        //                     t = t > 1 ? t % 1 : t;
        //                     Vector2 p = bezier.Position(t);

        //                     GUI.color = Prefs.highlightColor;
        //                     GUI.DrawTexture(new Rect(p.x - 4f * windowInfo.zoom, p.y - 4f * windowInfo.zoom, 8f * windowInfo.zoom, 8f * windowInfo.zoom), Icons.GetResource("round", false));
        //                 }
        //             }

        //             GUI.color = Color.white;
        //         }
        //     }

        //     if (requestingConnection != null)
        //     {
        //         Node node = requestingConnection;
        //         Vector2 nodeSize = GetArea(node, false);

        //         Vector2 from = GridToWindowPositionNoClipped(new Vector2(
        //             node.graphPosition.x + (nodeSize.x + GUIData.nodePadding * 2) / 2f,
        //             node.graphPosition.y + nodeSize.y + GUIData.nodePadding * 2 + 9f));
        //         Vector2 to = mousePos;

        //         Handles.DrawBezier(
        //             from,
        //             to,
        //             from + Vector2.up * 50f,
        //             to - Vector2.up * 50f,
        //             Color.white,
        //             null,
        //             3f * windowInfo.zoom
        //         );
        //     }
        //     EndZoomed();
        // }
        // private void DrawNodes()
        // {
        //     Event current = Event.current;

        //     //these will be overriden later if the nodes, decorators, or inspector contains the mouse position
        //     if (window.Contains(current.mousePosition) && IsNotLayoutEvent(current))
        //         windowInfo.hoveredType = Window.Hovering.Window;
        //     else if (IsNotLayoutEvent(current))
        //         windowInfo.hoveredType = Window.Hovering.None;

        //     BeginZoomed(window, windowInfo.zoom, tabHeight);

        //     Rect windowGridView = WindowToGridRect(window);

        //     List<Node> nodes = target.nodes.ToList();

        //     if (nodes != null)
        //     {
        //         Vector2 boxStartPos = GridToWindowPositionNoClipped(windowInfo.mouseDownPos);
        //         Vector2 boxSize = current.mousePosition - boxStartPos;
        //         if (boxSize.x < 0) { boxStartPos.x += boxSize.x; boxSize.x = Mathf.Abs(boxSize.x); }
        //         if (boxSize.y < 0) { boxStartPos.y += boxSize.y; boxSize.y = Mathf.Abs(boxSize.y); }
        //         Rect selectionBox = new Rect(boxStartPos, boxSize);

        //         List<Node> selectionQueue = new List<Node>();

        //         bool didHoverDecoratorThisFrame = false;

        //         if (activeAgent != null)
        //         {
        //             foreach (Node node in activeAgent.GetCalledNodes())
        //                 NodeTicked(node);

        //             windowInfo.nodeStatus = activeAgent.GetNodeStatus();
        //         }

        //         for (int i = nodes.Count - 1; i >= 0; i--)
        //         {
        //             Node node = nodes[i];
        //             Vector2 positionNoClipped = GridToWindowPositionNoClipped(node.graphPosition);
        //             Vector2 size = GetArea(node, false);
        //             Vector2 sizeWithPadding = GetAreaWithPadding(node, false);

        //             Rect contained = new Rect(positionNoClipped.x + GUIData.nodePadding, positionNoClipped.y + GUIData.nodePadding, size.x, size.y);
        //             Rect rect = new Rect(positionNoClipped, new Vector2(sizeWithPadding.x, sizeWithPadding.y));

        //             if (
        //                 !windowGridView.Overlaps(
        //                     new Rect(
        //                         node.graphPosition - Vector2.one * GUIData.nodePadding,
        //                         (sizeWithPadding + Vector2.one * GUIData.nodePadding * 2) * windowInfo.zoom)
        //                     )
        //                 )
        //                 continue;

        //             if (node.CanHaveParent())
        //             {
        //                 // float width = size.x - GUIData.nodePadding * 2;
        //                 float width = 24f;
        //                 Rect inConnection = new Rect(positionNoClipped.x + GUIData.nodePadding + size.x / 2f - width / 2f, positionNoClipped.y - width / 2f, width, 24f);
        //                 Rect inConnectionHover = new Rect(inConnection.position.x, inConnection.position.y, inConnection.size.x, inConnection.size.y / 2f);

        //                 if (inConnectionHover.Contains(current.mousePosition) && IsNotLayoutEvent(current))
        //                 {
        //                     windowInfo.hoveredType = Window.Hovering.InConnection;
        //                     windowInfo.hoveredNode = node;
        //                 }

        //                 if (windowInfo.hoveredType == Window.Hovering.InConnection && windowInfo.hoveredNode == node && !drawBox)
        //                     GUI.color = Color.white;
        //                 else
        //                     GUI.color = Prefs.portColor;

        //                 GUI.DrawTexture(inConnection, Styles.circle);

        //                 GUI.color = Color.white;

        //                 // GUI.Box(inConnection, "", Styles.styles.decorator);
        //             }

        //             // if (node.maxChildren > 0)
        //             // {
        //             //     float width = rect.width - GUIData.nodePadding * 3f;
        //             //     Rect outConnection = new Rect(rect.x + rect.width / 2f - width / 2f, rect.yMax - 9f, width, 18f);
        //             //     Rect outConnectionHover = new Rect(outConnection.x, outConnection.y + outConnection.height / 2f, outConnection.width, outConnection.height / 2f);

        //             //     if (outConnectionHover.Contains(current.mousePosition) && IsNotLayoutEvent(current))
        //             //     {

        //             //         windowInfo.hoveredType = Window.Hovering.OutConnection;
        //             //         windowInfo.hoveredNode = node;
        //             //     }

        //             //     if (windowInfo.hoveredType == Window.Hovering.OutConnection && windowInfo.hoveredNode == node && !drawBox)
        //             //         GUI.color = Color.white;
        //             //     else
        //             //         GUI.color = NodeEditorPrefs.portColor;

        //             //     GUI.Box(outConnection, "", Styles.styles.decorator);
        //             // }

        //             GUI.color = Styles.windowBackground;

        //             GUI.Box(rect.Pad(new RectOffset(-14, -14, -14, -14)), "", Styles.shadow);

        //             if (windowInfo.selected.Contains(node))
        //                 GUI.color = Prefs.selectionColor;
        //             else if (windowInfo.alpha.ContainsKey(node.uID))
        //                 GUI.color = Color.Lerp(new Color32(80, 80, 80, 255), Prefs.highlightColor, windowInfo.alpha[node.uID]);
        //             else if (windowInfo.selectedDecorator &&
        //                 node.priority > 0 &&
        //                 windowInfo.selectedDecorator.node.priority > 0 &&
        //                 (windowInfo.selectedDecorator.abortsType == Decorator.ObserverAborts.Self ||
        //                 windowInfo.selectedDecorator.abortsType == Decorator.ObserverAborts.Both
        //                 ) &&
        //                 IsSubTreeOf(windowInfo.selectedDecorator.node, node))
        //                 GUI.color = Styles.selfColor;
        //             else if (windowInfo.selectedDecorator &&
        //                 node.priority > 0 &&
        //                 windowInfo.selectedDecorator.node.priority > 0 &&
        //                 (windowInfo.selectedDecorator.abortsType == Decorator.ObserverAborts.LowerPriority ||
        //                 windowInfo.selectedDecorator.abortsType == Decorator.ObserverAborts.Both
        //                 ) &&
        //                 IsLowerPriority(windowInfo.selectedDecorator.node, node) &&
        //                 windowInfo.selectedDecorator.node.priority < node.priority)
        //                 GUI.color = Styles.lowerPriorityColor;
        //             else
        //                 GUI.color = EditorGUIUtility.isProSkin ? new Color32(80, 80, 80, 255) : new Color32(176, 176, 176, 255);

        //             GUI.Box(rect, "", Styles.styles.nodeSelected);

        //             GUI.color = Color.white;

        //             bool blocked = false;

        //             if (node.priority > 0)
        //             {
        //                 Handles.color = Styles.windowAccent;
        //                 Vector3[] circle = HelperMethods.Circle(new Vector2(rect.x, rect.center.y), 15f, 16);
        //                 Handles.DrawAAConvexPolygon(circle);
        //                 Handles.color = Color.white;

        //                 GUI.Label(new Rect(rect.x - 15f, rect.center.y - 15f, 30f, 30f), node.priority.ToString(), Styles.styles.title);
        //             }

        //             GUIContent error = GetErrors(node);

        //             if (error != GUIContent.none)
        //             {
        //                 float iconWidth = error.image.width;
        //                 float iconHeight = error.image.height;
        //                 GUI.Label(new Rect(rect.x + rect.width - iconWidth / 2f, rect.y + rect.height - iconHeight / 2f, iconWidth, iconHeight), error, GUIStyle.none);
        //             }

        //             if (Prefs.enableStatusIndicators && node.enableStatusIndicator && windowInfo.nodeStatus != null && Application.isPlaying && windowInfo.nodeStatus.ContainsKey(node.uID))
        //             {
        //                 float iconSize = 32f;

        //                 bool? nodeStatus = windowInfo.nodeStatus[node.uID];
        //                 if (nodeStatus == true)
        //                     GUI.color = Prefs.successColor;
        //                 else if (nodeStatus == false)
        //                     GUI.color = Prefs.failureColor;

        //                 GUI.Label(new Rect(rect.x + rect.width - iconSize / 2f, rect.y - iconSize / 2f, iconSize, iconSize),
        //                     "",
        //                     Styles.styles.decorator);
        //             }

        //             GUI.color = Color.white;

        //             GUILayout.BeginArea(contained);
        //             GUILayout.BeginVertical();

        //             List<float> positions = new List<float>();

        //             for (int j = 0; j < node.decorators.Length; j++)
        //             {
        //                 Decorator d = node.decorators[j];

        //                 if (d == null)
        //                     continue;

        //                 bool isSelected = windowInfo.selectedDecorator == d;

        //                 GUI.color = isSelected ? new Color(.6f, .6f, .1f, 1f) : new Color(.1f, .1f, .4f, 1f);

        //                 GUILayout.BeginVertical(Styles.styles.decorator);

        //                 GUI.color = Color.white;

        //                 GUILayout.Label(d.name, Styles.styles.nodeLabel, GUILayout.Height(GUIData.labelHeight), GUILayout.ExpandWidth(true));

        //                 GUILayout.Label(d.GetInfoContent(), Styles.styles.nodeText);

        //                 GUILayout.Space(GUIData.spacing / 2f);

        //                 GUILayout.EndVertical();

        //                 Rect last = GUILayoutUtility.GetLastRect();

        //                 positions.Add(last.position.y - (GUIData.spacing / 2f));

        //                 if (last.Contains(current.mousePosition))
        //                 {
        //                     windowInfo.hoveredDecorator = d;
        //                     windowInfo.hoveredType = Window.Hovering.Decorator;
        //                     didHoverDecoratorThisFrame = true;
        //                     blocked = true;
        //                 }
        //                 else if (!didHoverDecoratorThisFrame)
        //                 {
        //                     windowInfo.hoveredDecorator = null;
        //                 }

        //                 GUILayout.Space(GUIData.spacing);

        //                 if (j == node.decorators.Length - 1)
        //                 {
        //                     //last item, add extra position to snap to
        //                     positions.Add(last.position.y + last.size.y + (GUIData.spacing / 2f));
        //                 }
        //             }

        //             Rect toDraw = new Rect();
        //             bool draw = node.decorators.Length > 0 && (windowInfo.hoveredNode == node || windowInfo.hoveredType == Window.Hovering.Window) && IsNotLayoutEvent(Event.current);

        //             if (draw)
        //             {
        //                 float closest = positions[0];

        //                 for (int j = 1; j < positions.Count; j++)
        //                 {
        //                     float pos = positions[j];

        //                     closest = Mathf.Abs(current.mousePosition.y - pos) < Mathf.Abs(current.mousePosition.y - closest) ? pos : closest;
        //                 }

        //                 if (node.decorators.Contains(windowInfo.selectedDecorator))
        //                     windowInfo.hoveredDecoratorIndex = positions.IndexOf(closest);

        //                 toDraw = new Rect(new Vector2(0f, closest - GUIData.spacing / 8f), new Vector2(size.x, GUIData.spacing / 4f));
        //             }

        //             GUI.color = Styles.windowAccent;

        //             float contentHeight = Mathf.Max((node.icon == null ? 0 : node.icon.height + 10f) + GUIData.labelHeight, GUIData.minContentHeight);

        //             GUILayout.BeginVertical(Styles.roundedBox, GUILayout.Height(contentHeight));

        //             GUI.color = Color.white;

        //             if (node.icon != null)
        //             {
        //                 GUILayout.Space(5f);
        //                 GUILayout.BeginHorizontal();
        //                 GUILayout.FlexibleSpace();
        //                 GUILayout.Label(node.icon);
        //                 GUILayout.FlexibleSpace();
        //                 GUILayout.EndHorizontal();
        //                 GUILayout.Space(5f);
        //             }
        //             GUILayout.Label(node.name, Styles.styles.nodeLabel, GUILayout.ExpandHeight(true));

        //             GUILayout.EndVertical();

        //             GUILayout.EndVertical();

        //             GUILayout.EndArea();

        //             if (draw && windowInfo.lastClicked == Window.Hovering.Decorator && windowInfo.didDragSinceMouseUp && node.decorators.Contains(windowInfo.selectedDecorator))
        //             {
        //                 toDraw.position += contained.position;

        //                 EditorGUI.DrawRect(toDraw, new Color32(200, 200, 200, 255));
        //             }


        //             if (rect.Contains(current.mousePosition) && IsNotLayoutEvent(current))
        //             {
        //                 windowInfo.hoveredNode = node;
        //                 if (!blocked)
        //                 {
        //                     windowInfo.hoveredType = Window.Hovering.Node;
        //                 }
        //             }

        //             if (rect.Overlaps(selectionBox) && IsNotLayoutEvent(current) && drawBox && windowInfo.didDragSinceMouseUp)
        //             {
        //                 selectionQueue.Add(node);
        //             }
        //             else if (drawBox && windowInfo.didDragSinceMouseUp && IsNotLayoutEvent(current))
        //             {
        //                 if (windowInfo.selected.Contains(node))
        //                 {
        //                     windowInfo.selected.Remove(node);
        //                 }
        //             }
        //         }

        //         List<string> keys = windowInfo.alpha.Keys.ToList();

        //         foreach (string node in keys)
        //         {
        //             windowInfo.alpha[node] -= 2f * Mathf.Clamp(windowInfo.deltaTime, 0f, float.MaxValue);

        //             if (windowInfo.alpha[node] <= 0f)
        //             {
        //                 windowInfo.alpha.Remove(node);
        //             }
        //         }

        //         for (int i = selectionQueue.Count - 1; i >= 0; i--)
        //         {
        //             if (!windowInfo.selected.Contains(selectionQueue[i]))
        //             {
        //                 Select(selectionQueue[i], true);
        //             }
        //         }
        //     }

        //     GUI.color = Color.white;

        //     EndZoomed();
        // }

        private void CalculateWindow()
        {
            if (windowInfo.inspectorToggled)
                window = new Rect(0f, 0f, position.width - windowInfo.inspectorWidth - GUIData.sidebarPadding * 2,
                    position.height);
            else
                window = new Rect(0f, 0f, position.width, position.height);
        }

        /// <summary>
        ///     This function calculates the area of a node, based on its text and calculates decorators (based on contents
        ///     themselves, does not factor in padding)
        /// </summary>
        internal static Vector2 GetArea(Node node, bool recalculate)
        {
            if (node == null) return Vector2.zero;

            //try to get from dictionary
            if (GUIData.sizes.ContainsKey(node) && !recalculate) return GUIData.sizes[node];

            //get size of contents
            float height = Mathf.Max((node.icon == null ? 0 : node.icon.height + 10f) + GUIData.labelHeight,
                GUIData.minContentHeight);
            float width = 0f;

            // foreach (Decorator decorator in node.decorators)
            // {
            //     Debug.Log(decorator);

            //     if (decorator == null)
            //         continue;

            //     float decoratorLabelWidth = Styles.styles.nodeLabel.CalcSize(new GUIContent(decorator.name)).x;

            //     width = Mathf.Max(width, decoratorLabelWidth);
            //     height += GUIData.labelHeight;

            //     Vector2 infoSize = Styles.styles.nodeText.CalcSize(decorator.GetInfoContent());

            //     height += infoSize.y;
            //     width = Mathf.Max(width, infoSize.x);

            //     //The 4 is accounting for the area that GUILayout.BeginVertical adds when applying a background for decorators. Not sure why this happens.
            //     height += GUIData.spacing * 1.5f + 4;
            // }

            Vector2 final = new Vector2(width + 40f, height);

            if (recalculate)
                GUIData.sizes[node] = final;
            else
                GUIData.sizes.Add(node, final);

            return final;
        }

        internal static Vector2 GetAreaWithPadding(Node node, bool recalculate)
        {
            return GetArea(node, recalculate) + Vector2.one * GUIData.nodePadding * 2;
        }

        private static bool IsNotLayoutEvent(Event e)
        {
            return e.type != EventType.Layout;
        }

        private void DrawWindow()
        {
            //Draw selection box
            // if (drawBox)
            // {
            //     Vector2 curPos = WindowToGridPosition(Event.current.mousePosition);
            //     Vector2 size = curPos - windowInfo.mouseDownPos;
            //     Rect r = new Rect(windowInfo.mouseDownPos, size);
            //     r.position = GridToWindowPosition(r.position);
            //     r.size /= windowInfo.zoom;

            //     Handles.DrawSolidRectangleWithOutline(r, new Color(0, 0, 0, 0.1f), new Color(1, 1, 1, 0.6f));
            // }

            // if (windowInfo.selected.Count == 1 || windowInfo.selectedDecorator != null)
            // {
            //     string stringContent = windowInfo.selectedDecorator != null ? windowInfo.selectedDecorator.description : windowInfo.selected[0].description;

            //     if (!String.IsNullOrEmpty(stringContent))
            //     {
            //         GUIStyle s = new GUIStyle(EditorStyles.boldLabel);
            //         s.fixedWidth = window.width - GUIData.nodePadding * 2f;
            //         s.wordWrap = true;

            //         GUIContent content = new GUIContent(stringContent);
            //         float height = s.CalcHeight(content, s.fixedWidth);
            //         Vector2 size = new Vector2(s.fixedWidth, height);
            //         GUI.Label(new Rect(new Vector2(GUIData.nodePadding, position.height - GUIData.nodePadding - height), size), content, s);
            //     }
            // }
        }

        private void DrawToolbar()
        {
            Rect toolbar = new Rect(0f, 0f, window.width, EditorStyles.toolbar.fixedHeight);
            GUI.Box(toolbar, "", EditorStyles.toolbar);

            GUI.color = Color.white;
            GUILayout.BeginArea(toolbar, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Node", EditorStyles.toolbarButton))
            {
                QuickSearch search = new QuickSearch(
                    HelperMethods.GetEnumerableOfType(typeof(Node)),
                    t =>
                    {
                        NodeComponent.NodeComponentCreateArgs nodeCreateArgs =
                            new NodeComponent.NodeComponentCreateArgs();
                        nodeCreateArgs.graph = target;
                        nodeCreateArgs.nodeType = t;
                        nodeCreateArgs.position = canvas.zoomer.WindowToGridPosition(window.center);

                        canvas.Create<NodeComponent>(nodeCreateArgs);
                    }
                );

                WindowComponent.WindowComponentCreateArgs createArgs = new WindowComponent.WindowComponentCreateArgs();

                createArgs.id = 1;
                createArgs.layer = 100;
                createArgs.rect = new Rect(100f, 100f, 500f, 500f);
                createArgs.style = Styles.window;
                createArgs.title = GUIContent.none;
                createArgs.windowProvider = search;

                canvas.Create<WindowComponent>(createArgs);
            }

            EditorGUI.BeginDisabledGroup(
                canvas.selected.Length == 0 ||
                !canvas.selected.All(c =>
                    (c is NodeComponent
                     && (((NodeComponent)c).node.connectionDescriptor == Node.ConnectionDescriptor.Both
                         || ((NodeComponent)c).node.connectionDescriptor == Node.ConnectionDescriptor.OnlyInConnection))
                    || c is ConnectionComponent
                )
            );

            if (GUILayout.Button("Add Conditional", EditorStyles.toolbarButton))
            {
                QuickSearch search = new QuickSearch(
                    HelperMethods.GetEnumerableOfType(typeof(Conditional)),
                    t =>
                    {
                        foreach (GUIComponent component in canvas.selected)
                        {
                            ConditionalComponent.ConditionalComponentCreateArgs conditionalCreateArgs =
                                new ConditionalComponent.ConditionalComponentCreateArgs();
                            conditionalCreateArgs.node = component is NodeComponent
                                ? ((NodeComponent)component).node
                                : ((ConnectionComponent)component).to.node;
                            conditionalCreateArgs.conditionalType = t;

                            canvas.Create<ConditionalComponent>(conditionalCreateArgs);
                        }
                    }
                );

                WindowComponent.WindowComponentCreateArgs createArgs = new WindowComponent.WindowComponentCreateArgs();

                createArgs.id = 1;
                createArgs.layer = 100;
                createArgs.rect = new Rect((window.width - 500f) / 2f, (window.width - 500f) / 2f, 500f, 500f);
                createArgs.style = Styles.window;
                createArgs.title = GUIContent.none;
                createArgs.windowProvider = search;

                canvas.Create<WindowComponent>(createArgs);
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Arrange", EditorStyles.toolbarButton))
            {
                GraphUtility.Arrange(target.nodes);
                GetViewRect(100f, true);
            }

            GUILayout.FlexibleSpace();

            Prefs.liveLink = GUILayout.Toggle(Prefs.liveLink, "Live Link", EditorStyles.toolbarButton);
            Prefs.minimapEnabled = GUILayout.Toggle(Prefs.minimapEnabled, "Minimap", EditorStyles.toolbarButton);
            Prefs.gridSnap = GUILayout.Toggle(Prefs.gridSnap, "Grid Snap", EditorStyles.toolbarButton);

            if (!windowInfo.inspectorToggled && GUILayout.Button(Icons.GetEditor("animationvisibilitytoggleon"),
                    EditorStyles.toolbarButton))
                windowInfo.inspectorToggled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        ///     Draws the inspector for either the selected node or decorator
        /// </summary>
        private void DrawInspector()
        {
            if (!windowInfo.inspectorToggled)
                return;

            float inspectorWidth = windowInfo.inspectorWidth;
            Rect inspectorArea = new Rect(position.width - (inspectorWidth + GUIData.sidebarPadding * 2), 0f,
                inspectorWidth + GUIData.sidebarPadding * 2, position.height);

            Rect divider = new Rect(inspectorArea.x - 1f, EditorStyles.toolbar.fixedHeight, 1f,
                position.height - EditorStyles.toolbar.fixedHeight);
            Rect dividerRegion = new Rect(divider.x - 4.5f, divider.y, 10f, position.height);

            EditorGUI.DrawRect(inspectorArea, Styles.windowBackground);

            if (dividerRegion.Contains(Event.current.mousePosition))
            {
                windowInfo.hoveredType = Window.Hovering.Inspector;

                windowInfo.hoverDivider = true;

                if (Event.current.type == EventType.MouseDown)
                {
                    windowInfo.resizingInspector = true;
                    windowInfo.resizeClickOffset = Event.current.mousePosition.x - divider.x;
                }
            }
            else
            {
                windowInfo.hoverDivider = false;
            }

            if (windowInfo.resizingInspector)
            {
                float desired = Screen.width - Event.current.mousePosition.x - GUIData.sidebarPadding * 2 +
                                windowInfo.resizeClickOffset;
                windowInfo.inspectorWidth = desired;
            }

            EditorGUI.DrawRect(divider, Styles.windowAccent);

            if (!windowInfo.settingsShown)
            {
                Rect inspectorContainer = new Rect(
                    position.width - inspectorWidth - GUIData.sidebarPadding * 2,
                    0f,
                    inspectorWidth + GUIData.sidebarPadding * 2,
                    position.height
                );

                GUILayout.BeginArea(inspectorContainer);
                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                string[] values = Enum.GetNames(typeof(Window.InspectorView));

                GUIContent[] content = new GUIContent[2]
                    { new(values[0], Styles.inspectorIcon), new(values[1], Styles.hiearchyIcon) };

                GUILayout.FlexibleSpace();

                for (int i = 0; i < content.Length; i++)
                    if (GUILayout.Toggle((int)windowInfo.inspectorView == i, content[i], EditorStyles.toolbarButton,
                            GUILayout.Width(100)))
                        windowInfo.inspectorView = (Window.InspectorView)i;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        Icons.GetEditor("animationvisibilitytoggleon"),
                        EditorStyles.toolbarButton
                    )) windowInfo.inspectorToggled = false;

                GUILayout.EndHorizontal();

                windowInfo.inspectorScroll = GUILayout.BeginScrollView(windowInfo.inspectorScroll);
                GUILayout.BeginHorizontal();
                GUILayout.Space(GUIData.nodePadding);
                GUILayout.BeginVertical();

                switch (windowInfo.inspectorView)
                {
                    case Window.InspectorView.Inspector:
                        DrawInspectorWindow();
                        break;
                    case Window.InspectorView.Blackboard:
                        DrawBlackboard(target.blackboard);
                        break;
                }

                GUILayout.EndVertical();
                GUILayout.Space(GUIData.nodePadding);
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
            else
            {
                Rect prefsWindow = new Rect(position.width - inspectorWidth - GUIData.sidebarPadding * 2f, 0f,
                    inspectorWidth + GUIData.sidebarPadding * 2f, position.height);

                GUILayout.BeginArea(prefsWindow);

                GUILayout.BeginHorizontal(EditorStyles.toolbar);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(
                        Icons.GetEditor("animationvisibilitytoggleon"),
                        EditorStyles.toolbarButton
                    )) windowInfo.inspectorToggled = false;

                GUILayout.EndHorizontal();

                windowInfo.inspectorScroll = GUILayout.BeginScrollView(windowInfo.inspectorScroll);
                GUILayout.BeginHorizontal();
                GUILayout.Space(GUIData.nodePadding);
                GUILayout.BeginVertical();
                DrawPreferencesWindow();
                GUILayout.EndVertical();
                GUILayout.Space(GUIData.nodePadding);
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        private void DrawPreferencesWindow()
        {
            GUILayout.Space(GUIData.sidebarPadding);
            GUILayout.Label("Preferences", EditorStyles.largeLabel);
            GUILayout.Space(GUIData.sidebarPadding);

            EditorGUILayout.LabelField("General", EditorStyles.boldLabel);
            Prefs.saveOnClose = EditorGUILayout.Toggle("Save on Close", Prefs.saveOnClose);
            Prefs.formatOnSave = EditorGUILayout.Toggle("Format on Save", Prefs.formatOnSave);
            Prefs.screenshotPath = EditorGUILayout.TextField("Screenshot Path", Prefs.screenshotPath);

            EditorGUILayout.LabelField("");

            EditorGUILayout.LabelField("Editor", EditorStyles.boldLabel);
            Prefs.selectionColor = EditorGUILayout.ColorField(
                new GUIContent("Selection Color", "The selection color to use for nodes"),
                Prefs.selectionColor
            );
            Prefs.highlightColor = EditorGUILayout.ColorField(
                new GUIContent("Highlight Color", "The color to use when highlighting a node"),
                Prefs.highlightColor
            );
            Prefs.enableStatusIndicators = EditorGUILayout.Toggle(
                new GUIContent("Enable Status Indicators", "Toggle status indicators for all nodes"),
                Prefs.enableStatusIndicators
            );
            Prefs.successColor = EditorGUILayout.ColorField(
                new GUIContent("Success Color", "Color to use when successful"),
                Prefs.successColor
            );
            Prefs.failureColor = EditorGUILayout.ColorField(
                new GUIContent("Failure Color", "Color to use when failed"),
                Prefs.failureColor
            );
            Prefs.connectionColor = EditorGUILayout.ColorField(
                new GUIContent("Connection Color", "Color to use for node connections"),
                Prefs.connectionColor
            );
            Prefs.portColor = EditorGUILayout.ColorField(
                new GUIContent("Port Color", "Color to use for node ports"),
                Prefs.portColor
            );

            EditorGUILayout.LabelField("");

            EditorGUILayout.LabelField("Minimap", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Minimap Position");
            Prefs.minimapPosition = GUILayout.Toolbar(Prefs.minimapPosition,
                new[] { "Bottom Left", "Top Left", "Bottom Right", "Top Right" });
            Prefs.minimapWidth = EditorGUILayout.FloatField("Minimap Width", Prefs.minimapWidth);
            Prefs.maxMinimapHeight = EditorGUILayout.FloatField("Max Minimap Height", Prefs.maxMinimapHeight);
            Prefs.minimapOpacity = EditorGUILayout.Slider("Minimap Opacity", Prefs.minimapOpacity, 0f, 1f);
            Prefs.minimapOutlineColor = EditorGUILayout.ColorField("Minimap Outline Color", Prefs.minimapOutlineColor);

            EditorGUILayout.LabelField("");
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            Prefs.enableDebugView = EditorGUILayout.Toggle("Enable Debug View", Prefs.enableDebugView);

            if (Prefs.enableDebugView)
                Prefs.enableDebugViewPlus = EditorGUILayout.Toggle("Enable Debug View+", Prefs.enableDebugViewPlus);

            EditorGUILayout.LabelField("");

            if (GUILayout.Button("Reset to default"))
                Prefs.ResetToDefault();
        }

        /// <summary>
        ///     Where the drawing of the inspector takes place
        /// </summary>
        private void DrawInspectorWindow()
        {
            GUILayout.Space(GUIData.sidebarPadding);
            GUILayout.Label("Inspector", EditorStyles.largeLabel);
            GUILayout.Space(GUIData.sidebarPadding);

            if (distinctTypes.Count > 1)
            {
                EditorGUILayout.LabelField("Different Node Types Selected");
                GUILayout.Label("");

                foreach (Type t in distinctTypes) EditorGUILayout.LabelField(t.Name);
                return;
            }

            if (editor != null)
            {
                bool isInspectingDecorator = false;

                EditorGUI.BeginChangeCheck();
                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                if (isInspectingDecorator)
                {
                    defaultDecoratorEditor.OnInspectorGUI();
                    EditorGUILayout.LabelField("");
                }
                else if (editor.targets.OfType<Node>().Count() > 0)
                {
                    defaultNodeEditor.OnInspectorGUI();
                    EditorGUILayout.LabelField("");
                }

                EditorGUILayout.LabelField(editor.targets[0].name, EditorStyles.boldLabel);
                editor.OnInspectorGUI();
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawBlackboard(Blackboard blackboard)
        {
            GUILayout.Space(GUIData.sidebarPadding);
            GUILayout.Label("Blackboard", EditorStyles.largeLabel);
            GUILayout.Space(GUIData.sidebarPadding);

            blackboardEditor.OnInspectorGUI();
        }

        private void ToggleSearch()
        {
            windowInfo.searchIsShown = !windowInfo.searchIsShown;
            windowInfo.searchRect.position = new Vector2(window.size.x / 2f - windowInfo.searchRect.width / 2f,
                window.size.y / 2f - windowInfo.searchRect.height / 2f);
            windowInfo.searchText = "";
        }

        private void DoSplashWindow(int winID)
        {
            if (windowInfo == null) return;

        }

        private void RebuildComponentTree()
        {
            if (target == null)
                DoSplashCanvas();

            if (target == null || canvas != null)
                return;

            SelectionBoxComponent.SelectionBoxComponentCreateArgs sBoxCreateArgs =
                new SelectionBoxComponent.SelectionBoxComponentCreateArgs();
            sBoxCreateArgs.hideOnMouseUp = true;

            MinimapComponent.MinimapComponentCreateArgs minimapCreateArgs =
                new MinimapComponent.MinimapComponentCreateArgs();
            minimapCreateArgs.offset = () => new Vector2(0f, EditorStyles.toolbar.fixedHeight);

            if (canvas == null)
            {
                PannerZoomer zoomer = new PannerZoomer(this, 0.05f, target.zoom, target.pan,
                    () => isDocked() ? 19.0f : 21.0f);

                zoomer.onPanChange += pan => target.pan = pan;
                zoomer.onZoomChange += zoom => target.zoom = zoom;

                canvas = new ComponentCanvas(this, sBoxCreateArgs, minimapCreateArgs, zoomer, DrawGrid);
            }

            foreach (Node node in target.nodes.OrderBy(x => x.priority))
            {
                NodeComponent.NodeComponentCreateArgs args = new NodeComponent.NodeComponentCreateArgs();
                args.fromExisting = node;

                canvas.Create<NodeComponent>(args);
            }
        }

        private void DoSplashCanvas()
        {
            if (canvas != null)
                return;

            windowInfo.inspectorToggled = false;

            CalculateWindow();

            canvas = new ComponentCanvas(this, null, null, null, DrawGrid);

            WindowComponent.WindowComponentCreateArgs windowCreateArgs
                = new WindowComponent.WindowComponentCreateArgs();

            float height = 512f;
            float width = 512f;

            windowCreateArgs.id = 1;
            windowCreateArgs.rect = new Rect((window.width - width) / 2f, (window.height - height) / 2f, width, height);
            windowCreateArgs.style = Styles.window;
            windowCreateArgs.title = new GUIContent("Open Graph");
            windowCreateArgs.windowProvider = new Splash();
            windowCreateArgs.canClose = false;

            canvas.Create<WindowComponent>(windowCreateArgs);
        }

        private List<Type> GetSearchResults(string query)
        {
            string[] words = query.Trim().Split(' ');

            List<Type> types = windowInfo.searchWantsNode
                ? nodeTypes.Values.SelectMany(x => x).ToList()
                : decoratorTypes.ToList();

            if (string.IsNullOrEmpty(query))
                return types;

            List<Type> ret = new List<Type>();

            foreach (Type t in types)
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];

                    if (t.Name.ToLower().Contains(word.ToLower()) && !ret.Contains(t)) ret.Add(t);
                }

            ret = ret.OrderBy(t => StringSimilarity(t.Name, query)).ToList();

            return ret;
        }

        private int StringSimilarity(string s, string t)
        {
            if (string.IsNullOrEmpty(s))
            {
                if (string.IsNullOrEmpty(t))
                    return 0;
                return t.Length;
            }

            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // initialize the top and right of the table to 0, 1, 2, ...
            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 1; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                {
                    int cost = t[j - 1] == s[i - 1] ? 0 : 1;
                    int min1 = d[i - 1, j] + 1;
                    int min2 = d[i, j - 1] + 1;
                    int min3 = d[i - 1, j - 1] + cost;
                    d[i, j] = Math.Min(Math.Min(min1, min2), min3);
                }

            return d[n, m];
        }

        /// <summary>
        ///     Helper functions to zoom and pan the UI (taken from the XNode framework with some modifications)
        /// </summary>
        public void BeginZoomed(Rect rect, float zoom, float topPadding)
        {
            prevMatrix = GUI.matrix;
            GUI.EndClip();

            GUIUtility.ScaleAroundPivot(Vector2.one / zoom, rect.size * 0.5f);
            GUI.BeginClip(new Rect(-(rect.width * zoom - rect.width) * 0.5f,
                -((rect.height * zoom - rect.height) * 0.5f) + topPadding * zoom,
                rect.width * zoom,
                rect.height * zoom));
        }

        /// <summary>
        ///     Helper functions to zoom and pan the UI (taken from the XNode framework with some modification)
        /// </summary>
        public void EndZoomed()
        {
            GUI.EndClip();
            GUI.BeginClip(new Rect(0f, tabHeight, position.width, position.height));
            GUI.matrix = prevMatrix;
        }

        /// <summary>
        ///     Pans the view smoothly over time
        /// </summary>
        /// <param name="to">Position to pan to (in scaled coordinates)</param>
        /// <param name="duration">How long, in seconds, it takes to pan to the given point</param>
        public void PanView(Vector2 to, float duration)
        {
            windowInfo.panDuration = duration;
            windowInfo.recordedTime = Time.realtimeSinceStartup;
            windowInfo.nextPan = to;

            needsPan = true;
        }

        /// <summary>
        ///     Draws the grid to the screen based on zoom and pan
        /// </summary>
        public void DrawGrid(Rect rect, float zoom, Vector2 panOffset)
        {
            float transitionPoint = 2f;
            float transitionWindow = 0.25f;

            rect.position = Vector2.zero;

            Vector2 center = rect.size * .5f;
            Texture2D gridTex = zoom > 2f ? Styles.gridTexture2x : Styles.gridTexture;

            float fac = 1f - Mathf.Clamp(zoom - (transitionPoint - transitionWindow), 0f, transitionWindow * 2f) /
                (transitionWindow * 2f);

            // Offset from origin in tile units
            float xOffset = -(center.x * zoom + panOffset.x) / gridTex.width;
            float yOffset = ((center.y - rect.size.y) * zoom + panOffset.y) / gridTex.height;

            Vector2 tileOffset = new Vector2(xOffset, yOffset);

            // Amount of tiles
            float tileAmountX = Mathf.Round(rect.size.x * zoom) / gridTex.width;
            float tileAmountY = Mathf.Round(rect.size.y * zoom) / gridTex.height;

            Vector2 tileAmount = new Vector2(tileAmountX, tileAmountY);

            GUI.DrawTextureWithTexCoords(rect, Styles.gridTexture2x, new Rect(tileOffset, tileAmount));
            GUI.color = new Color(1f, 1f, 1f, fac);
            GUI.DrawTextureWithTexCoords(rect, Styles.gridTexture, new Rect(tileOffset, tileAmount));
            GUI.color = Color.white;
        }

        [Serializable]
        public class Window
        {
            public enum Hovering
            {
                Node,
                InConnection,
                OutConnection,
                Decorator,
                Inspector,
                Window,
                Minimap,
                MinimapNode,
                None
            }

            public enum InspectorView
            {
                Inspector,
                Blackboard
            }

            public NodeEditor editor;
            public bool isPanning;
            public Vector2 nextPan;
            public float recordedTime;
            public float timeLastFrame;
            public float panDuration;
            public Vector2 mouseDownPos;
            public List<Node> selected = new();
            public Queue<Node> changedNodes = new();
            public Node hoveredNode;
            public Hovering hoveredType;
            public Node hoveredConnection;
            public bool shouldCheckConnectionHover;
            public Hovering lastClicked;
            public bool didDragSinceMouseUp;
            public int hoveredDecoratorIndex;
            public Rect viewRect;
            public Vector2 blackboardScroll;
            public Vector2 inspectorScroll;
            public InspectorView inspectorView;
            public bool inspectorToggled = true;
            public bool searchIsShown;
            public bool searchWantsNode = true;
            public bool searchAddChildren;
            public float searchbarScroll;
            public string searchText;
            public float splashScroll;
            public Rect minimapView;
            public bool settingsShown;
            public bool useLiveLink;
            public bool resizingInspector;
            public bool hoverDivider;
            public float resizeClickOffset;
            public bool drawMinimap = true;
            public Vector2 mousePosition;
            [SerializeField] internal Rect searchRect = new(0f, 0f, 250f, 350f);
            private float _inspectorWidth = 350f;
            private Vector2 _pan;
            private float _zoom = 1f;
            public Dictionary<string, float> alpha = new();
            public Dictionary<string, bool?> nodeStatus;
            public SearchField search;

            public float zoom
            {
                get => _zoom;
                set
                {
                    float val = Mathf.Clamp(value, 1f, 2.5f);
                    _zoom = val;
                }
            }

            public float deltaTime => Time.realtimeSinceStartup - timeLastFrame;

            public Vector2 pan
            {
                get =>
                    //Remove inconsistencies between the target and actual value (if undo occurs, for example)
                    _pan;
                set => _pan = value;
            }

            public float inspectorWidth
            {
                get => _inspectorWidth;
                set => _inspectorWidth = Mathf.Clamp(value, 350f, editor.position.width - 100f);
            }
        }

        /// <summary>
        ///     Contains utility info for the GUI
        /// </summary>
        [Serializable]
        internal static class GUIData
        {
            public static SerializableDictionary<Node, Vector2> sizes = new();
            public static readonly float nodePadding = 15f;
            public static readonly float sidebarPadding = 15f;
            public static readonly float labelHeight = 30f;
            public static readonly float textHeight = 15f;
            public static readonly float spacing = 10f;
            public static readonly float minContentHeight = 75f;
            public static readonly float zoomSpeed = .05f;
            public static float inspectorWidth = 400f;
        }
    }
}