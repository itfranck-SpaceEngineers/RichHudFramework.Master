﻿using System;
using VRage;
using VRageMath;

namespace RichHudFramework
{
    namespace UI
    {
        using Server;
        using Client;

        /// <summary>
        /// Base type for all hud elements with definite size and position. Inherits from HudParentBase and HudNodeBase.
        /// </summary>
        public abstract class HudElementBase : HudNodeBase, IHudElement
        {
            /// <summary>
            /// Parent object of the node.
            /// </summary>
            public override IHudParent Parent
            {
                get { return base.Parent; }
                protected set
                {
                    base.Parent = value;
                    parent = value as HudElementBase;
                }
            }

            /// <summary>
            /// Scales the size and offset of an element. Any offset or size set at a given
            /// be increased or decreased with scale. Defaults to 1f. Includes parent scale.
            /// </summary>
            public virtual float Scale
            {
                get { return (parent == null || ignoreParentScale) ? localScale : localScale * parentScale; }
                set { localScale = value; }
            }

            /// <summary>
            /// Size of the element in pixels.
            /// </summary>
            public virtual Vector2 Size
            {
                get { return new Vector2(Width, Height); }
                set { Width = value.X; Height = value.Y; }
            }

            /// <summary>
            /// With of the hud element in pixels.
            /// </summary>
            public virtual float Width
            {
                get { return (width * Scale) + Padding.X; }
                set
                {
                    if (Padding.X < value)
                        width = (value - Padding.X) / Scale;
                    else
                        width = (value / Scale);
                }
            }

            /// <summary>
            /// Height of the hud element in pixels.
            /// </summary>
            public virtual float Height
            {
                get { return (height * Scale) + Padding.Y; }
                set
                {
                    if (Padding.Y < value)
                        height = (value - Padding.Y) / Scale;
                    else
                        height = (value / Scale);
                }
            }

            public virtual Vector2 Padding { get { return padding * Scale; } set { padding = value / Scale; } }

            /// <summary>
            /// Starting position of the hud element on the screen in pixels.
            /// </summary>
            public virtual Vector2 Origin => (parent == null) ? Vector2.Zero : (parent.Origin + parent.Offset + offsetAlignment);

            /// <summary>
            /// Position of the element relative to its origin.
            /// </summary>
            public virtual Vector2 Offset { get { return offset * Scale; } set { offset = value / Scale; } }

            /// <summary>
            /// Determines the starting position of the hud element relative to its parent.
            /// </summary>
            public ParentAlignments ParentAlignment { get; set; }

            public DimAlignments DimAlignment { get; set; }

            /// <summary>
            /// If set to true the hud element will be allowed to capture the cursor.
            /// </summary>
            public bool CaptureCursor { get; set; }

            /// <summary>
            /// If set to true the hud element will share the cursor with its child elements.
            /// </summary>
            public bool ShareCursor { get; set; }

            /// <summary>
            /// Indicates whether or not the cursor is currently over the element. The element must
            /// be set to capture the cursor for this to work.
            /// </summary>
            public virtual bool IsMousedOver => Visible && isMousedOver;

            public bool ignoreParentScale;

            private const float minMouseBounds = 8f;
            private float parentScale, localScale, width, height;
            private bool isMousedOver;
            private Vector2 offset, padding, offsetAlignment;

            protected HudElementBase parent;

            /// <summary>
            /// Initializes a new hud element with cursor sharing enabled and scaling set to 1f.
            /// </summary>
            public HudElementBase(IHudParent parent) : base(parent)
            {
                ShareCursor = true;
                DimAlignment = DimAlignments.None;
                ParentAlignment = ParentAlignments.Center;

                localScale = 1f;
                parentScale = 1f;
            }

