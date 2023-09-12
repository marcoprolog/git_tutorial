#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleBehaviourTree {
    public class TreeHandler : MonoBehaviour {
        [SerializeField] private TreeObject treeObj = null;
        public bool isDebugging = false;
        [SerializeField] private List<EventListItem> leafMethods = new List<EventListItem>();
        private Node? previousNode = null;
        private List<Node> nodes = new List<Node>();

        private void OnValidate() {
            if (treeObj != null) {
                List<UINode> uiNodesLeafs = treeObj.nodes.FindAll(x => x.GetNodeType() == NodeTypes.Leaf);
                if (leafMethods.Count < uiNodesLeafs.Count) {
                    for (int i = 0; i < uiNodesLeafs.Count; i++) {
                        if (leafMethods.Find(x => x.GetLeafName() == uiNodesLeafs[i].name) == null) {
                            leafMethods.Add(new(uiNodesLeafs[i].name));
                        }
                    }
                } else if (leafMethods.Count > uiNodesLeafs.Count) {
                    leafMethods.Clear();
                    for (int i = 0; i < uiNodesLeafs.Count; i++) {
                        if (leafMethods.Find(x => x.GetLeafName() == uiNodesLeafs[i].name) == null) {
                            leafMethods.Add(new(uiNodesLeafs[i].name));
                        }
                    }
                }
            } else
                leafMethods.Clear();
        }

        private void Start() {
            CreateTree(BuildNodeDefinition());
        }

        /// <summary>
        /// Build the behaviour tree based on a given list of node definitions. Set <paramref name="_isDebugging"/> to <see langword="true"/> to toggle debugging (default is <see langword="false"/>)
        /// </summary>
        /// <param name="nodeDefinitions"></param>
        public void CreateTree(List<NodeDefinition> nodeDefinitions) {
            Node node;
            for (int i = 0; i < nodeDefinitions.Count; i++) {
                node = new Node(
                    _name: nodeDefinitions[i].nodeName,
                    _type: nodeDefinitions[i].nodeType,
                    _parent: nodes.Find(x => x.name.Equals(nodeDefinitions[i].parentName)),
                    _leafMethod: nodeDefinitions[i].leafAction
                );
                node.parent?.AddChild(node);
                nodes.Add(node);

                if (isDebugging)
                    Debug(node, $"Node created as child of {node.parent}");
            }
        }

        /// <summary>
        /// Execute the tree, traversing from a default root node through the nodes given at initialization
        /// </summary>
        /// <returns><see langword="true"/> if the tree finished execution and reset, otherwise <see langword="false"/> where the tree will continue execution of the same leaf next run</returns>
        /// <exception cref="InvalidOperationException">Thrown if a reference in the tree returns null. This should be handled in the user's code</exception>
        public bool Execute() {            
            Node currentNode = nodes[0];

            //Result must be nullable and set to 'null' at the beginning or it might trigger 'end execution' when reaching composite nodes
            //Remember to check the value using the == comparator if checked at the same time as a normal bool value in an if-statement!
            bool? result = null;
            while (true) {
                if (currentNode == null)
                    throw new InvalidOperationException($"A node wasn't connected correctly, resulting in a null-reference during tree execution. Previous node was: [Name:{previousNode!.name}, Type: {previousNode.type}]");

                if (!currentNode.isRunning)
                    currentNode.StartExecution();

                if (isDebugging)
                    Debug(currentNode, $"Current node for execution");

                switch (currentNode.type) {
                    case NodeTypes.Leaf:
                        //IF leaf action returns true THEN end leaf execution
                        //ELSE return false to indicate the action wasn't completed yet
                        //AND out the result (bool?) from the leaf action to the result variable
                        if (currentNode.LeafMethod(out result)) {
                            if (isDebugging)
                                Debug(currentNode, $"Completed leaf method, result is {result}");                            

                            currentNode.EndExecution();
                            previousNode = currentNode;
                            currentNode = currentNode.parent!;
                        } else {
                            if (isDebugging)
                                Debug(currentNode, $"Didn't complete leaf method, ending tree execution and returning false");

                            previousNode = currentNode;
                            return false;
                        }
                        break;
                    case NodeTypes.Selector:
                        //IF result is 'true' or all children have been executed THEN end execution and select parent node
                        //ELSE IF a child is still running THEN select that child
                        //ELSE select the next child and increment child index
                        if ((result != null && result.Value) || currentNode.AllChildrenHaveBeenExecuted()) {
                            if (isDebugging)
                                Debug(currentNode, $"Completed execution, result is {result}");

                            currentNode.EndExecution();
                            previousNode = currentNode;
                            currentNode = currentNode.parent!;

                            if (currentNode == null && previousNode.name == "Root") {
                                for (int i = 0; i < nodes.Count; i++)
                                    nodes[i].ResetExecution();
                                return true;
                            }                                
                        } else if (currentNode.IsAChildStillRunning()) {
                            if (isDebugging)
                                Debug(currentNode, $"Seleting an already running child");

                            previousNode = currentNode;
                            currentNode = currentNode.GetRunningChild();
                        } else {
                            if (isDebugging)
                                Debug(currentNode, $"Seleting the next child");

                            previousNode = currentNode;
                            currentNode = currentNode.GetChildNode();
                            previousNode.IncrementCurrentChildNode();
                        }
                        break;
                    case NodeTypes.Sequence:
                        //IF result is 'false' or all children have been executed THEN end execution and select parent node
                        //ELSE IF a child is still running THEN select that child
                        //ELSE select the next child and increment child index
                        if ((result != null && !result.Value) || currentNode.AllChildrenHaveBeenExecuted()) {
                            if (isDebugging)
                                Debug(currentNode, $"Completed execution, result is {result}");

                            currentNode.EndExecution();
                            previousNode = currentNode;
                            currentNode = currentNode.parent!;
                        } else if (currentNode.IsAChildStillRunning()) {
                            if (isDebugging)
                                Debug(currentNode, $"Seleting an already running child");

                            previousNode = currentNode;
                            currentNode = currentNode.GetRunningChild();
                        } else {
                            if (isDebugging)
                                Debug(currentNode, $"Seleting the next child");

                            previousNode = currentNode;
                            currentNode = currentNode.GetChildNode();
                            previousNode.IncrementCurrentChildNode();
                        }
                        break;
                    case NodeTypes.Inverter:
                        //IF all children have been executed THEN flip result's value, end execution, and select the parent node
                        //ELSE select the child node
                        if (currentNode.AllChildrenHaveBeenExecuted()) {
                            if (isDebugging)
                                Debug(currentNode, $"Flipping result");

                            result = !result;
                            currentNode.EndExecution();
                            previousNode = currentNode;
                            currentNode = currentNode.parent!;
                        } else {
                            if (isDebugging)
                                Debug(currentNode, $"Seleting child node");

                            previousNode = currentNode;
                            currentNode = currentNode.GetChildNode();
                        }
                        break;
                    case NodeTypes.Succeeder:
                        //IF all children have been executed THEN set result's value to true, end execution, and select the parent node
                        //ELSE select the child node
                        if (currentNode.AllChildrenHaveBeenExecuted()) {
                            if (isDebugging)
                                Debug(currentNode, $"Setting result to true");

                            result = true;
                            currentNode.EndExecution();
                            previousNode = currentNode;
                            currentNode = currentNode.parent!;
                        } else {
                            if (isDebugging)
                                Debug(currentNode, $"Seleting child node");

                            previousNode = currentNode;
                            currentNode = currentNode.GetChildNode();
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Build a list of node definitions from a tree object
        /// </summary>
        /// <returns>A list of node definitions that can be parsed to a new instance of TreeHandler as an executable behaviour tree</returns>
        public List<NodeDefinition> BuildNodeDefinition() {
            if (treeObj == null)
                return null;

            List<NodeDefinition> nodeDefinitions = new();

            //public NodeDefinition(string _nodeName, NodeTypes _nodeType, string _parentName, Func<bool?>? _leafAction)
            for (int i = 0; i < treeObj.nodes.Count; i++) {
                nodeDefinitions.Add(new(
                    treeObj.nodes[i].name,
                    treeObj.nodes[i].GetNodeType(),
                    treeObj.nodes[i].parent?.name,
                    treeObj.nodes[i].GetNodeType() == NodeTypes.Leaf ? leafMethods.Find(x => x.GetLeafName() == treeObj.nodes[i].name).GetLeafMethod() : null
                ));
            }

            return nodeDefinitions;
        }

        private void Debug(Node node, string msg) => UnityEngine.Debug.Log($"Node [Name: {node.name}, Type: {node.type}], message: {msg}");
    }

    public class NodeDefinition {
        public readonly string nodeName;
        public readonly NodeTypes nodeType;
        public readonly string parentName;
        public readonly UnityEvent<Node>? leafAction;

        /// <summary>
        /// Create a node definition used to create a node and its parental relation during tree generation
        /// </summary>
        /// <param name="_nodeName"></param>
        /// <param name="_nodeType"></param>
        /// <param name="_parentName"></param>
        /// <param name="_leafAction"></param>
        public NodeDefinition(string _nodeName, NodeTypes _nodeType, string _parentName, UnityEvent<Node>? _leafAction) {
            nodeName = _nodeName;
            nodeType = _nodeType;
            parentName = _parentName;
            leafAction = _leafAction;
        }
    }

    [Serializable]
    public class EventListItem {
        [SerializeField][ReadOnly] private string leafName = "";
        [SerializeField] private UnityEvent<Node> leafMethod;

        public EventListItem(string name) => leafName = name;

        public string GetLeafName() { return leafName; }

        public UnityEvent<Node> GetLeafMethod() { return leafMethod; }
    }

    public class ReadOnlyAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label, true);                                  //Get the height of the object and its children (ture parameter adds children)
        }

        //This method stops the GUI from drawing, then draw the property fields using the attribute, and then it allows the GUI to draw again making the rest as it should be
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            GUI.enabled = false;                                                                        //Stop drawing the GUI
            EditorGUI.PropertyField(position, property, label, true);                                   //Write the property field including children
            GUI.enabled = true;                                                                         //Start drawing the GUI again
        }
    }
}
