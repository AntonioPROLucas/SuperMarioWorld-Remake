using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class coin : MonoBehaviour
{
    public int Coins = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Coins = 1;
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider.gameObject.tag == "Player")
        {
            Destroy(gameObject);

            GameController.instance.totalCoins += Coins;
            GameController.instance.UpdateCoinText();
        }
    }
}
