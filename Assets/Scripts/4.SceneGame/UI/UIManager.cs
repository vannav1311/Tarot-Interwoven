using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum InfoKind
{
    Not,
    Warning,
    Success
}
public class UIManager : MonoBehaviour {
    public static UIManager Instance;
    //for all ui prefab create to be child
    public Transform UIRoot;
    public Transform[] handAreas; // 0=bottom, 1=right, 2=top, 3=left
    public Transform discardArea;
    public Transform spreadArea;

    public Sprite discardAreaImage;
    public Transform discardPanel;

    public Transform deck;
    public Transform compass;
    public Transform indicatePhase;

    //Choose combo in call step
    public GameObject chooseComboPanel;
    public Button OKButton;
    public Button CancelButton;

    //Label for inform
    public GameObject InfoLabel;
    public GameObject SuccessLabel;

    public GameObject EmptyBoard;

    public enum Label
    {
        Info,
        Success,
    }



    // default -1 nghĩa chưa biết
    int _localSeat = -1;
    public int localSeat {
        get => _localSeat;
        private set {
            _localSeat = value;
            StartCoroutine(DelayedSeatEvent());
        }
    }
    IEnumerator DelayedSeatEvent()
    {
        yield return null;  
        yield return null;  
        OnLocalSeatSet?.Invoke(_localSeat);
    }

    public event Action<int> OnLocalSeatSet;

    void Awake() => Instance = this;

    void Start()
    {
        if (chooseComboPanel != null)
            chooseComboPanel.SetActive(false);
        if (InfoLabel != null)
            InfoLabel.SetActive(false);
        if (SuccessLabel != null)
            SuccessLabel.SetActive(false);
    }

    // public setter để PlayerController gọi
    public void SetLocalSeat(int seat) {
        if (_localSeat == seat) return;
        localSeat = seat;
    }

    public int GetDisplaySeat(int realSeat) {
        if (_localSeat < 0) return realSeat; // fallback tạm thời
        return (realSeat - _localSeat + 4) % 4;
    }

    //General

    public void displayChooseComboScene(bool open)
    {
        if (chooseComboPanel != null)
            chooseComboPanel.SetActive(open);

        if (open)
        {
            if (OKButton != null)
            {
                OKButton.onClick.RemoveAllListeners();
                OKButton.onClick.AddListener(() => GameManager.Instance.thisPlayer.Rpc_OnCallButton());
            }

            if (CancelButton != null)
            {
                CancelButton.onClick.RemoveAllListeners();
                CancelButton.onClick.AddListener(() => GameManager.Instance.thisPlayer.Rpc_OnCancelCallButton());
            }
        }
        else
        {
            if (OKButton != null)
                OKButton.onClick.RemoveAllListeners();
            if (CancelButton != null)
                CancelButton.onClick.RemoveAllListeners();
        }
    }
    
    public void ShowSystemMessage(Label label, string msg, float time)
    {
        StartCoroutine(DisplayInfoLabel(label, msg, time));
    }

    public IEnumerator DisplayInfoLabel(Label label, string text, float showTime)
    {
        if (label == Label.Info && InfoLabel == null && InfoLabel.GetComponentInChildren<TextMeshProUGUI>() == null) 
            yield break;
        else if (label == Label.Success && SuccessLabel == null && SuccessLabel.GetComponentInChildren<TextMeshProUGUI>() == null)
            yield break;

        float fadeDuration = 0f;

        GameObject labelRoot = new GameObject();
        if (label == Label.Info)
        {
            labelRoot = InfoLabel;
            fadeDuration = showTime / 5f;
        }
        else if (label == Label.Success)
        {
            labelRoot = SuccessLabel;
            fadeDuration = showTime / 4f;
        }

        if (!labelRoot) yield break;

        labelRoot.GetComponentInChildren<TextMeshProUGUI>().text = text;
        labelRoot.SetActive(true);
        
        Image labelImg = labelRoot.GetComponent<Image>();
        
        CanvasGroup cg = labelRoot.GetComponent<CanvasGroup>();
        cg.alpha = 0f;


        // Fade in
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            cg.alpha = t / fadeDuration;
            yield return null;
        }
        cg.alpha = 1f;

        // Keep
        yield return new WaitForSeconds(showTime);

        // Fade out
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            cg.alpha = 1f - t / fadeDuration;
            yield return null;
        }
        cg.alpha = 0f;

        labelRoot.SetActive(false);
    }


}