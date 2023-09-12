using System;
using UnityEngine;

public class GUIDraggableObject {
	public Vector2 pos = Vector2.zero;
	[NonSerialized] public Vector2 dragStart = Vector2.zero;
	public bool isDragging { get; internal set; } = false;
	private bool isDraggingAll = false;
	private Vector2 draggingAllStart = Vector2.zero;

	public GUIDraggableObject() {}

	public GUIDraggableObject(Vector2 position) => pos = position;

	public void Drag(Rect draggingRect) {
		if (Event.current.type == EventType.MouseUp) {
			isDragging = false;
		} else if (Event.current.type == EventType.MouseDown && draggingRect.Contains(Event.current.mousePosition)) {
			isDragging = true;
			dragStart = Event.current.mousePosition - pos;
            pos = new Vector2(Mathf.Round(pos.x), Mathf.Round(pos.y));
            Event.current.Use();
		}

		if (isDragging && !isDraggingAll) {
			Vector2 newPos = Event.current.mousePosition - dragStart;
			pos = newPos;
		}
	}

	public void SetDraggingAll(bool dragAll) {
		isDraggingAll = dragAll;
		if (dragAll) draggingAllStart = pos;
	}

	public void DraggingAllSetPosition(Vector2 newPos) => pos = draggingAllStart - newPos;
}