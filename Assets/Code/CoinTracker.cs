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
    private Dropdown retrospectDropdown;
    [SerializeField]
    private Button refreshButton;

    [SerializeField]
    private CoinTrack coinTrackPrefab;
    [SerializeField]
    private ScrollRect trackedCoinsContainer;
    
    private JSONNode allCoinGeckoCoins;
    private JSONNode localCoinsToTrack = JSON.Parse("{}");

    private Dictionary<string, Coin> trackedCoins = new Dictionary<string, Coin>();
    private Dictionary<string, CoinTrack> coinTracks = new Dictionary<string, CoinTrack>();
    private string coinPath;
    private int retroSpect = 30;
    // Start is called before the first frame update
    void Start()
    {
        coinPath = Path.Combine(Application.persistentDataPath, "coins.json");
        retrospectDropdown.onValueChanged.AddListener(OnRetrospectChange);

        StartCoroutine(InitializeTracker());        
    }

    IEnumerator InitializeTracker()
    {
        yield return StartCoroutine(RESTFULInterface.Instance.GetRequest("https://api.coingecko.com/api/v3/coins/list", (response) =>
        {
            if (response.Contains("Error"))
            {
                return;
            }
            allCoinGeckoCoins = JSON.Parse(response);
        }));

        var coinsFile = File.ReadAllText(coinPath);
        localCoinsToTrack = JSON.Parse(coinsFile);
        foreach (JSONNode n in localCoinsToTrack.AsArray)
            CreateTrackForCoin(n);
        Refresh();
    }

    public void Refresh()
    {
        StartCoroutine(RefreshTrackedCoins());
    }

    Coroutine UpdateEuroCostAverage(string coin, int days, string frequency)
    {
        var url = "https://api.coingecko.com/api/v3/coins/" + coin + "/market_chart?vs_currency=eur&days=" + days + "&interval=" + frequency;
        return StartCoroutine(RESTFULInterface.Instance.GetRequest(url,(responseData) => {

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

    Coroutine UpdateInvestmentAverage(string coin, int days, string frequency)
    {
        var url = "https://api.coingecko.com/api/v3/coins/" + coin + "/market_chart?vs_currency=eur&days=" + days + "&interval=" + frequency;
        return StartCoroutine(RESTFULInterface.Instance.GetRequest(url, (responseData) => {

            if (responseData.Contains("Error"))
            {
                Debug.LogError(responseData);
                return;
            }

            var jsonData = JSON.Parse(responseData)["prices"];
            Debug.Log(jsonData);
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
            coinData.InvestmentValue = (10.0 * count) / totalCoins;
            trackedCoins[coin] = coinData;
        }));
    }

    Coroutine UpdateCurrentPrice(string coin)
    {
        var url = "https://api.coingecko.com/api/v3/coins/markets?vs_currency=eur&ids=" + coin;
        return StartCoroutine(RESTFULInterface.Instance.GetRequest(url, (responseData) =>
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
        var coinsFile = File.ReadAllText(coinPath);
        localCoinsToTrack = JSON.Parse(coinsFile);
        foreach (JSONNode c in localCoinsToTrack.AsArray)
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
        File.WriteAllText(coinPath, localCoinsToTrack.ToString());
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
        UpdateEuroCostAverage(coin, retroSpect, "daily");
        UpdateInvestmentAverage(coin, retroSpect, "daily");
        UpdateCurrentPrice(coin);
        coinTrack.UpdateView(trackedCoins[coin]);

    }

    IEnumerator RefreshTrackedCoins()
    {
        retrospectDropdown.interactable = refreshButton.interactable = false;
        
        var clonedCoins = new Dictionary<string, Coin>(trackedCoins);
        foreach (var coinKey in clonedCoins.Keys)
        {
            var coin = clonedCoins[coinKey];
            yield return UpdateEuroCostAverage(coin.ID, retroSpect, "daily");
            yield return UpdateInvestmentAverage(coin.ID, retroSpect, "daily");
            yield return UpdateCurrentPrice(coin.ID);
            if (coinTracks.ContainsKey(coin.ID))
                coinTracks[coin.ID].UpdateView(trackedCoins[coin.ID]);
            else
                Debug.LogError("Coin key not tracked: " + coin.ID);
            yield return new WaitForSeconds(1.1f);
        }
        retrospectDropdown.interactable = refreshButton.interactable = true;
    }

    public void OnRetrospectChange(int value)
    {
        var selectedOption = retrospectDropdown.options[value];
        retroSpect = int.Parse(selectedOption.text.Substring(0,selectedOption.text.Length - 4));
        Refresh();
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
