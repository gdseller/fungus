// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Fungus.EventHandlers;
using Fungus.Commands;

namespace Fungus
{
    /// <summary>
    /// Detects drag and drop interactions on a Game Object, and sends events to all Flowchart event handlers in the scene.
    /// The Game Object must have Collider2D & RigidBody components attached. 
    /// The Collider2D must have the Is Trigger property set to true.
    /// The RigidBody would typically have the Is Kinematic property set to true, unless you want the object to move around using physics.
    /// Use in conjunction with the Drag Started, Drag Completed, Drag Cancelled, Drag Entered & Drag Exited event handlers.
    /// </summary>
    public class Draggable2D : MonoBehaviour, IDraggable2D, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Is object dragging enabled")]
        [SerializeField] protected bool dragEnabled = true;

        [Tooltip("Move object back to its starting position when drag is cancelled")]
        [FormerlySerializedAs("returnToStartPos")]
        [SerializeField] protected bool returnOnCancelled = true;

        [Tooltip("Move object back to its starting position when drag is completed")]
        [SerializeField] protected bool returnOnCompleted = true;

        [Tooltip("Time object takes to return to its starting position")]
        [SerializeField] protected float returnDuration = 1f;

        [Tooltip("Mouse texture to use when hovering mouse over object")]
        [SerializeField] protected Texture2D hoverCursor;

        [Tooltip("Use the UI Event System to check for drag events. Clicks that hit an overlapping UI object will be ignored. Camera must have a PhysicsRaycaster component, or a Physics2DRaycaster for 2D colliders.")]
        [SerializeField] protected bool useEventSystem;

        protected Vector3 startingPosition;
        protected bool updatePosition = false;
        protected Vector3 newPosition;
        protected Vector3 delta = Vector3.zero;

        protected virtual void LateUpdate()
        {
            // iTween will sometimes override the object position even if it should only be affecting the scale, rotation, etc.
            // To make sure this doesn't happen, we force the position change to happen in LateUpdate.
            if (updatePosition)
            {
                transform.position = newPosition;
                updatePosition = false;
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other) 
        {
            if (!dragEnabled)
            {
                return;
            }

            foreach (DragEntered handler in GetHandlers<DragEntered>())
            {
                handler.OnDragEntered(this, other);
            }

            foreach (DragCompleted handler in GetHandlers<DragCompleted>())
            {
                handler.OnDragEntered(this, other);
            }
        }

        protected virtual void OnTriggerExit2D(Collider2D other) 
        {
            if (!dragEnabled)
            {
                return;
            }

            foreach (DragExited handler in GetHandlers<DragExited>())
            {
                handler.OnDragExited(this, other);
            }

            foreach (DragCompleted handler in GetHandlers<DragCompleted>())
            {
                handler.OnDragExited(this, other);
            }
        }

        protected virtual T[] GetHandlers<T>() where T : EventHandler
        {
            // TODO: Cache these object for faster lookup
            return GameObject.FindObjectsOfType<T>();
        }

        protected virtual void DoBeginDrag()
        {
            // Offset the object so that the drag is anchored to the exact point where the user clicked it
            float x = Input.mousePosition.x;
            float y = Input.mousePosition.y;
            delta = Camera.main.ScreenToWorldPoint(new Vector3(x, y, 10f)) - transform.position;
            delta.z = 0f;

            startingPosition = transform.position;

            foreach (DragStarted handler in GetHandlers<DragStarted>())
            {
                handler.OnDragStarted(this);
            }
        }

        protected virtual void DoDrag()
        {
            if (!dragEnabled)
            {
                return;
            }

            float x = Input.mousePosition.x;
            float y = Input.mousePosition.y;
            float z = transform.position.z;

            newPosition = Camera.main.ScreenToWorldPoint(new Vector3(x, y, 10f)) - delta;
            newPosition.z = z;
            updatePosition = true;
        }

        protected virtual void DoEndDrag()
        {
            if (!dragEnabled)
            {
                return;
            }

            bool dragCompleted = false;

            DragCompleted[] handlers = GetHandlers<DragCompleted>();
            foreach (DragCompleted handler in handlers)
            {
                if (handler.DraggableObject == this)
                {
                    if (handler.IsOverTarget())
                    {
                        handler.OnDragCompleted(this);
                        dragCompleted = true;

                        if (returnOnCompleted)
                        {
                            LeanTween.move(gameObject, startingPosition, returnDuration).setEase(LeanTweenType.easeOutExpo);
                        }
                    }
                }
            }

            if (!dragCompleted)
            {
                foreach (DragCancelled handler in GetHandlers<DragCancelled>())
                {
                    handler.OnDragCancelled(this);
                }

                if (returnOnCancelled)
                {
                    LeanTween.move(gameObject, startingPosition, returnDuration).setEase(LeanTweenType.easeOutExpo);
                }
            }
        }

        protected virtual void DoPointerEnter()
        {
            ChangeCursor(hoverCursor);
        }

        protected virtual void DoPointerExit()
        {
            SetMouseCursor.ResetMouseCursor();
        }

        protected virtual void ChangeCursor(Texture2D cursorTexture)
        {
            if (!dragEnabled)
            {
                return;
            }

            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
        }

        #region Legacy OnMouseX methods

        protected virtual void OnMouseDown()
        {
            if (!useEventSystem)
            {
                DoBeginDrag();
            }
        }

        protected virtual void OnMouseDrag()
        {
            if (!useEventSystem)
            {
                DoDrag();
            }
        }

        protected virtual void OnMouseUp()
        {
            if (!useEventSystem)
            {
                DoEndDrag();
            }
        }

        protected virtual void OnMouseEnter()
        {
            if (!useEventSystem)
            {
                DoPointerEnter();
            }
        }
        
        protected virtual void OnMouseExit()
        {
            if (!useEventSystem)
            {
                DoPointerExit();
            }
        }

        #endregion

        #region IDraggable2D implementation

        public virtual bool DragEnabled { get { return dragEnabled; } set { dragEnabled = value; } }

        #endregion

        #region IBeginDragHandler implementation

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (useEventSystem)
            {
                DoBeginDrag();
            }
        }

        #endregion

        #region IDragHandler implementation

        public void OnDrag(PointerEventData eventData)
        {
            if (useEventSystem)
            {
                DoDrag();
            }
        }

        #endregion

        #region IEndDragHandler implementation

        public void OnEndDrag(PointerEventData eventData)
        {
            if (useEventSystem)
            {
                DoEndDrag();
            }
        }

        #endregion

        #region IPointerEnterHandler implementation

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (useEventSystem)
            {
                DoPointerEnter();
            }
        }

        #endregion

        #region IPointerExitHandler implementation

        public void OnPointerExit(PointerEventData eventData)
        {
            if (useEventSystem)
            {
                DoPointerExit();
            }
        }

        #endregion
    }
}
