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
    private Button tradeButton;
    [SerializeField]
    private Image background;

    private Color textColor;
    private TradePanel tradesUI;

    private void Start()
    {
        tradeButton.onClick.AddListener(() =>
        {
            if (tradesUI == null)
                tradesUI = GameObject.FindGameObjectWithTag("Trades").GetComponent<TradePanel>();

            tradesUI.OpenTradesForCoin(name);
        });
    }

    public void UpdateView(Coin coin)
    {
        if (icon.sprite == null && !string.IsNullOrEmpty(coin.symbol))
        {
            StartCoroutine(RESTFULInterface.Instance.GetRequest(Path.Combine(Application.streamingAssetsPath, coin.symbol + ".png"), ApplyIcon));
        }
        coinId.color = average.color = currentPrice.color = investment.color = textColor;
        coinId.text = coin.ID.ToUpper();

        string averageFormat = coin.EuroAverage < 1 ? "N5" : "N2";
        average.text = string.Format("AVG: €{0:" + averageFormat + "}", coin.EuroAverage);
        investment.text = string.Format("ECA: €{0:" + averageFormat + "}", coin.InvestmentValue);
        currentPrice.text = string.Format("Current: €{0}", coin.CurrentPrice);
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
}
