using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UINode : GUIDraggableObject {
    [Header("Base Node Settings")]
    public string name = "";
    [SerializeReference] public UINode parent = null;
    [SerializeReference] public List<UINode> children = new();
    [SerializeField] private NodeTypes nodeType = NodeTypes.Leaf;

    [Header("Tree Maker Settings")]
    private TreeMaker treeMaker = null;
    public bool isRoot = false;
    [NonSerialized] public bool isSelected = false;
    public Vector3 childConnectionPoint = Vector3.zero;
    public Vector3 parentConnectionPoint = Vector3.zero;
    public int listIndex { get; private set; } = 0;
    public bool doDrawImage { get; private set; } = true;
    public float[] defaultBoxSize { get; private set; } = new float[2] { 210, 80 };
    public float[] defaultImageSize { get; private set; } = new float[2] { 50, 50 };
    private float[] boxSize = new float[2] { 210, 80 };
    private float[] imageSize = new float[2] { 50, 50 };

    public UINode() { }

    public UINode(UINode nodeToCopy, TreeMaker treeMaker = null) {
        if (treeMaker != null)
            this.treeMaker = treeMaker;
        parent = nodeToCopy.parent;
        children = new List<UINode>(nodeToCopy.children);
        pos = nodeToCopy.pos;
        name = nodeToCopy.name;
        nodeType = nodeToCopy.nodeType;
        isRoot = nodeToCopy.isRoot;
        childConnectionPoint = nodeToCopy.childConnectionPoint;
        parentConnectionPoint = nodeToCopy.parentConnectionPoint;
    }

    public UINode(string name, Vector2 position, TreeMaker treeMaker) : base(position) {
        this.name = name;
        this.treeMaker = treeMaker;
    }

    public void OnGUI() {
        //Define the node's box
        Rect drawRect;
        drawRect = new Rect(pos.x, pos.y, boxSize[0], boxSize[1]);
        GUILayout.BeginArea(drawRect, GUI.skin.GetStyle("Box"));
        GUILayout.Label(name, GUI.skin.GetStyle("Box"), GUILayout.ExpandWidth(true));
        GUILayout.EndArea();

        //Define the connection buttons
        Rect setParentButton = new(drawRect.width / 2, 0, 15, 15);
        Rect addChildButton = new(drawRect.width / 2, drawRect.height, 15, 15);
        childConnectionPoint = drawRect.position + addChildButton.position;
        parentConnectionPoint = drawRect.position + setParentButton.position;

        #region
        GUILayout.BeginArea(new Rect(pos.x - 5, pos.y - 5, boxSize[0] + 10, boxSize[1] + 10));
        //Draw the button for adding a parent if the node isn't the root
        if (!isRoot) {
            if (GUI.Button(setParentButton, "")) {
                if (treeMaker.currentConnection == null) {
                    if (parent == null) {
                        new UINodeConnection(this, null, treeMaker);
                    } else {
                        Debug.LogWarning("A node can only ever have one parent");
                    }
                } else {
                    if (!treeMaker.currentConnection.GotChild()) {
                        if (parent == null) {
                            if (treeMaker.currentConnection.GetParent() != this) {
                                treeMaker.currentConnection.FinishConnection(this, treeMaker);
                            } else {
                                Debug.LogWarning("A node can't connect to itself");
                            }
                        } else {
                            Debug.LogWarning("A node can only ever have one parent");
                        }
                    } else {
                        Debug.LogWarning("The connection already got a parent node set");
                    }
                }
            }
        }

        //Draw the button for adding a child if the node isn't a leaf
        if (nodeType != NodeTypes.Leaf) {
            if (GUI.Button(addChildButton, "")) {
                if ((nodeType == NodeTypes.Inverter && children.Count >= 1) || (nodeType == NodeTypes.Succeeder && children.Count >= 1)) {
                    Debug.LogWarning("A decorater can only have one child");
                } else {
                    if (treeMaker.currentConnection == null) {
                        new UINodeConnection(null, this, treeMaker);
                    } else {
                        if (!treeMaker.currentConnection.GotParent()) {
                            if (treeMaker.currentConnection.GetChild() != this) {
                                treeMaker.currentConnection.FinishConnection(this, treeMaker);
                            } else {
                                Debug.LogWarning("A node can't connect to itself");
                            }
                        } else {
                            Debug.LogWarning("The current connection already got a child node set");
                        }
                    }
                }
            }
        }

        //Based on the node type, draw a texture while the node is maximized to indicate the type. The rect sets the position, width, and height of the texture
        if (doDrawImage) {
            if (!isRoot) {
                switch (nodeType) {
                    case NodeTypes.Inverter:
                        GUI.DrawTexture(new Rect(drawRect.width / 2 - imageSize[0] / 2, 25, imageSize[0], imageSize[1]), treeMaker.inverter);
                        break;
                    case NodeTypes.Leaf:
                        GUI.DrawTexture(new Rect(drawRect.width / 2 - imageSize[0] / 2, 25, imageSize[0], imageSize[1]), treeMaker.leaf);
                        break;
                    case NodeTypes.Selector:
                        GUI.DrawTexture(new Rect(drawRect.width / 2 - imageSize[0] / 2, 25, imageSize[0], imageSize[1]), treeMaker.prioritySelector);
                        break;
                    case NodeTypes.Sequence:
                        GUI.DrawTexture(new Rect(drawRect.width / 2 - imageSize[0] / 2, 25, imageSize[0], imageSize[1]), treeMaker.sequence);
                        break;
                    case NodeTypes.Succeeder:
                        GUI.DrawTexture(new Rect(drawRect.width / 2 - imageSize[0] / 2, 25, imageSize[0], imageSize[1]), treeMaker.succeeder);
                        break;
                }
            } else {
                GUI.DrawTexture(new Rect(drawRect.width / 2 - imageSize[0] / 2, 8 + imageSize[1] / 2, imageSize[0], imageSize[1]), treeMaker.root);
            }
        }
        GUILayout.EndArea();
        #endregion

        if (nodeType == NodeTypes.Inverter || nodeType == NodeTypes.Succeeder) {
            if (children.Count > 1) {
                int iterations = children.Count;
                for (int i = 1; i < iterations; i++) {
                    treeMaker.FindImproperConnections(this);
                    if (children.Count > 1)
                        children.RemoveAt(1);
                }
            }
        }
        if (nodeType == NodeTypes.Leaf && children.Count != 0) {
            children.Clear();
            treeMaker.FindImproperConnections(this);
        }
        Drag(drawRect);
    }

    public bool IsConnectedToRoot() {
        if (isRoot)
            return true;
        else if (parent != null)
            return parent.IsConnectedToRoot();
        else
            return false;
    }

    public void MakeRoot() => isRoot = true;

    public void SetTypeToRoot() => nodeType = NodeTypes.Selector;

    public void Resize(float direction, UINode root) {
        boxSize[0] = defaultBoxSize[0] * treeMaker.sizeLevel;
        boxSize[1] = defaultBoxSize[1] * treeMaker.sizeLevel;

        if (treeMaker.sizeLevel < 0.8f)
            doDrawImage = false;
        else {
            doDrawImage = true;
            imageSize[0] = defaultImageSize[0] * treeMaker.sizeLevel;
            imageSize[1] = defaultImageSize[1] * treeMaker.sizeLevel;
        }

        if (treeMaker.sizeLevel > treeMaker.zoomMin && treeMaker.sizeLevel < treeMaker.zoomMax) {
            if (direction > 0) {
                pos += new Vector2((isRoot ? 0 : pos.x < root.pos.x ? -1 : 1) * 6, isRoot ? -1 : 1);
            } else if (direction < 0) {
                pos -= new Vector2((isRoot ? 0 : pos.x < root.pos.x ? -1 : 1) * 6, isRoot ? -1 : 1);
            }
        }
    }

    public void SetName(string name) {
        this.name = name;
        OnGUI();
    }

    public string GetNodeName() {
        return name;
    }

    public void SetParent(UINode parent) => this.parent = parent;

    public void AddChild(UINode child) => children.Add(child);

    public void RemoveChild(UINode child) {
        int index = -1;
        for (int i = 0; i < children.Count; i++) {
            if (children[i] == child) {
                index = i;
                break;
            }
        }
        if (index == -1)
            return;
        children.RemoveAt(index);
    }

    public NodeTypes GetNodeType() {
        return nodeType;
    }

    public void SetNodeType(NodeTypes type) => nodeType = type;

    public void ParseNodeInfoToEditPanel() => treeMaker.PassNodeInformation(name, nodeType);

    public void SetExecutionOrderIndicesInConnections() {
        List<UINodeConnection> connections = treeMaker.nodeConnections.FindAll(x => x.GetParent() == this);
        foreach (UINodeConnection connection in connections) {
            connection.SetExecutionOrderIndex(children.FindIndex(x => connection.GetChild().name == x.name));
        }
    }
}