using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


/// <summary>
/// the purpose of this class is to read data from the Coin Gecko api
/// and to read and store data on the local machine
/// </summary>
public class GlobalData : MonoBehaviour
{
    private enum TransactionKind
    {
        crypto_withdrawal,
        viban_purchase, 
        referral_card_cashback,
        card_top_up,
        dust_conversion_credited,
        dust_conversion_debited,
        crypto_exchange,
        card_cashback_reverted,
        crypto_earn_interest_paid,
        crypto_earn_program_created,
        crypto_earn_program_withdrawn,
        reimbursement,
        crypto_viban_exchange,
        supercharger_deposit,
        supercharger_withdrawal,
        exchange_to_crypto_transfer,
        lockup_lock
    }
    public bool Initialized { get; private set; }

    static public JSONNode localCoinsToTrack = JSON.Parse("{}");
    private JSONNode allCoinGeckoCoins;

    private string coinPath;
    private Dictionary<string, List<Trade>> tradeMap = new Dictionary<string, List<Trade>>();
    private Dictionary<string, Coin> trackedCoins = new Dictionary<string, Coin>();

    private Dictionary<string, double> CurrencyTotals = new Dictionary<string, double>();

    static private GlobalData s_instance;
    static public GlobalData Instance
    {
        get { return s_instance; }
    }
    public double TotalSpend
    {
        get
        {
            return CurrencyTotals["EUR"];
        }
    }
    void Awake()
    {
        s_instance = this;
        Initialized = false;
        coinPath = Path.Combine(Application.persistentDataPath, "coins.json");
        var coinsFile = System.IO.File.ReadAllText(coinPath);
        localCoinsToTrack = JSON.Parse(coinsFile);

        LoadSpreadSheet();
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        yield return StartCoroutine(RESTFULInterface.Instance.GetRequest("https://api.coingecko.com/api/v3/coins/list", (response) =>
         {
             if (response.Contains("Error"))
             {
                 return;
             }
             allCoinGeckoCoins = JSON.Parse(response);
             Debug.Log(allCoinGeckoCoins);
         }));

        foreach (JSONNode idNode in localCoinsToTrack.AsArray)
        {
            var coinID = idNode.Value;
            trackedCoins[coinID] = new Coin()
            {
                ID = coinID
            };

            foreach (JSONNode node in allCoinGeckoCoins.AsArray)
            {
                if (coinID == node["id"].Value)
                {
                    var c = trackedCoins[coinID];
                    c.symbol = node["symbol"].Value.ToUpper();
                    trackedCoins[coinID] = c;

                    break;
                }
            }

            yield return SetIcon(coinID);
        }

        Initialized = true;
    }

    private void LoadSpreadSheet()
    {
        var strReader = new StreamReader(Path.Combine(Application.streamingAssetsPath, "crypto_com_data.csv"));
        var endOfFile = false;

        var dataString = strReader.ReadLine();//read the 1st line to discard it

        while (!endOfFile)
        {
            dataString = strReader.ReadLine();

            if (dataString == null)
            {
                endOfFile = true;
                break;
            }

            Debug.Log(dataString);

            var splitData = dataString.Split(',');

            var transactionType = splitData[1];
            var transactionKind = Enum.Parse(typeof(TransactionKind), splitData[9]);
            //Debug.Log("Tranaction kind of this line: " + transactionKind);

            switch (transactionKind)
            {
                case TransactionKind.viban_purchase:
                    HandleVibanPurchase(splitData);
                    break;
                case TransactionKind.referral_card_cashback:
                case TransactionKind.reimbursement:
                case TransactionKind.crypto_earn_interest_paid:
                case TransactionKind.exchange_to_crypto_transfer:
                    HandlePassiveSources(splitData);
                    break;
                
            }
        }
    }

    private void HandlePassiveSources(string[] incomeData)
    {
        var fromCurrency = incomeData[2];
        var fromAmount = double.Parse(incomeData[3]);

        if (!CurrencyTotals.ContainsKey(fromCurrency))
            CurrencyTotals.Add(fromCurrency, fromAmount);
        else
            CurrencyTotals[fromCurrency] += fromAmount;

        var trade = new Trade()
        {
            Symbol = incomeData[2],
            Date = incomeData[0],
            fromCurrency = string.Empty,
            PricePaid = 0,
            CoinPrice = fromAmount / double.Parse(incomeData[7]),
            coinQuantity = double.Parse(incomeData[3])
        };

        if (!tradeMap.ContainsKey(trade.Symbol))
            tradeMap.Add(trade.Symbol, new List<Trade>());

        tradeMap[trade.Symbol].Add(trade);
    }

    private void HandleVibanPurchase(string[] vibanPurchaseData)
    {
        var fromCurrency = vibanPurchaseData[2].Trim();
        var toCurrency = vibanPurchaseData[4].Trim();
        var fromAmount = fromCurrency == "EUR" ? -1 * double.Parse(vibanPurchaseData[3]) : double.Parse(vibanPurchaseData[3]);
        var toAmount = double.Parse(vibanPurchaseData[5]);

        if (!CurrencyTotals.ContainsKey(fromCurrency))
            CurrencyTotals.Add(fromCurrency, fromAmount);
        else
            CurrencyTotals[fromCurrency] += fromAmount;

        if (!CurrencyTotals.ContainsKey(toCurrency))
            CurrencyTotals.Add(toCurrency, toAmount);
        else
            CurrencyTotals[toCurrency] += toAmount;

        var trade = new Trade()
        {
            Symbol = toCurrency,
            Date = vibanPurchaseData[0],
            fromCurrency = fromCurrency,
            PricePaid = double.Parse(vibanPurchaseData[7]),
            CoinPrice = fromAmount / toAmount,
            coinQuantity = toAmount
        };

        if (!tradeMap.ContainsKey(trade.Symbol))
            tradeMap.Add(trade.Symbol, new List<Trade>());

        tradeMap[trade.Symbol].Add(trade);
    }

