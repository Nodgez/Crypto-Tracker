using SimpleJSON;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class TradePanel : MonoBehaviour
{
    [SerializeField]
    private Text totalSpendText, totalQuantityText, averageBuyInText, coinName, currentValueText, percentageDiffText;
    [SerializeField]
    private PurchaseHistoryTrack purchaseHistoryPrefab;
    [SerializeField]
    private ScrollRect purchaseHistoryContainer;
    [SerializeField]
    private CanvasGroup tradesUI;
    private double totalSpend;
    private double totalQuantity;
    private double averageBuyInCost;
    private string tradesPath;

    private void Start()
    {
        tradesPath = Path.Combine(Application.persistentDataPath, "trades.json");
    }

    public void OpenTradesForCoin(Coin coin)
    {
        coinName.text = coin.symbol;
        DisplayAllPurchases(coin);
        tradesUI.On();
    }

    public void CloseTradesForCoins()
    {
        tradesUI.Off();
    }

    public void DisplayAllPurchases(Coin coin)
    {
        totalSpend = totalQuantity = averageBuyInCost = 0;
        purchaseHistoryContainer.content.ClearChildren();
        var history = GlobalData.Instance.GetCoinTradeHistory(coin.symbol);
        var historyCount = history.Count;

        foreach (var trade in history)
        {
            totalSpend += trade.PricePaid;

            var purchaseTrack = Instantiate(purchaseHistoryPrefab, purchaseHistoryContainer.content);
            purchaseTrack.UpdateView(trade);
        }

        totalQuantity = GlobalData.Instance.GetCurrencyTotal(coin.symbol);
        averageBuyInCost = totalSpend / totalQuantity;

        var currentValue = coin.CurrentPrice * totalQuantity;
        var percentageDifference = (currentValue / totalSpend) * 100;
        var percentageColor = percentageDifference > 100 ? "<color=green>" : "<color=red>";
        currentValueText.text = "Current Value: €" + currentValue.ToString("N2");
        percentageDiffText.text = "Diff: "+ percentageColor + percentageDifference.ToString("N2") + "%</color>";
        totalQuantityText.text = "Quantity: " + totalQuantity.ToString();
        totalSpendText.text = "Spend: €" + totalSpend.ToString();
        averageBuyInText.text = "AVG Cost: €" + averageBuyInCost.ToString("N2");
    }

    public JSONArray GetCoinPurchaseHistory(string coin)
    {
        var tradesFile = "{}";
        if (File.Exists(tradesPath))
            tradesFile = File.ReadAllText(tradesPath);
        var purchaseObject = JSON.Parse(tradesFile);
        return purchaseObject[coin].AsArray;
    }
}
