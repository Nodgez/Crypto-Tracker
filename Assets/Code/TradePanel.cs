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

    public void GenerateTrade()
    {
        var trade = new Trade()
        {
            Date = dateInput.text,
            PricePaid = Convert.ToDouble(pricePaidInput.text),
            coinQuantity = Convert.ToDouble(quantityInput.text),
        };

        AddPurchase(openCoinSymbol, trade);
        dateInput.text = string.Empty;
        pricePaidInput.text = string.Empty;
        quantityInput.text = string.Empty;
        addTradePopup.Off();
    }

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

    public void AddPurchase(string coin, Trade newTrade)
    {
        StartCoroutine(RESTFULInterface.Instance.GetRequest("https://api.coingecko.com/api/v3/coins/" + coin + "/history?date=" + newTrade.Date, (response) =>
        {
            if (response.Contains("Error"))
            {
                Debug.LogError(response);
                return;
            }
            var currentPrice = JSON.Parse(response)["market_data"]["current_price"]["eur"];
            newTrade.CoinPrice = Convert.ToDouble(currentPrice);

            var purchaseObject = JSON.Parse(File.ReadAllText(tradesPath));
            if (purchaseObject[coin] == null)
                purchaseObject.Add(coin, JSON.Parse("[]"));

            var tradeFormatted = newTrade.FormatToJSON();
            purchaseObject[coin].AsArray.Add(tradeFormatted);

            File.WriteAllText(tradesPath, purchaseObject.ToString());

            DisplayAllPurchases(coin);
        }));

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
            purchaseTrack.AddDelete(() =>
            {
                RemovePurchase(openCoinSymbol, trade.Date);
            });
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

    public void RemovePurchase(string coin, string date)
    {
        var tradesFile = File.ReadAllText(tradesPath);

        var purchaseObject = JSON.Parse(tradesFile);
        if (purchaseObject[coin] == null)
            return;

        int indexer = 0;
        foreach (JSONNode node in purchaseObject[coin].AsArray)
        {
            if (node["date"].Value.Equals(date))
            {
                purchaseObject[coin].AsArray.Remove(indexer);
                File.WriteAllText(tradesPath, purchaseObject.ToString());
                break;
            }
            indexer++;
        }

        DisplayAllPurchases(coin);
    }

    private void DisplayTotalInvestment()
    {
        totalInvesement.text = "€" + GlobalData.Instance.TotalSpend.ToString("N2");

        //totalInvesement.text = "TI: " + total.ToString("N2");
    }
}
