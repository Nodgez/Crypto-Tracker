using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Networking;

public class CoinTracker : MonoBehaviour
{
    [SerializeField]
    private Image background;

    private const string TRACKEDCOINSKEY = "addedCoinsForTracking";
    [SerializeField]
    private CoinTrack coinTrackPrefab;
    [SerializeField]
    private ScrollRect trackedCoinsContainer;
    
    private JSONNode allCoinGeckoCoins;
    private JSONNode localCoinsToTrack = JSON.Parse("{}");

    private Dictionary<string, Coin> trackedCoins = new Dictionary<string, Coin>();
    private Dictionary<string, CoinTrack> coinTracks = new Dictionary<string, CoinTrack>();
    private string coinPath;
    // Start is called before the first frame update
    void Start()
    {
        coinPath = Path.Combine(Application.persistentDataPath, "coins.json");
        StartCoroutine(InitializeTracker());        
    }

    IEnumerator InitializeTracker()
    {
        yield return StartCoroutine(RESTFULInterface.Instance.GetRequest("https://api.coingecko.com/api/v3/coins/list", (response) =>
        {
            allCoinGeckoCoins = JSON.Parse(response);
        }));

        yield return StartCoroutine(RESTFULInterface.Instance.GetRequest("https://picsum.photos/1920/1080?grayscale&blur=5", RetrievedBackground));

        var coinsFile = File.ReadAllText(coinPath);
        localCoinsToTrack = JSON.Parse(coinsFile);
        foreach (JSONNode n in localCoinsToTrack.AsArray)
            CreateTrackForCoin(n);

        yield return StartCoroutine(RefreshTrackedCoins());
    }

    void UpdateEuroCostAverage(string coin, int days, string frequency)
    {
        var url = "https://api.coingecko.com/api/v3/coins/" + coin + "/market_chart?vs_currency=eur&days=" + days + "&interval=" + frequency;
        StartCoroutine(RESTFULInterface.Instance.GetRequest(url,(responseData) => {

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

    void UpdateInvestmentAverage(string coin, int days, string frequency)
    {
        var url = "https://api.coingecko.com/api/v3/coins/" + coin + "/market_chart?vs_currency=eur&days=" + days + "&interval=" + frequency;
        StartCoroutine(RESTFULInterface.Instance.GetRequest(url, (responseData) => {

            if (responseData.Contains("Error"))
            {
                Debug.LogError(responseData);
                return;
            }

            var jsonData = JSON.Parse(responseData)["prices"];
            var count = jsonData.Count;
            var totalCoins = 0.0;
            for (var i = 0; i < count; i++)
            {
                var priceOnInterval = Convert.ToDouble(jsonData[i][1]);
                var tenEuroWorth = 10 / priceOnInterval;
                totalCoins += tenEuroWorth;
            }
            var finalIntervalPrice = Convert.ToDouble(jsonData[count - 1][1]);
            var coinData = trackedCoins[coin];
            coinData.InvestmentValue = (10 * count) / totalCoins;
            trackedCoins[coin] = coinData;
        }));
    }

    void UpdateCurrentPrice(string coin)
    {
        var url = "https://api.coingecko.com/api/v3/coins/markets?vs_currency=eur&ids=" + coin;
        StartCoroutine(RESTFULInterface.Instance.GetRequest(url, (responseData) =>
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

        foreach (JSONNode node in allCoinGeckoCoins.AsArray)
        {
            if (coin == node["id"].Value)
            {
                var c = trackedCoins[coin];
                c.symbol = node["symbol"];
                trackedCoins[coin] = c;

                break;
            }
        }

        var coinTrack = Instantiate(coinTrackPrefab, trackedCoinsContainer.content);
        coinTrack.name = coin;
        coinTracks.Add(coin, coinTrack);
        UpdateEuroCostAverage(coin, 30, "daily");
        UpdateInvestmentAverage(coin, 30, "daily");
        UpdateCurrentPrice(coin);
        coinTrack.UpdateView(trackedCoins[coin]);

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
                yield return new WaitForSeconds(0.15f);

                UpdateInvestmentAverage(coin.ID, 30, "daily");
                yield return new WaitForSeconds(0.15f);

                UpdateCurrentPrice(coin.ID);
                yield return new WaitForSeconds(0.15f);


                if (coinTracks.ContainsKey(coin.ID))
                    coinTracks[coin.ID].UpdateView(coin);
                else
                    Debug.LogError("Coin key not tracked: " + coin.ID);
                yield return new WaitForSeconds(0.1f * coinTracks.Count);
            }

            yield return null;
        }
    }

    private void RetrievedBackground(Texture2D texture)
    {
        if (texture == null)
            return;

        background.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
    }

#if UNITY_EDITOR
    [MenuItem("Data Helper/Clear Player Prefs")]
    static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }
#endif

}

public struct Coin
{
    public string ID;
    public string symbol;
    public string Name;
    public double EuroAverage;
    public double InvestmentValue;
    public double CurrentPrice;
}

public struct Trade
{
    public string Date;
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
}
