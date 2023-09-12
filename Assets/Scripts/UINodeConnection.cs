using UnityEngine;
using System;
using UnityEditor;
using SimpleBehaviourTree;
using System.Collections.Generic;

[Serializable]
public class UINodeConnection {
    public Rect connectionRect { get; internal set; }
    [NonSerialized] private TreeMaker treeMaker = null;
    [SerializeField] private UINode childUINode = null;
    [SerializeField] private UINode parentUINode = null;
    private Vector3 childNodeConnectionPoint = Vector3.zero;
    private Vector3 parentNodeConnectionPoint = Vector3.zero;
    public int executionOrderIndex { get; private set; } = -1;
    private bool isDeleting = false;

    public UINodeConnection(UINodeConnection nodeConnectionToCopy, List<UINode> nodesBeingSaved) {
        childUINode = nodesBeingSaved.Find(x => x.name == nodeConnectionToCopy.childUINode.name);
        parentUINode = nodesBeingSaved.Find(x => x.name == nodeConnectionToCopy.parentUINode.name);
    }

    public UINodeConnection(UINodeConnection nodeConnectionToCopy, TreeMaker treeMaker) {
        if (treeMaker != null)
            this.treeMaker = treeMaker;
        List<UINode> nodes = new(treeMaker.GetNodes());
        childUINode = nodes.Find(x => x.name == nodeConnectionToCopy.childUINode.name);
        parentUINode = nodes.Find(x => x.name == nodeConnectionToCopy.parentUINode.name);
        childNodeConnectionPoint = childUINode.childConnectionPoint;
        parentNodeConnectionPoint = parentUINode.parentConnectionPoint;
    }

    public UINodeConnection(UINode child, UINode parent, TreeMaker treeMaker, bool isLoadingConnection = false, bool isSavingConnection = false) {
        this.treeMaker = treeMaker;
        childUINode = child;
        parentUINode = parent;
        treeMaker.AddNodeConnection(this);
        treeMaker.SetCurrentConnection(this);
    }

    public void OnGUI(TreeMaker treeMaker) {
        if (this.treeMaker == null)
            this.treeMaker = treeMaker;
        if (isDeleting)
            return;
        if (childUINode == null && parentUINode == null) {
            Debug.LogWarning("No start nor end UINode; destroying connection!");
            Destroy();
            return;
        }
        childNodeConnectionPoint = childUINode != null ? childUINode.parentConnectionPoint : treeMaker.mousePos;
        parentNodeConnectionPoint = parentUINode != null ? parentUINode.childConnectionPoint : treeMaker.mousePos;
        Handles.DrawLine(childNodeConnectionPoint, parentNodeConnectionPoint);

        if (parentUINode != null && childUINode != null) {
            Vector2 size = new Vector2(25, 25);
            Vector2 pos = ((parentNodeConnectionPoint + childNodeConnectionPoint) / 2) - new Vector3(size.x / 2, size.y / 2, 0);
            Rect drawRect = new Rect(pos, size);
            connectionRect = drawRect;
            GUILayout.BeginArea(drawRect);
            Rect btnRect = new Rect(0, 0, drawRect.width, drawRect.height);
            GUILayout.Label("", GUI.skin.GetStyle("Box"), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (GUI.Button(btnRect, treeMaker.delete))
                DeleteConnection(false);
            GUILayout.EndArea();

            Color guiColor = GUI.color;
            GUI.color = Color.white;
            drawRect = new Rect(pos + Vector2.down * -25, size);
            GUILayout.BeginArea(drawRect);
            GUILayout.Label(executionOrderIndex.ToString(), GUI.skin.GetStyle("Box"), GUILayout.ExpandWidth(true));
            GUILayout.EndArea();
            GUI.color = guiColor;
        }
    }

    public bool IsFullyConnected() {
        return childUINode == null || parentUINode == null ? false : true;
    }

    public void FinishConnection(UINode n, TreeMaker treeMaker) {
        if (parentUINode == null && childUINode == null) {
            if (this.treeMaker == null)
                this.treeMaker = treeMaker;
            Destroy();
            return;
        } else if (parentUINode == null) {
            parentUINode = n;
            childUINode.SetParent(parentUINode);
            parentUINode.AddChild(childUINode);
        } else {
            childUINode = n;
            parentUINode.AddChild(childUINode);
            childUINode.SetParent(parentUINode);
        }
        treeMaker.SetCurrentConnection(null);
        parentUINode.SetExecutionOrderIndicesInConnections();
    }

    public void SetExecutionOrderIndex(int index) => executionOrderIndex = index;

    public bool GotChild() {
        return childUINode == null ? false : true;
    }

    public bool GotParent() {
        return parentUINode == null ? false : true;
    }

    public UINode GetChild() {
        return childUINode;
    }

    public UINode GetParent() {
        return parentUINode;
    }

    public void DeleteConnection(bool isPhantom) {
        UINode parent = parentUINode;
        isDeleting = true;
        if (childUINode != null)
            childUINode.SetParent(null);
        if (parentUINode != null)
            parentUINode.RemoveChild(childUINode);
        childUINode = null;
        parentUINode = null;
        if (!isPhantom)
            treeMaker.RemoveConnection(treeMaker.GetConnectionIndex(this));
        if (parent != null)
            parent.SetExecutionOrderIndicesInConnections();
    }

    void Destroy() {
        if (treeMaker.nodeConnections.Contains(this))
            treeMaker.RemoveConnection(treeMaker.nodeConnections.IndexOf(this));
        treeMaker.currentConnection = null;
        childUINode = null;
        parentUINode = null;
        treeMaker = null;
    }
}