using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinTrack : MonoBehaviour
{
    [SerializeField]
    private Text coinId, average, currentPrice;
    [SerializeField]
    private Button tradeButton, removeButton;

    private CanvasGroup tradesUI;

    public void UpdateView(Coin coin)
    {
        coinId.text = coin.ID.ToUpper();
        average.text = string.Format("AVG: €{0:N2}", coin.EuroAverage);
        currentPrice.text = string.Format("Current: €{0}", coin.CurrentPrice);

        tradeButton.onClick.AddListener(() =>
        {
            if (tradesUI == null)
                tradesUI = GameObject.FindGameObjectWithTag("Trades").GetComponent<CanvasGroup>();

            tradesUI.On();
        });
    }
}
