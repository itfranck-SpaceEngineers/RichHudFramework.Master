﻿using System;
using System.Collections.Generic;
using VRage;
using RichHudFramework.Game;
using ApiMemberAccessor = System.Func<object, int, object>;
using EventAccessor = VRage.MyTuple<bool, System.Action>;
using GlyphFormatMembers = VRage.MyTuple<byte, float, VRageMath.Vector2I, VRageMath.Color>;

namespace RichHudFramework
{
    using HudElementMembers = MyTuple<
        Func<bool>, // Visible
        object, // ID
        Action, // BeforeDrawStart
        Action, // DrawStart
        Action, // HandleInput
        ApiMemberAccessor // GetOrSetMembers
    >;

    namespace UI
    {
        /// <summary>
        /// Base class for HUD elements to which other elements are parented. Types deriving from this class cannot be
        /// parented to other elements; only types of <see cref="IHudNode"/> can be parented.
        /// </summary>
        public abstract class HudParentBase : IHudParent
        {
            /// <summary>
            /// Determines whether or not an element will be drawn or process input. Visible by default.
            /// </summary>
            public virtual bool Visible { get; set; }

            /// <summary>
            /// Unique identifer.
            /// </summary>
            public object ID => this;

            protected readonly List<IHudNode> children;

            public HudParentBase()
            {
                Visible = true;
                children = new List<IHudNode>();
            }

            /// <summary>
            /// Updates input for the element and its children.
            /// </summary>
            public virtual void HandleInputStart()
            {
                for (int n = children.Count - 1; n >= 0; n--)
                {
                    if (children[n].Visible)
                        children[n].HandleInputStart();
                }

                HandleInput();
            }

            protected virtual void HandleInput() { }

            /// <summary>
            /// Updates before draw for the element and its children.
            /// </summary>
            public virtual void BeforeDrawStart()
            {
                BeforeDraw();

                for (int n = 0; n < children.Count; n++)
                {
                    if (children[n].Visible)
                        children[n].BeforeDrawStart();
                }
            }

            protected virtual void BeforeDraw() { }

            /// <summary>
            /// Draws the element and its children.
            /// </summary>
            public virtual void DrawStart()
            {
                Draw();

                for (int n = 0; n < children.Count; n++)
                {
                    if (children[n].Visible)
                        children[n].DrawStart();
                }
            }

            protected virtual void Draw() { }

            /// <summary>
            /// Moves the specified child element to the end of the update list in
            /// order to ensure that it's drawn on top/updated last.
            /// </summary>
            public void SetFocus(IHudNode child) =>
                SetFocus(child.ID);

            private void SetFocus(object childID)
            {
                int last = children.Count - 1,
                    childIndex = children.FindIndex(x => x.ID == childID);

                children.Swap(last, childIndex);
            }

            /// <summary>
            /// Registers a child node to the object.
            /// </summary>
            public virtual void RegisterChild(IHudNode child)
            {
                if (child.Parent?.ID == ID && !child.Registered)
                    children.Add(child);
                else if (child.Parent?.ID == null)
                    child.Register(this);
            }

            /// <summary>
            /// Registers a collection of child nodes to the object.
            /// </summary>
            public void RegisterChildren(IEnumerable<IHudNode> newChildren)
            {
                foreach (IHudNode child in newChildren)
                    child.Register(this);
            }

            /// <summary>
            /// Unregisters the specified node from the parent.
            /// </summary>
            public virtual void RemoveChild(IHudNode child)
            {
                if (child.Parent?.ID == ID)
                    child.Unregister();
                else if (child.Parent == null)
                    children.Remove(child);
            }

            private void RemoveChild(object childID)
            {
                IHudNode node = children.Find(x => x.ID == childID);
                RemoveChild(node);
            }

            protected virtual object GetOrSetMember(object data, int memberEnum)
            {
                switch ((HudParentAccessors)memberEnum)
                {
                    case HudParentAccessors.Add:
                        RegisterChild(new HudNodeData((HudElementMembers)data));
                        break;
                    case HudParentAccessors.RemoveChild:
                        RemoveChild(data);
                        break;
                    case HudParentAccessors.SetFocus:
                        SetFocus(data);
                        break;
                }

                return null;
            }

            /// <summary>
            /// Retrieves the information necessary to access the <see cref="IHudParent"/> through the API.
            /// </summary>
            public HudElementMembers GetApiData()
            {
                return new HudElementMembers()
                {
                    Item1 = () => Visible,
                    Item2 = this,
                    Item3 = () => ExceptionHandler.Run(BeforeDrawStart),
                    Item4 = () => ExceptionHandler.Run(DrawStart),
                    Item5 = () => ExceptionHandler.Run(HandleInputStart),
                    Item6 = GetOrSetMember
                };
            }           
        }
    }
}