            public sealed override void HandleInputStart()
            {
                if (Visible)
                {
                    if (CaptureCursor && HudMain.Cursor.Visible && !HudMain.Cursor.IsCaptured)
                    {
                        isMousedOver = IsMouseInBounds();

                        if (isMousedOver)
                            HudMain.Cursor.Capture(ID);
                    }
                    else
                        isMousedOver = false;

                    if (ShareCursor)
                        ShareInput();
                    else
                        HandleChildInput();

                    HandleInput();
                }
                else
                    isMousedOver = false;
            }

            /// <summary>
            /// Temporarily releases the cursor and shares it with its child elements then attempts
            /// to recapture it.
            /// </summary>
            private void ShareInput()
            {
                bool wasCapturing = isMousedOver && HudMain.Cursor.IsCapturing(ID);
                HudMain.Cursor.TryRelease(ID);
                HandleChildInput();

                if (!HudMain.Cursor.IsCaptured && wasCapturing)
                    HudMain.Cursor.Capture(ID);
            }

            private void HandleChildInput()
            {
                for (int n = children.Count - 1; n >= 0; n--)
                {
                    if (children[n].Visible)
                        children[n].HandleInputStart();
                }
            }

            /// <summary>
            /// Determines whether or not the cursor is within the bounds of the hud element.
            /// </summary>
            private bool IsMouseInBounds()
            {
                Vector2 pos = Origin + Offset, cursorPos = HudMain.Cursor.Origin;
                float
                    width = Math.Max(minMouseBounds, Size.X),
                    height = Math.Max(minMouseBounds, Size.Y),
                    leftBound = pos.X - width / 2f,
                    rightBound = pos.X + width / 2f,
                    upperBound = pos.Y + height / 2f,
                    lowerBound = pos.Y - height / 2f;

                return
                    (cursorPos.X >= leftBound && cursorPos.X < rightBound) &&
                    (cursorPos.Y >= lowerBound && cursorPos.Y < upperBound);
            }

            public override void BeforeDrawStart()
            {
                base.BeforeDrawStart();

                if (parent != null)
                {
                    if (parentScale != parent.Scale)
                        parentScale = parent.Scale;

                    GetDimAlignment();
                    offsetAlignment = GetParentAlignment();
                }
            }

            private void GetDimAlignment()
            {
                if (Size != parent.Size)
                {
                    if (DimAlignment.HasFlag(DimAlignments.IgnorePadding))
                    {
                        if (DimAlignment.HasFlag(DimAlignments.Width))
                            Width = parent.Width - parent.Padding.X;

                        if (DimAlignment.HasFlag(DimAlignments.Height))
                            Height = parent.Height - parent.Padding.Y;
                    }
                    else
                    {
                        if (DimAlignment.HasFlag(DimAlignments.Width))
                            Width = parent.Width;

                        if (DimAlignment.HasFlag(DimAlignments.Height))
                            Height = parent.Height;
                    }
                }
            }

            /// <summary>
            /// Calculates the offset necessary to achieve the alignment specified by the
            /// ParentAlignment property.
            /// </summary>
            private Vector2 GetParentAlignment()
            {
                Vector2 alignment = Vector2.Zero;
                Vector2 max = (parent.Size + Size) / 2f, min = -max;

                if (ParentAlignment.HasFlag(ParentAlignments.UsePadding))
                {
                    min += parent.Padding / 2f;
                    max -= parent.padding / 2f;
                }

                if (ParentAlignment.HasFlag(ParentAlignments.InnerV))
                {
                    min.Y += Size.Y;
                    max.Y -= Size.Y;
                }

                if (ParentAlignment.HasFlag(ParentAlignments.InnerH))
                {
                    min.X += Size.X;
                    max.X -= Size.X;
                }

                if (ParentAlignment.HasFlag(ParentAlignments.Bottom))
                    alignment.Y = min.Y;
                else if (ParentAlignment.HasFlag(ParentAlignments.Top))
                    alignment.Y = max.Y;

                if (ParentAlignment.HasFlag(ParentAlignments.Left))
                    alignment.X = min.X;
                else if (ParentAlignment.HasFlag(ParentAlignments.Right))
                    alignment.X = max.X;

                return alignment;
            }
        }
    }
}