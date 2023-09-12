//Developed by Tobias Oliver Jensen, 2023
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public enum NodeTypes { Inverter, Leaf, Selector, Sequence, Succeeder }
public class TreeMaker : EditorWindow {
    public Texture inverter = null;
    public Texture leaf = null;
    public Texture prioritySelector = null;
    public Texture root = null;
    public Texture sequence = null;
    public Texture succeeder = null;
    public Texture delete = null;
    public float sizeLevel = 1f;
    public readonly float zoomMin = 0.4f;
    public readonly float zoomMax = 1.5f;
    public List<UINodeConnection> nodeConnections { get; private set; } = new List<UINodeConnection>();

    private List<UINode> nodes = new List<UINode>();
    private Rect dropTargetRect = new Rect(10.0f, 10.0f, 30.0f, 30.0f);
    public UINodeConnection currentConnection { get; internal set; } = null;
    public Vector3 mousePos { get; internal set; } = Vector2.zero;

    private bool draggingAll = false;
    private Vector2 draggingAllStart = Vector2.zero;
    private Vector2 draggingAllCurrent = Vector2.zero;
    private UINode treeRoot = null;

    [Header("Node informations")]
    private UINode selectedNode = null;
    private string nodeName = "";
    private NodeTypes nodeType = NodeTypes.Selector;

    [Header("Tree Options")]
    private string treeName = "";
    private string error = "";
    private bool isTreeNameInUse = false;
    private TreeObject loadedTree = null;
    private bool isTreeLoaded = false;

    [MenuItem("Window/Tree Maker")]
    public static void Launch() => GetWindow(typeof(TreeMaker)).Show();

    void OnInspectorUpdate() => Repaint();

    public void PassNodeInformation(string nodeName, NodeTypes nodeType) {
        this.nodeName = nodeName;
        this.nodeType = nodeType;
    }

