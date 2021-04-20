using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CoinTrack : MonoBehaviour
{
    [SerializeField]
    private Sprite defaultCoinIcon;
    [SerializeField]
    private Image icon;
    [SerializeField]
    private Text coinId, average, investment, currentPrice;
    [SerializeField]
    private Button tradeButton, refreshButton;
    [SerializeField]
    private Image background, updateFlash;

    private Color textColor;
    private TradePanel tradesUI;
    private CoinTracker trackerUI;

    private void Start()
    {
        tradeButton.onClick.AddListener(() =>
        {
            if (tradesUI == null)
                tradesUI = FindObjectOfType<TradePanel>();

            var coin = GlobalData.Instance.GetCoin(name);
            tradesUI.OpenTradesForCoin(coin.symbol);
        });

        refreshButton.onClick.AddListener(() =>
        {
            if (trackerUI == null)
                trackerUI = FindObjectOfType<CoinTracker>();

            trackerUI.Refresh(name);
        });
    }

    public void UpdateView(Coin coin, int days)
    {
        ApplyIcon(coin.Icon);
        coinId.color = average.color = currentPrice.color = investment.color = textColor;
        coinId.text = coin.ID.ToUpper();

        RefreshAVG(coin.MarketHistory, days);
        RefreshECA(coin.MarketHistory, days);
        currentPrice.text = "€" + coin.CurrentPrice.ToString();

        StartCoroutine(FlashTrack());
    }

    private void ApplyIcon(Texture2D texture)
    {
        if (texture == null)
        {
            icon.sprite = defaultCoinIcon;
            textColor = Color.black;
            return;
        }
        icon.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);

        float r = 0, g = 0, b = 0;
        var pixels = texture.GetPixels();
        var colorCount = pixels.Length;
        foreach (var p in pixels)
        {
            if (p.a < 1 || GetLuminence(p) > 0.95f)
            {
                colorCount--;
                continue;
            }
            r += p.r;
            g += p.g;
            b += p.b;
        }
        background.color = new Color(r /= colorCount, g /= colorCount, b /= colorCount);
        textColor = GetLuminence(background.color) < 0.45f ? Color.white : Color.black;
    }

    private float GetLuminence(Color color)
    {
        return 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
    }

    private IEnumerator FlashTrack()
    {
        updateFlash.color = Color.white;
        var a = 1f;
        while (updateFlash.color.a > 0)
        {
            updateFlash.color = new Color(1, 1, 1, a -= Time.deltaTime);
            yield return null;
        }
    }

    public void RefreshECA(JSONNode marketPrices, int days)
    {
        var startingPoint = marketPrices.Count - days;
        var totalCoins = 0.0;
        for (var i = startingPoint; i < marketPrices.Count; i++)
        {
            var priceOnInterval = Convert.ToDouble(marketPrices[i][1]);
            var tenEuroWorth = 10 / priceOnInterval;
            totalCoins += tenEuroWorth;
        }
        var eca = (10.0 * days) / totalCoins;
        string averageFormat = eca < 1 ? "N5" : "N2";
        investment.text = string.Format("ECA: €{0:" + averageFormat + "}", eca);

    }

    public void RefreshAVG(JSONNode marketPrices, int days)
    {
        var sum = 0.0;
        var startingPoint = marketPrices.Count - days;
        for (var i = startingPoint; i < marketPrices.Count; i++)
        {
            var dp = marketPrices[i][1];
            sum += Convert.ToDouble(dp);
        }

        var avg = sum / days;
        string averageFormat = avg < 1 ? "N5" : "N2";
        average.text = string.Format("AVG: €{0:" + averageFormat + "}", avg);
    }
}
