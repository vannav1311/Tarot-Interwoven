using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverUIAndAppearInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI Components")]
    [SerializeField] private Image targetImage;
    [SerializeField] private List<Image> targetImages;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(249f / 255f, 242f / 255f, 163f / 255f);
    [SerializeField] private Color disabledColor = new Color(220f/255f, 215f/255f, 185f/255f);
    [SerializeField] private float scaleFactor = 1.1f;
    private Vector3 originalScale;

    [Header("Deck Info (Optional)")]
    [SerializeField] private TextMeshProUGUI textAppear;
    [SerializeField] private GameObject deckInfoPanel;

    private GameManager gameManager;

    IEnumerator Start()
    {
        yield return new WaitUntil(() =>
            GameManager.Instance != null && GameManager.Instance.GameReady);

        gameManager = GameManager.Instance;

        originalScale = transform.localScale;


        if (CompareTag("Deck"))
        {
            if (textAppear != null)
            {
                textAppear.gameObject.SetActive(false);
            }

            if (deckInfoPanel != null)
                deckInfoPanel.SetActive(false);
        }
        if (CompareTag("PlayerActionButton"))
        {
            if (targetImage != null)
                targetImage.color = disabledColor;
        }
        if (CompareTag("Timer"))
        {
            if (targetImage != null)
            {
                Color color = targetImage.color;
                color.a = 0f;
                targetImage.color = color;
                Color colorT = textAppear.color;
                colorT.a = 0f;
                textAppear.color = colorT;
            }
        }
    }

    public void Update()
    {
        if (gameManager != null && gameManager.Object != null && gameManager.GameReady)
        {
            if (CompareTag("Deck"))
                textAppear.text = gameManager.DeckCount.ToString();

            else if (CompareTag("StepInfo"))
            {
                if (gameManager.stage == Stage.Startgame)
                    textAppear.text = "Start game";
                else if (gameManager.stage == Stage.ChoosePath)
                    textAppear.text = "Choose path";
                else if (gameManager.stage == Stage.Abandon)
                    textAppear.text = "Abandon";
                else if (gameManager.stage == Stage.Spread)
                    textAppear.text = "Spread";
                else if (gameManager.stage == Stage.Decide)
                    textAppear.text = "Decide";
                else if (gameManager.stage == Stage.Renew)
                    textAppear.text = "Renew";
                else if (gameManager.stage == Stage.Endgame)
                    textAppear.text = "Endgame";
            }

            else if (CompareTag("PlayerTurnIndicator") &&
                textAppear.text != gameManager.currentPlayer.ToString())
            {
                textAppear.text = gameManager.currentPlayer.ToString();
                StartCoroutine(PlayerTurnOnChange(gameManager.currentPlayer));
            }

            else if (gameManager.PlayerCount > 0)
            {
                if (CompareTag("Transcript1"))
                    textAppear.text = gameManager.playerPoints[0].ToString();
                else if (CompareTag("Transcript2"))
                    textAppear.text = gameManager.playerPoints[1].ToString();
                else if (CompareTag("Transcript3"))
                    textAppear.text = gameManager.playerPoints[2].ToString();
                else if (CompareTag("Transcript4"))
                    textAppear.text = gameManager.playerPoints[3].ToString();
            }
            
            if (CompareTag("PlayerActionButton") && gameManager.thisPlayer != null)
            {
                if (gameManager.stage == Stage.Decide && gameManager.currentPlayer == gameManager.thisPlayer.SeatIndex + 1)
                {
                    targetImage.color = normalColor;
                }
                else
                    targetImage.color = disabledColor;
            }
            else if (CompareTag("Timer") && gameManager.thisPlayer != null && gameManager.thisPlayer.phaseCountdown)
            {
                if (targetImage.color.a == 0f)
                {
                    Color color = targetImage.color;
                    color.a = 256f;
                    targetImage.color = color;
                    Color colorT = textAppear.color;
                    colorT.a = 256f;
                    textAppear.color = colorT;
                }
                
                textAppear.text = gameManager.GetTimerCountdownTick().ToString();
            }
            else if (CompareTag("Timer") && gameManager.thisPlayer != null && !gameManager.thisPlayer.phaseCountdown)
            {
                if (targetImage.color.a == 256f)
                {
                    Color color = targetImage.color;
                    color.a = 0f;
                    targetImage.color = color;
                    Color colorT = textAppear.color;
                    colorT.a = 0f;
                    textAppear.color = colorT;
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (gameManager == null || gameManager.Object == null || !gameManager.GameReady)
            return;

        if (CompareTag("Deck"))
        {
            foreach (var i in targetImages)
            {
                if (i != null)
                    i.color = highlightColor;
            }

            if (textAppear != null && gameManager != null)
            {
                textAppear.gameObject.SetActive(true);
            }
        }

        if (CompareTag("PlayerActionButton") && gameManager.thisPlayer != null && gameManager.thisPlayer != null &&
        gameManager.stage == Stage.Decide && gameManager.currentPlayer == gameManager.thisPlayer.SeatIndex + 1)
        {
            transform.localScale = originalScale * scaleFactor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (gameManager == null || gameManager.Object == null || !gameManager.GameReady)
            return;

        if (CompareTag("Deck"))
        {
            foreach (Image i in targetImages)
            {
                i.color = normalColor;
            }

            if (textAppear != null && gameManager != null)
            {
                textAppear.gameObject.SetActive(false);
            }
        }

        if (CompareTag("PlayerActionButton") && gameManager.thisPlayer != null &&
        gameManager.stage == Stage.Decide && gameManager.currentPlayer == gameManager.thisPlayer.SeatIndex + 1)
        {
            transform.localScale = originalScale;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CompareTag("PlayerActionButton") && gameManager.thisPlayer != null
        && gameManager.stage == Stage.Decide && 
        gameManager.currentPlayer == gameManager.thisPlayer.SeatIndex + 1)
        {
            gameManager.thisPlayer.Rpc_GiveDecision(name);
        }
    }

    private IEnumerator PlayerTurnOnChange(int currentPlayer)
    {
        targetImage.gameObject.SetActive(true);

        if (currentPlayer == 0)
            targetImage.gameObject.SetActive(false);
        else
        {
            RectTransform rt = targetImage.GetComponent<RectTransform>();
            int realSeat = currentPlayer - 2 - gameManager.thisPlayer.SeatIndex;
            float targetAngle = realSeat * 90f;
            float startAngle = rt.localEulerAngles.z;

            float t = 0f;
            float dur = 0.35f;

            while (t < 1f)
            {
                t += Time.deltaTime / dur;
                float eased = EaseOut(t);

                float angle =
                    Mathf.LerpAngle(startAngle, targetAngle, eased);

                rt.localEulerAngles = new Vector3(0f, 0f, angle);

                yield return null;
            }
        }
    }

    float EaseOut(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3);
    }
}
