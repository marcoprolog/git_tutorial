using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Tree", menuName = "ScriptableObjects/TreeObject", order = 1)]
public class TreeObject : ScriptableObject {
    public List<UINode> nodes = new List<UINode>();
    public List<UINodeConnection> nodeConnections = new List<UINodeConnection>();
}