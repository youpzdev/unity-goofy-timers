using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerTest : MonoBehaviour
{
    [SerializeField] private TMP_Text counter1Text;
    [SerializeField] private TMP_Text counter2Text;
    [SerializeField] private TMP_Text countdownText;

    [SerializeField] private Button toggle1Button;
    [SerializeField] private Button toggle2Button;
    [SerializeField] private Button toggle3Button;

    private int counter1;
    private int counter2;
    private float countdown;

    private Action cancel1;
    private Action cancel2;
    private Action cancelFrame;
    private Action cancelFinish;

    private void Start()
    {
        toggle1Button.onClick.AddListener(Toggle1);
        toggle2Button.onClick.AddListener(Toggle2);
        toggle3Button.onClick.AddListener(ToggleCountdown);
    }

    private void Toggle1()
    {
        if (cancel1 != null) { cancel1(); cancel1 = null; return; }
        cancel1 = Timer.Repeat(1f, AddCounter1);
    }

    private void Toggle2()
    {
        if (cancel2 != null) { cancel2(); cancel2 = null; return; }
        cancel2 = Timer.Repeat(3f, AddCounter2);
    }

    private void ToggleCountdown()
    {
        if (cancelFrame != null)
        {
            cancelFrame(); cancelFrame = null;
            cancelFinish(); cancelFinish = null;
            countdownText.text = "10.00";
            return;
        }

        countdown = 10f;
        cancelFrame = Timer.EveryFrame(UpdateCountdown);
        cancelFinish = Timer.After(10f, FinishCountdown);
    }

    private void UpdateCountdown()
    {
        countdown -= Time.deltaTime;
        countdownText.text = Mathf.Max(countdown, 0f).ToString("F2");
    }

    private void FinishCountdown()
    {
        countdownText.text = "Watermelon";
        cancelFrame?.Invoke();
        cancelFrame = null;
    }

    private void AddCounter1() => counter1Text.text = (++counter1).ToString();
    private void AddCounter2() => counter2Text.text = (++counter2).ToString();
}