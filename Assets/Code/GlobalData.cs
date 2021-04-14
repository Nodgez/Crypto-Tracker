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

    static public JSONNode localCoinsToTrack = JSON.Parse("{}");
    private JSONNode allCoinGeckoCoins;
    private string coinPath;
    private Dictionary<string, List<Trade>> tradeMap = new Dictionary<string, List<Trade>>();

    static private GlobalData s_instance;
    static public GlobalData Instance
    {
        get { return s_instance; }
    }
    public double TotalSpend { get; private set; }
    void Start()
    {
        s_instance = this;
        coinPath = Path.Combine(Application.persistentDataPath, "coins.json");
        var coinsFile = File.ReadAllText(coinPath);
        localCoinsToTrack = JSON.Parse(coinsFile);

        LoadSpreadSheet();

        StartCoroutine(RESTFULInterface.Instance.GetRequest("https://api.coingecko.com/api/v3/coins/list", (response) =>
        {
            if (response.Contains("Error"))
            {
                return;
            }
            allCoinGeckoCoins = JSON.Parse(response);
            Debug.Log(allCoinGeckoCoins);
        }));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            LoadSpreadSheet();
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

            if (transactionType.Contains("Earn"))
            {

            }
            else if (transactionType.Contains("->"))
            {
                var pricePaid = Convert.ToDouble(splitData[7]);
                var coinQuantity = Convert.ToDouble(splitData[5]);
                var trade = new Trade()
                {
                    Symbol = splitData[4].ToLower(),
                    Date = splitData[0],
                    PricePaid = pricePaid,
                    CoinPrice = pricePaid / coinQuantity,
                    coinQuantity = coinQuantity
                };
                TotalSpend += pricePaid;
                if (!tradeMap.ContainsKey(trade.Symbol))
                    tradeMap.Add(trade.Symbol, new List<Trade>());

                tradeMap[trade.Symbol].Add(trade);
                Debug.Log(trade.ToString());

            }
            else if (transactionType.Contains("Card"))
            {

            }
            else if (transactionType.Contains("Supercharger"))
            {

            }
        }
    }


    public List<Trade> GetCoinTradeHistory(string symbol)
    {
        if (tradeMap.ContainsKey(symbol))
            return tradeMap[symbol];
        return null;
    }
}
