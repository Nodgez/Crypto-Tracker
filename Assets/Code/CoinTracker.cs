using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CoinTracker : MonoBehaviour
{
    private const string TRACKEDCOINSKEY = "addedCoinsForTracking";
    private const string ALLCOINSKEY = "allCoins";
    private const string PURCHASEHISTORY = "purchaseHistory";
    [SerializeField]
    private CoinTrack coinTrackPrefab;
    [SerializeField]
    private PurchaseHistoryTrack purchaseHistoryPrefab;
    [SerializeField]
    private ScrollRect trackedCoinsContainer, purchaseHistoryContainer;
    
    private JSONNode allCoinGeckoCoins;
    private JSONNode localCoinsToTrack = JSON.Parse("{}");

    private Dictionary<string, Coin> trackedCoins = new Dictionary<string, Coin>();
    private Dictionary<string, CoinTrack> coinTracks = new Dictionary<string, CoinTrack>();

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(GetRequest("https://api.coingecko.com/api/v3/coins/list", (response) =>
         {
             allCoinGeckoCoins = JSON.Parse(response);
         }));

        localCoinsToTrack = JSON.Parse(PlayerPrefs.GetString(TRACKEDCOINSKEY, "[]"));
        foreach (JSONNode n in localCoinsToTrack.AsArray)
        {
            CreateTrackForCoin(n);
        }

        StartCoroutine(RefreshTrackedCoins());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddPurchase("bitcoin", new Trade() {
                Date = DateTime.Now.ToShortDateString(),
                CoinPrice= 0,
                PricePaid = 0,
                CoinValue = 0
            });
        }
    }

    IEnumerator GetRequest(string uri, Action<string> responseCallback)
    {
        Debug.Log("sending request to: " + uri);
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    responseCallback("Error :" + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    responseCallback("Error :" + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    responseCallback(webRequest.downloadHandler.text);
                    break;
            }
        }
    }

    void UpdateEuroCostAverage(string coin, int days, string frequency)
    {
        var url = "https://api.coingecko.com/api/v3/coins/" + coin + "/market_chart?vs_currency=eur&days=" + days + "&interval=" + frequency;
        StartCoroutine(GetRequest(url,(responseData) => {

            if (responseData.Contains("Error"))
            {
                Debug.LogError(responseData);
                return;
            }

            var jsonData = JSON.Parse(responseData)["prices"];
            var count = jsonData.Count;
            var sum = 0.0;
            for (var i = 0; i < count; i++)
            {
                var dp = jsonData[i][1];
                sum += Convert.ToDouble(dp);
            }

            var coinData = trackedCoins[coin];
            coinData.EuroAverage = sum / count;
            trackedCoins[coin] = coinData;
        }));

    }

    void UpdateCurrentPrice(string coin)
    {
        var url = "https://api.coingecko.com/api/v3/coins/markets?vs_currency=eur&ids=" + coin;
        StartCoroutine(GetRequest(url, (responseData) =>
        {
            if (responseData.Contains("Error"))
            {
                Debug.LogError(responseData);
                return;
            }

            var jsonData = JSON.Parse(responseData)[0]["current_price"];
            var coinData = trackedCoins[coin];
            coinData.CurrentPrice = Convert.ToDouble(jsonData);
            trackedCoins[coin] = coinData;
        }));

        }

    public void StoreCoinToTrack(Text textComponent)
    {
        StoreCoinToTrack(textComponent.text);
    }

    private void StoreCoinToTrack(string coin)
    {
        localCoinsToTrack = JSON.Parse(PlayerPrefs.GetString(TRACKEDCOINSKEY, "[]"));
        
        foreach(JSONNode c in localCoinsToTrack.AsArray)
        {
            if (coin.Equals(c))
            {
                Debug.LogWarning(coin + " Already Tracked");
                return;
            }
        }

        var isValidCoin = false;
        foreach (JSONNode n in allCoinGeckoCoins.AsArray)
        {
            if (coin == n["id"].Value)
            {
                isValidCoin = true;
                break;
            }
        }
        if (!isValidCoin)
        {
            Debug.LogWarning(coin + " is not a valid coin");
            return;
        }

        localCoinsToTrack.Add(coin);
        PlayerPrefs.SetString(TRACKEDCOINSKEY, localCoinsToTrack.ToString());
        Debug.Log(localCoinsToTrack.ToString());
        CreateTrackForCoin(coin);
    }

    private void CreateTrackForCoin(string coin)
    {
        trackedCoins[coin] = new Coin()
        {
            ID = coin
        };

        var coinTrack = Instantiate(coinTrackPrefab, trackedCoinsContainer.content);
        coinTracks.Add(coin, coinTrack);
        coinTrack.UpdateView(trackedCoins[coin]);
        UpdateEuroCostAverage(coin, 30, "daily");
        UpdateCurrentPrice(coin);
        coinTrack.UpdateView(trackedCoins[coin]);

    }

    private void RemoveCoinToTrack(string coin)
    {
        localCoinsToTrack = JSON.Parse(PlayerPrefs.GetString(TRACKEDCOINSKEY, "[]"));
        localCoinsToTrack.AsArray.Remove(coin);
        PlayerPrefs.SetString(TRACKEDCOINSKEY, localCoinsToTrack.ToString());
        Debug.Log(localCoinsToTrack.ToString());
    }

    IEnumerator RefreshTrackedCoins()
    {
        while (true)
        {
            var clonedCoins = new Dictionary<string, Coin>(trackedCoins);
            foreach (var coinKey in clonedCoins.Keys)
            {
                var coin = clonedCoins[coinKey];
                UpdateEuroCostAverage(coin.ID, 30, "daily");
                UpdateCurrentPrice(coin.ID);

                if (coinTracks.ContainsKey(coin.ID))
                    coinTracks[coin.ID].UpdateView(coin);
                else
                    Debug.LogError("Coin key not tracked: " + coin.ID);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    [MenuItem("Data Helper/Clear Player Prefs")]
    static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }

    [MenuItem("Data Helper/Clear Purchase History")]
    static void ClearPurchaseHistory()
    {
        PlayerPrefs.DeleteKey(PURCHASEHISTORY);
    }

    public void AddPurchase(string coin, Trade trade)
    {
        var purchaseObject = JSON.Parse(PlayerPrefs.GetString(PURCHASEHISTORY, "{ }"));
        if (purchaseObject["bitcoin"] == null)
            purchaseObject.Add("bitcoin", JSON.Parse("[]"));

        var tradeFormatted = trade.FormatToJSON();
        purchaseObject["bitcoin"].AsArray.Add(tradeFormatted);

        PlayerPrefs.SetString(PURCHASEHISTORY, purchaseObject.ToString());
        Debug.Log(purchaseObject);
        //var purchaseTrack = Instantiate(purchaseHistoryPrefab, purchaseHistoryContainer.content);
    }
}

public struct Coin
{
    public string ID;
    public string Name;
    public double EuroAverage;
    public double CurrentPrice;
}

public struct Trade
{
    public string Date;
    public double PricePaid;
    public double CoinValue;
    public double CoinPrice;

    public string FormatToJSON()
    {
        return string.Format("{{date: {0}, pricePaid: {1}, coinValue: {2}, coinPrice: {3}}}", Date, PricePaid, CoinValue, CoinPrice);
    }
}