    void NodeEditPanel() {
        #region Basic Tree-Building Fields
        //Define edit panel
        Rect editPanel = new Rect(0, 0, 200, position.height);
        GUILayout.BeginArea(editPanel, GUI.skin.GetStyle("Box"));
        GUI.color = Color.white;

        if (GUILayout.Button("Add Node"))
            nodes.Add(new UINode("Node " + (nodes.Count), new Vector2(210, 50), this));

        //Define node edit inputs
        EditorGUI.BeginChangeCheck();
        GUILayout.Label("Node name:");
        nodeName = EditorGUILayout.TextField(nodeName);

        if (selectedNode != null && !selectedNode.isRoot) {
            GUILayout.Label("Node type:");
            nodeType = (NodeTypes)EditorGUILayout.EnumPopup(nodeType);
        } else {
            GUILayout.Label("");
            GUILayout.Label("");
        }

        if (EditorGUI.EndChangeCheck()) {
            selectedNode.SetName(nodeName);
            selectedNode.SetNodeType(nodeType);
        }

        //Create some spaces
        GUILayout.Label("");
        GUILayout.Label("");

        //Define the delete and clear buttons
        if (selectedNode != null) {
            if (GUILayout.Button("Delete Current Node")) {
                DeleteNode();
                Debug.Log("Current node deleted");
            }
        }

        if (GUILayout.Button("Clear Everything"))
            ClearWorkSpace();

        if (GUILayout.Button("Reset Size Level"))
            ResetSizeLevel();

        GUILayout.Label("Current size level: x" + sizeLevel.ToString("0.0"));

        GUILayout.EndArea();
        #endregion

        #region Saving and Loading Trees
        //Define a rect for the bottom of the edit panel to handle inputs regarding TreeObjects
        Rect bottomRect;
        if (loadedTree == null)
            bottomRect = new Rect(0, position.height - 120, 200, position.height);
        else
            bottomRect = new Rect(0, position.height - 60, 200, position.height);

        GUILayout.BeginArea(bottomRect, GUI.skin.GetStyle("Box"));
        loadedTree = (TreeObject)EditorGUILayout.ObjectField(loadedTree, typeof(TreeObject), true);

        //Change UI if a tree is loaded
        if (loadedTree == null) {
            //Reset if a tree has been previously loaded
            if (isTreeLoaded) {
                isTreeLoaded = false;
                ClearWorkSpace();
                loadedTree = null;
                isTreeLoaded = false;
            }

            //Define inputs to create a new TreeObject
            GUILayout.Label("Tree name:");
            treeName = EditorGUILayout.TextField(treeName);
            error = "";
            isTreeNameInUse = false;
            string temp;

            foreach (string guid in AssetDatabase.FindAssets(treeName, new[] { "Assets/Trees" })) {
                temp = AssetDatabase.GUIDToAssetPath(guid);

                if (temp.Contains(treeName)) {
                    error = "Name already in use!";
                    isTreeNameInUse = true;
                    break;
                }
            }
            GUILayout.Label(error);

            //Define button to create a ScriptableObject containing the tree
            if (GUILayout.Button("Create Tree Object"))
                CreateBehaviorTreeObject();
        } else {
            if (!isTreeLoaded) {
                //Clear workspace to load tree
                isTreeLoaded = true;
                treeName = loadedTree.name;
                ClearWorkSpace();

                //Create each node of the tree
                foreach (UINode n in loadedTree.nodes)
                    nodes.Add(new UINode(n, this));
                treeRoot = nodes[0];

                //Create the connections between the nodes
                foreach (UINodeConnection c in loadedTree.nodeConnections)
                    nodeConnections.Add(new UINodeConnection(c, this));

                //Clean all connections if the roots child count is 0
                if (nodes[0].children.Count == 0 && nodeConnections.Count > 0) {
                    for (int i = 0; i < nodeConnections.Count;)
                        nodeConnections[i].DeleteConnection(false);
                }

                //Delete the current connection being made
                if (currentConnection != null)
                    currentConnection.DeleteConnection(true);

                foreach (UINode n in nodes)
                    if (n.children.Count > 0)
                        n.SetExecutionOrderIndicesInConnections();
            }

            //Define button to save the current tree
            if (GUILayout.Button("Save changes to Tree"))
                SaveTree();
        }
        GUILayout.EndArea();
        #endregion
    }