    public double GetCurrencyTotal(string symbol)
    {
        if (!CurrencyTotals.ContainsKey(symbol))
            return 0;
        return CurrencyTotals[symbol];
    }

    public double GetPercentageDifference()
    {
        double totalWorth = 0;
        foreach (var c in trackedCoins.Keys)
        {
            var coin = trackedCoins[c];
            totalWorth += coin.CurrentPrice * GetCurrencyTotal(coin.symbol);
        }

        return totalWorth / TotalSpend;
    }

    public List<Trade> GetCoinTradeHistory(string symbol)
    {
        if (tradeMap.ContainsKey(symbol))
            return tradeMap[symbol];
        return null;
    }

    public Coin GetCoin(string coinID)
    {
        return trackedCoins[coinID];
    }

    Coroutine LoadMarketPrices(string coinID)
    {
        var url = "https://api.coingecko.com/api/v3/coins/" + coinID + "/market_chart?vs_currency=eur&days=90&interval=daily";
        return StartCoroutine(RESTFULInterface.Instance.GetRequest(url,(responseData) => {

            if (responseData.Contains("Error"))
            {
                Debug.LogError(responseData);
                return;
            }

            if (trackedCoins.ContainsKey(coinID))
                trackedCoins[coinID].MarketHistory = JSON.Parse(responseData)["prices"];
        }));
    }

    Coroutine UpdateCurrentPrice(string coinID)
    {
        var url = "https://api.coingecko.com/api/v3/coins/markets?vs_currency=eur&ids=" + coinID;
        return StartCoroutine(RESTFULInterface.Instance.GetRequest(url, (responseData) =>
        {
            if (responseData.Contains("Error"))
            {
                Debug.LogError(responseData);
                return;
            }

            var jsonData = JSON.Parse(responseData)[0]["current_price"];
            if (trackedCoins.ContainsKey(coinID))
                trackedCoins[coinID].CurrentPrice = Convert.ToDouble(jsonData.Value);
        }));
    }

    Coroutine SetIcon(string coinID)
    {
        var coin = trackedCoins[coinID];
        return StartCoroutine(RESTFULInterface.Instance.GetRequest(Path.Combine(Application.streamingAssetsPath, coin.symbol + ".png"), (icon) =>
        {
            coin.Icon = icon;
        }));
    }

    public JSONNode GetMarketPrices(string coinID)
    {
        return trackedCoins[coinID].MarketHistory;
    }

    public bool IsValidCoin(string coinID)
    {
        foreach (JSONNode n in allCoinGeckoCoins.AsArray)
        {
            if (coinID == n["id"].Value)
                return true;
        }

        return false;
    }

    public void RefrehTrackedCoinData(Action onRefreshComplete)
    {
        StartCoroutine(CO_RefreshTrackedCoins(onRefreshComplete));
    }

    public void RefrehTrackedCoinData(string coinID, Action<string> onRefreshComplete)
    {
        StartCoroutine(CO_RefreshTrackedCoins(coinID, onRefreshComplete));
    }

    IEnumerator CO_RefreshTrackedCoins(Action onRefreshComplete)
    {
        foreach (JSONNode node in localCoinsToTrack.AsArray)
        {
            var c = node.Value;
            yield return LoadMarketPrices(c);
            yield return UpdateCurrentPrice(c);
        }

        onRefreshComplete?.Invoke();
    }

    IEnumerator CO_RefreshTrackedCoins(string coinID, Action<string> onRefreshComplete)
    {
        yield return LoadMarketPrices(coinID);
        yield return UpdateCurrentPrice(coinID);

        onRefreshComplete?.Invoke(coinID);
    }
}

public class Coin
{
    public string ID;
    public string symbol;
    public string Name;
    public JSONNode MarketHistory;
    public Texture2D Icon;
    public double CurrentPrice;
}

public class Trade
{
    public string Symbol;
    public string Date;
    public string fromCurrency;
    public double PricePaid;
    public double coinQuantity;
    public double CoinPrice;

    public JSONNode FormatToJSON()
    {
        var tradeJSON = JSON.Parse("{}");
        tradeJSON.Add("date", Date);
        tradeJSON.Add("pricePaid", PricePaid.ToString());
        tradeJSON.Add("coinQuantity", coinQuantity.ToString());
        tradeJSON.Add("coinPrice", CoinPrice.ToString());
        return tradeJSON;
    }

    public override string ToString()
    {
        var str = string.Format("id: {0},date: {1}, pricePaid: {2},quantity: {3}, price: {4} ", Symbol, Date, PricePaid.ToString(), coinQuantity.ToString(), CoinPrice.ToString());
        return str;
    }
}
