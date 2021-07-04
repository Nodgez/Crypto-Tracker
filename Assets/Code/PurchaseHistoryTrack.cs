using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PurchaseHistoryTrack : MonoBehaviour
{
    [SerializeField]
    private Text dateOfPurchase, purchasePrice, coinPrice, coinQuantity;

    public void UpdateView(Trade trade)
    {
        dateOfPurchase.text = "Date: " + trade.Date;
        purchasePrice.text = trade.fromCurrency + " Cost: " + trade.PricePaid.ToString();
        coinPrice.text = trade.fromCurrency + " Worth: " + trade.CoinPrice.ToString(trade.CoinPrice < 1.0 ? "N5" : "N2");
        coinQuantity.text = "Owned: " + trade.coinQuantity.ToString();
    }
}