    public void OnGUI() {
        //If the tree root is missing, create it
        if (treeRoot == null) {
            nodes.Add(treeRoot = new UINode("Root", new Vector2(position.width / 2, 50), this));
            nodes[0].MakeRoot();
            nodes[0].SetTypeToRoot();
        }

        //Draw window UI
        NodeEditPanel();

        //Define necessary variables for dragging and selecting nodes
        wantsMouseMove = true;
        UINode toFront, dropDead;
        bool previousState;
        Color color;
        toFront = dropDead = null;
        UINode n;

        #region Dragging and Coloring
        //Iterate through all nodes to handle events
        for (int i = 0; i < nodes.Count; i++) {
            n = nodes[i];
            previousState = n.isDragging;

            //Set rhe node's color depending on its state, draw the node, and reset the GUI color
            color = GUI.color;
            if (n.isSelected)
                GUI.color = Color.yellow;
            else if (n.isRoot && n.isDragging)
                GUI.color = Color.grey;
            else
                GUI.color = color;
            n.OnGUI();
            GUI.color = color;

            //Handle dragging of the node
            if (n.isDragging) {
                n.isSelected = true;
                selectedNode = n;
                n.ParseNodeInfoToEditPanel();
                foreach (UINode otherNodes in nodes) {
                    if (otherNodes != n) {
                        otherNodes.isSelected = false;
                        GUI.color = color;
                    }
                }
                if (nodes.IndexOf(n) != nodes.Count - 1) {
                    toFront = n;
                }
            } else if (previousState) {
                if (dropTargetRect.Contains(Event.current.mousePosition)) {
                    dropDead = n;
                }
            }
        }

        //Move node to front
        if (toFront != null) {
            nodes.Remove(toFront);
            nodes.Add(toFront);
        }

        //Drop the node
        if (dropDead != null) {
            nodes.Remove(dropDead);
        }
        #endregion

        //Update all the node connections
        try {
            foreach (UINodeConnection nc in nodeConnections) {
                nc.OnGUI(this);
            }
        } catch { }

        #region Event Checks
        //Handle mouse events
        if (Event.current.type == EventType.ScrollWheel) {
            //If mousewheel is scrolled, change the node size
            if (Event.current.delta.y < 0)
                SetSizeLevel(0.1f);
            else
                SetSizeLevel(-0.1f);
            Event.current.Use();
        } else if (Event.current.type == EventType.MouseDown) {
            if (Event.current.button == 0) {
                //Left mouse button is down
                //If mouse is hovering a connection point, start creating a new connection
                bool isHoveringConnection = false;
                foreach (UINodeConnection nc in nodeConnections) {
                    if (nc.connectionRect.Contains(Event.current.mousePosition)) {
                        isHoveringConnection = true;
                        break;
                    }
                }

                //If not creating a new connection, start dragging all nodes
                if (!isHoveringConnection) {
                    draggingAll = true;
                    draggingAllStart = Event.current.mousePosition;
                    draggingAllCurrent = Event.current.mousePosition;
                    foreach (UINode to in nodes) {
                        to.SetDraggingAll(true);
                    }
                }
            } else if (Event.current.button == 1) {
                //Right mouse button is down
                //If a connection is currently being made (not completed), delete it
                if (currentConnection != null) {
                    currentConnection.DeleteConnection(false);
                    currentConnection = null;
                }
            }
            Event.current.Use();
        } else if (Event.current.type == EventType.MouseUp) {
            //All mouse buttons are up
            //Stop dragging all nodes
            draggingAll = false;
            foreach (UINode to in nodes) {
                to.SetDraggingAll(false);
            }
            Event.current.Use();
        } else if (Event.current.type == EventType.MouseDrag) {
            //If mouse was dragged, update current drag position
            draggingAllCurrent = Event.current.mousePosition;
            Event.current.Use();
        } else if (Event.current.type == EventType.MouseMove) {
            //If mouse moved, update mouse position
            mousePos = Event.current.mousePosition;
        }
        //If dragging all nodes, update their positions
        if (draggingAll) {
            foreach (UINode to in nodes) {
                to.DraggingAllSetPosition(draggingAllStart - draggingAllCurrent);
            }
        }
        #endregion

        //If a connection isn't currently being created, check all connections and remove broken ones (missing either parent or child)
        if (currentConnection == null) {
            int index = -1;
            for (int i = 0; i < nodeConnections.Count; i++) {
                if (!nodeConnections[i].GotParent() || !nodeConnections[i].GotChild()) {
                    index = i;
                    break;
                }
            }
            if (index != -1)
                RemoveConnection(index);
        } else {
            if (currentConnection.GotChild() && currentConnection.GotParent()) {
                currentConnection.DeleteConnection(true);
                currentConnection = null;
            }
        }

        NodeEditPanel();
    }

    void DeleteNode(UINode nodeToDelete = null) {
        if (nodeToDelete != null) {
            for (int i = 0; i < nodeConnections.Count; i++) {
                if (nodeConnections[i].GetChild() == nodeToDelete || nodeConnections[i].GetParent() == nodeToDelete) {
                    nodeConnections[i].DeleteConnection(false);
                }
            }
            for (int i = 0; i < nodes.Count; i++) {
                if (nodes[i] == nodeToDelete) {
                    nodes.RemoveAt(i);
                }
            }
        } else {
            for (int i = 0; i < nodeConnections.Count; i++) {
                if (nodeConnections[i].GetChild() == selectedNode || nodeConnections[i].GetParent() == selectedNode) {
                    nodeConnections[i].DeleteConnection(false);
                }
            }
            for (int i = 0; i < nodes.Count; i++) {
                if (nodes[i] == selectedNode) {
                    nodes.RemoveAt(i);
                }
            }
            selectedNode = null;
        }
    }

