using System;
using System.Linq;
using System.Collections.Generic;
using SchemaEditor;
using SchemaEditor.Internal;
using UnityEngine;
using UnityEditor;

namespace SchemaEditor.Internal.ComponentSystem.Components
{
    public sealed class MinimapComponent : GUIComponent, ICanvasMouseEventSink
    {
        public override void Create(CreateArgs args)
        {
            MinimapComponentCreateArgs createArgs = args as MinimapComponentCreateArgs;

            if (createArgs == null)
                throw new ArgumentException();

            this.offset = createArgs.offset;

            listModified = true;
            canvas.onComponentListModified += () => listModified = true;
        }
        private Rect rect;
        private float viewWidth;
        private Func<Vector2> offset;
        private bool listModified;
        private Rect graphRect;
        private Rect gridViewRect;
        public override void OnGUI()
        {
            rect = canvas.context.GetViewRect();

            graphRect = GetGraphRect(100f);

            float height = graphRect.height / graphRect.width * NodeEditor.Prefs.minimapWidth;

            viewWidth = height > NodeEditor.Prefs.maxMinimapHeight
                ? NodeEditor.Prefs.minimapWidth / height * NodeEditor.Prefs.maxMinimapHeight
                : NodeEditor.Prefs.minimapWidth;

            height = Mathf.Clamp(height, 0f, NodeEditor.Prefs.maxMinimapHeight);

            float toolbarHeight = canvas.context.GetToolbarHeight();

            switch (NodeEditor.Prefs.minimapPosition)
            {
                case 0:
                    rect = new Rect(10f, rect.yMax - height - 10f - toolbarHeight, NodeEditor.Prefs.minimapWidth, height);
                    break;
                case 1:
                    rect = new Rect(10f, 10f + toolbarHeight, NodeEditor.Prefs.minimapWidth, height);
                    break;
                case 2:
                    rect = new Rect(rect.xMax - 10f - NodeEditor.Prefs.minimapWidth, rect.yMax - height - 10f, NodeEditor.Prefs.minimapWidth, height);
                    break;
                case 3:
                    rect = new Rect(rect.xMax - 10f - NodeEditor.Prefs.minimapWidth, 0f, NodeEditor.Prefs.minimapWidth, height);
                    break;
            }

            DoEvents();

            gridViewRect = canvas.zoomer.WindowToGridRect(canvas.context.GetViewRect());
            gridViewRect.position = GridToMinimapPosition(gridViewRect.position);
            gridViewRect.position = new Vector2(gridViewRect.position.x + rect.width / 2f - viewWidth / 2f, gridViewRect.position.y);
            gridViewRect.size = gridViewRect.size / graphRect.width * viewWidth;

            Handles.DrawSolidRectangleWithOutline(rect, new Color(0f, 0f, 0f, NodeEditor.Prefs.minimapOpacity), NodeEditor.Prefs.minimapOutlineColor);

            GUI.BeginGroup(rect);

            IEnumerable<NodeComponent> nodeComponents = canvas.components
                .Where(x => x is NodeComponent)
                .Cast<NodeComponent>()
                .Reverse();

            foreach (NodeComponent node in nodeComponents)
            {
                Vector2 position = GridToMinimapPosition(node.node.graphPosition);
                position.x += rect.width / 2f - viewWidth / 2f;

                Vector2 size = node.layout.body.size / graphRect.width * viewWidth;

                Rect nodeRect = new Rect(position, size);

                Handles.DrawSolidRectangleWithOutline(nodeRect, Styles.windowBackground, node.IsSelected() ? NodeEditor.Prefs.selectionColor : Color.black);
            }


            Handles.DrawSolidRectangleWithOutline(gridViewRect, new Color(0f, 0f, 0f, 0f), Color.gray);

            GUI.EndGroup();
        }
        private void DoEvents()
        {
            Event e = Event.current;

            if (!rect.Contains(e.mousePosition))
                return;

            switch (e.rawType)
            {
                case EventType.MouseDown:
                    Vector2 newPan = e.mousePosition - rect.position;
                    newPan.x -= rect.width / 2f - viewWidth / 2f;
                    newPan = MinimapToGridPosition(newPan);
                    canvas.zoomer.pan = -newPan;

                    canvas.cursor = MouseCursor.Pan;
                    break;
                case EventType.MouseDrag:
                    Vector2 ratio = canvas.context.GetViewRect().size / gridViewRect.size;
                    canvas.zoomer.pan -= e.delta * ratio * canvas.zoomer.zoom;
                    break;
            }
        }
        public Vector2 GridToMinimapPosition(Vector2 position)
        {
            position = (position - graphRect.position) / graphRect.size;
            Vector2 sizeFac = new Vector2(viewWidth, graphRect.height / graphRect.width * viewWidth);

            return position * sizeFac;
        }
        public Vector2 MinimapToGridPosition(Vector2 position)
        {
            Vector2 sizeFac = new Vector2(viewWidth, graphRect.height / graphRect.width * viewWidth);
            position = position / sizeFac;

            position *= graphRect.size;
            position += graphRect.position;

            return position;
        }
        private Rect GetGraphRect(float padding)
        {
            IEnumerable<NodeComponent> nodeComponents = canvas.components
                .Where(x => x is NodeComponent)
                .Cast<NodeComponent>();

            float xMin = nodeComponents.Min(x => x.layout.gridRect.x) - padding;
            float xMax = nodeComponents.Max(x => x.layout.gridRect.x + x.layout.gridRect.width) + padding;
            float yMin = nodeComponents.Min(x => x.layout.gridRect.y) - padding;
            float yMax = nodeComponents.Max(x => x.layout.gridRect.y + x.layout.gridRect.height) + padding;

            graphRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);

            listModified = false;

            return graphRect;
        }
        public Rect GetRect() { return rect; }
        public class MinimapComponentCreateArgs : CreateArgs
        {
            public Func<Vector2> offset { get; set; }
        }
    }
}