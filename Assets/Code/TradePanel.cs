using SimpleJSON;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class TradePanel : MonoBehaviour
{
    [SerializeField]
    private Text totalSpendText, totalQuantityText, averageBuyInText, coinName, totalInvesement;
    [SerializeField]
    private InputField dateInput, pricePaidInput, quantityInput;
    [SerializeField]
    private PurchaseHistoryTrack purchaseHistoryPrefab;
    [SerializeField]
    private ScrollRect purchaseHistoryContainer;
    [SerializeField]
    private CanvasGroup tradesUI, addTradePopup;
    private string openCoinSymbol;
    private double totalSpend;
    private double totalQuantity;
    private double averageBuyInCost;
    private string tradesPath;

    private void Start()
    {
        tradesPath = Path.Combine(Application.persistentDataPath, "trades.json");
    }
    public void OpenAddTradePopup()
    {
        addTradePopup.On();
    }

    //public void GenerateTrade()
    //{
    //    var trade = new Trade()
    //    {
    //        Date = dateInput.text,
    //        PricePaid = Convert.ToDouble(pricePaidInput.text),
    //        coinQuantity = Convert.ToDouble(quantityInput.text),
    //    };

    //    AddPurchase(openCoinSymbol, trade);
    //    dateInput.text = string.Empty;
    //    pricePaidInput.text = string.Empty;
    //    quantityInput.text = string.Empty;
    //    addTradePopup.Off();
    //}

    public void OpenTradesForCoin(string symbol)
    {
        openCoinSymbol = symbol;
        coinName.text = symbol;
        DisplayAllPurchases(openCoinSymbol);
        tradesUI.On();
        DisplayTotalInvestment();
    }

    public void CloseTradesForCoins()
    {
        openCoinSymbol = string.Empty;
        tradesUI.Off();
        DisplayTotalInvestment();
    }

    public void DisplayAllPurchases(string symbol)
    {
        totalSpend = totalQuantity = averageBuyInCost = 0;
        purchaseHistoryContainer.content.ClearChildren();
        var history = GlobalData.Instance.GetCoinTradeHistory(symbol);
        var historyCount = history.Count;

        foreach (var trade in history)
        {
            totalSpend += trade.PricePaid;
            totalQuantity += trade.coinQuantity;
            if (trade.coinQuantity < 0)
                historyCount--;
            else
                averageBuyInCost += trade.CoinPrice;


            var purchaseTrack = Instantiate(purchaseHistoryPrefab, purchaseHistoryContainer.content);
            purchaseTrack.UpdateView(trade);
        }

        averageBuyInCost = averageBuyInCost / historyCount;

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

    private void DisplayTotalInvestment()
    {
        totalInvesement.text = "€" + GlobalData.Instance.TotalSpend.ToString("N2");
    }
}
