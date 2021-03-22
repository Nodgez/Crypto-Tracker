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
    [SerializeField]
    private Button delete;

    public void AddDelete(UnityAction onDelete)
    {
        delete.onClick.AddListener(onDelete);
        delete.onClick.AddListener(() => Destroy(this.gameObject));
    }

    public void UpdateView(Trade trade)
    {
        dateOfPurchase.text = "Date: " + trade.Date;
        purchasePrice.text = "Cost: €" + trade.PricePaid.ToString();
        coinPrice.text = "Worth: €" + trade.CoinPrice.ToString();
        coinQuantity.text = "Owned: " + trade.coinQuantity.ToString();
    }
}
