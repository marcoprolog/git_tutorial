using SimpleBehaviourTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class BTMario : TreeHandler
{
    public Transform closestCoin;
    public int score;
    // Start is called before the first frame update
    void Start()
    {
        //Michelle making an annoying comment
    }

    // Update is called once per frame
    void Update()
    {
        //run the BT every frame
        Execute();
    }

    //step 1: change return type to void and have a Node as a parameter
    public void GetClosestCoin(Node node)
    {
        GameObject[] coins = GameObject.FindGameObjectsWithTag("Coin");

        //example of failure of the method
        if (coins.Length == 0)
            node.SetActionResult(false); //if no coins, the node fails

        float minDistance = float.PositiveInfinity;
        GameObject closestCoin = null;
        foreach (GameObject coin in coins)
        {
            float distance = Vector3.Distance(coin.transform.position, transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCoin = coin;
            }
        }
        //step 2: we can't return a value, so we should save relevant info some other way (in this case in a field)
        this.closestCoin =  closestCoin.transform;
        //step 3: add code to tell the BT if the node has succeeded/failed or it's running (by node.SetActionResult(null);)
        node.SetActionResult(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Coin")
        {
            score++;
            Debug.Log("Got a coin!");
            Destroy(other.gameObject);
        }
        if (other.gameObject.tag == "Boo")
        {
            Debug.Log("Mamma mia! [score:" + score + "]");
            Destroy(gameObject);
        }
    }
}