    public void ResetSizeLevel() {
        sizeLevel = 1;
        for (int i = 0; i < nodes.Count; i++)
            nodes[i].Resize(0, nodes[0]);
    }

    public void SetSizeLevel(float direction) {
        sizeLevel += direction;

        if (sizeLevel < zoomMin)
            sizeLevel = zoomMin;
        else if (sizeLevel > zoomMax)
            sizeLevel = zoomMax;

        for (int i = 0; i < nodes.Count; i++)
            nodes[i].Resize(direction, nodes[0]);
    }

    public void AddNodeConnection(UINodeConnection nodeConnection) => nodeConnections.Add(nodeConnection);

    public void SetCurrentConnection(UINodeConnection nodeConnection) => currentConnection = nodeConnection;

    public void RemoveConnection(int index) {
        if (index < nodeConnections.Count)
            nodeConnections.RemoveAt(index);
    }

    public void FindImproperConnections(UINode nodeToCheckAgainst) {
        int connectionToRemove = -1;
        nodeConnections.ForEach(n => { if (n.GetParent() == nodeToCheckAgainst) connectionToRemove = GetConnectionIndex(n); });
        if (connectionToRemove != -1) {
            nodeConnections[connectionToRemove].DeleteConnection(false);
        }
    }

    public int GetNodeIndex(UINode node) => nodes.IndexOf(node);

    public List<UINode> GetNodes() { return nodes; }

    public UINode GetNodeFromIndex(int index) { return nodes[index]; }

    public int GetConnectionIndex(UINodeConnection nodeConnection) => nodeConnections.IndexOf(nodeConnection);

    void CreateBehaviorTreeObject() {
        if (isTreeNameInUse) {
            Debug.LogError("Name already in use!");
            return;
        }
        if (treeName.Equals(string.Empty)) {
            Debug.LogError("No name entered!");
            return;
        }
        SaveTree();
    }

    void ClearWorkSpace() {
        for (int i = 0; i < nodeConnections.Count; i = 0) {
            nodeConnections[i].DeleteConnection(false);
        }

        treeRoot = null;
        for (int i = 0; i < nodes.Count;) {
            DeleteNode(nodes[i]);
        }

        sizeLevel = 1;
    }

    void SaveTree() {
        TreeObject treeObj = CreateInstance<TreeObject>();
        treeObj.name = treeName;
        UINode currentNode;
        //Ensure root is the first node on the list, as it sometimes isn't (not sure how)
        currentNode = nodes.Find(x => x.isRoot);
        if (nodes.IndexOf(currentNode) != 0) {
            nodes.RemoveAt(nodes.IndexOf(currentNode));
            nodes.Insert(0, currentNode);
        }

        //Save a copy of all the nodes that are connected to the root to a new list
        List<UINode> rootedNodes = new();
        List<UINodeConnection> rootedConnections = new();
        currentNode = nodes[nodes.IndexOf(nodes.Find(x => x.isRoot))];
        rootedNodes.Add(new(currentNode));
        AddNodes(currentNode, rootedNodes);
        treeObj.nodes = new(rootedNodes);

        //Save a copy of all node connections to a new list 
        rootedConnections.Clear();
        for (int i = 0; i < nodeConnections.Count; i++)
            if (nodeConnections[i].IsFullyConnected())
                rootedConnections.Add(new(nodeConnections[i], treeObj.nodes));

        treeObj.nodeConnections = rootedConnections;

        if (!AssetDatabase.IsValidFolder($"Assets/Trees"))
            AssetDatabase.CreateFolder("Assets", "Trees");
        AssetDatabase.CreateAsset(treeObj, $"Assets/Trees/{treeName}.asset");
        AssetDatabase.SaveAssets();
        ClearWorkSpace();
    }

    private void AddNodes(UINode current, List<UINode> rootedNodes) {
        for (int i = 0; i < current.children.Count; i++) {
            rootedNodes.Add(new(current.children[i]));
            AddNodes(current.children[i], rootedNodes);
        }
    }
}