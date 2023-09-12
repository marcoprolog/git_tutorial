#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

namespace SimpleBehaviourTree {
    public class Node {
        public readonly string name;
        public readonly NodeTypes type;
        public readonly Node? parent;
        public bool? actionResult { get; private set; } = null;
        public bool executionComplete { get; private set; } = false;
        public bool isRunning { get; private set; }
        private List<Node> children;
        private int childNodeIndex = 0;
        private UnityEvent<Node>? leafMethod;

        /// <summary>
        /// Create a new node for the behaviour tree
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_type"></param>
        /// <param name="_parent"></param>
        internal Node(string _name, NodeTypes _type, Node? _parent, UnityEvent<Node>? _leafMethod) {
            name = _name;
            type = _type;
            parent = _parent;
            leafMethod = _leafMethod;
            children = new List<Node>();
        }

        /// <summary>
        /// Add a new child node. This should only be done during tree initialization!
        /// </summary>
        /// <param name="child"></param>
        internal void AddChild(Node child) => children.Add(child);

        /// <summary>
        /// Set running state to true
        /// </summary>
        internal void StartExecution() => isRunning = true;

        /// <summary>
        /// Set running state to false and reset current child index to 0
        /// </summary>
        internal void EndExecution() {
            isRunning = false;
            childNodeIndex = 0;
            executionComplete = true;
        }

        /// <summary>
        /// Reset the execution of the node for a new tree execution
        /// </summary>
        internal void ResetExecution() => executionComplete = false;

        /// <summary>
        /// Get the child node that is set to be executed
        /// </summary>
        /// <returns>Current child node</returns>
        internal Node GetChildNode() {
            return children[childNodeIndex];
        }

        /// <summary>
        /// Get the first child node that is still running
        /// </summary>
        /// <returns></returns>
        internal Node GetRunningChild() {
            return children.First(x => x.isRunning);
        }

        /// <summary>
        /// Check if all child nodes have been executed
        /// </summary>
        /// <returns><see langword="true"/> if all children have been executed, otherwise <see langword="false"/></returns>
        internal bool AllChildrenHaveBeenExecuted() {
            return children.All(x => x.executionComplete);
        }

        /// <summary>
        /// Check if a child node is still running
        /// </summary>
        /// <returns></returns>
        internal bool IsAChildStillRunning() {
            return children.Any(x => x.isRunning);
        }

        /// <summary>
        /// Increment the index of the current child node or reset if the last child was executed
        /// </summary>
        /// <returns><see langword="true"/> if the last child was executed, otherwise <see langword="false"/></returns>
        internal void IncrementCurrentChildNode() {
            childNodeIndex++;
            if (childNodeIndex == children.Count)
                childNodeIndex = 0;
        }

        /// <summary>
        /// Executes the action of the leaf. Should only be called if 'type = Leaf'
        /// </summary>
        /// <param name="result">Success of the action; should be <see langword="null"/> if the action didn't complete</param>
        /// <returns><see langword="true"/> if the action was completed, otherwise <see langword="false"/></returns>
        internal bool LeafMethod(out bool? result) {
            leafMethod!.Invoke(this);
            result = actionResult;
            return result == null ? false : true;
        }

        public void SetActionResult(bool? result) => actionResult = result;
    }
}