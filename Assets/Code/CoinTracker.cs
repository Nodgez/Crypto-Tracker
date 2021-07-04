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
    private CanvasGroup canvasGroup;
    [SerializeField]
    private CoinTrack coinTrackPrefab;
    [SerializeField]
    private ScrollRect trackedCoinsContainer;
    [SerializeField]
    private Text totalInvesement, percentageDifference;
    [SerializeField]
    private Image refreshingIcon;

    static public JSONNode localCoinsToTrack = JSON.Parse("{}");

    private Dictionary<string, CoinTrack> coinTracks = new Dictionary<string, CoinTrack>();
    private string coinPath;
    private int retroSpect = 30;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
        while (!GlobalData.Instance.Initialized)
            yield return null;

        coinPath = Path.Combine(Application.persistentDataPath, "coins.json");
        retrospectDropdown.onValueChanged.AddListener(OnRetrospectChange);
        refreshingIcon.gameObject.RotateAdd(Vector3.forward * -360, 1, 0, EaseType.linear, LoopType.loop);

        var coinsFile = File.ReadAllText(coinPath);
        localCoinsToTrack = JSON.Parse(coinsFile);
        foreach (JSONNode n in localCoinsToTrack.AsArray)
            CreateTrackForCoin(n);
        Refresh();
    }
    public void Refresh()
    {
        SetInteractable(false);
        GlobalData.Instance.RefrehTrackedCoinData(RefreshComplete);
        refreshingIcon.enabled = true;
    }

    public void Refresh(string coinId)
    {
        SetInteractable(false);
        GlobalData.Instance.RefrehTrackedCoinData(coinId, RefreshComplete);
        refreshingIcon.enabled = true;
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

        var isValidCoin = GlobalData.Instance.IsValidCoin(coin);

        if (!isValidCoin)
        {
            Debug.LogWarning(coin + " is not a valid coin");
            return;
        }

        localCoinsToTrack.Add(coin);
        File.WriteAllText(coinPath, localCoinsToTrack.ToString());
        CreateTrackForCoin(coin);
    }

    private void CreateTrackForCoin(string symbol)
    {
        var coinTrack = Instantiate(coinTrackPrefab, trackedCoinsContainer.content);
        coinTrack.name = symbol;
        coinTracks.Add(symbol, coinTrack);
    }

    private void RefreshComplete()
    {
        foreach (var key in coinTracks.Keys)
        {
            var track = coinTracks[key];
            var coin = GlobalData.Instance.GetCoin(track.name);
            track.UpdateView(coin, retroSpect);
        }

        totalInvesement.text = "€" + GlobalData.Instance.TotalSpend.ToString("N2");
        percentageDifference.text = GlobalData.Instance.GetPercentageDifference().ToString("N2");
        SetInteractable(true);
        refreshingIcon.enabled = false;
    }

    private void RefreshComplete(string coinID)
    {
        var track = coinTracks[coinID];
        var coin = GlobalData.Instance.GetCoin(track.name);
        track.UpdateView(coin, retroSpect);       
        SetInteractable(true);
        refreshingIcon.enabled = false;
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
    private void SetInteractable(bool isInteractable)
    {
        canvasGroup.interactable = refreshButton.interactable = retrospectDropdown.interactable = isInteractable;
    }
}
