using SimpleBehaviourTree;
using System.Collections;
using System.Collections.Generic;
//using System.Drawing.Design;
using UnityEditor;
using UnityEngine;

public class BTAgent : TreeHandler {
    private int breaker = 0;
    private bool? result = null;
    [SerializeField] private Transform point1;
    [SerializeField] private Transform point2;

    int healthPoints = 10;
    float damagePerSecond = 20.4f;

    void Update() {
        Execute();
    }

    public void MoveTowardsPoint1(Node node) {
        Debug.Log("Move 1");
        transform.position = Vector3.MoveTowards(transform.position, point1.transform.position, 1);
        node.SetActionResult(true);
    }

    public void Example(Node node)
    {
        //YOUR CODE
        node.SetActionResult(true);
    }

    public void CheckIfArrivedAtPoint1(Node node) {
        Debug.Log("Check 1");
        if (Vector3.Distance(transform.position, point1.position) < 2)
            node.SetActionResult(true);
        else
            node.SetActionResult(false);
    }

    public void MoveTowardsPoint2(Node node) {
        Debug.Log("Move 2");
        transform.position = Vector3.MoveTowards(transform.position, point2.transform.position, 1);
        node.SetActionResult(true);
    }

    public void CheckIfArrivedAtPoint2(Node node) {
        Debug.Log("Check 2");
        if (Vector3.Distance(transform.position, point1.position) < 2)
            node.SetActionResult(true);
        else
            node.SetActionResult(false);
    }
}
