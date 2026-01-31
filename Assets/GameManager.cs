using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private UIMarquee _uiMarquee;

    private void Awake()
    {
        Instance = this;
    }

    private const string ObjectiveText = "Incinerate the <b><color=green>Infected</color></b>! Save the rest! The world counts on us...";

    private const string IntroText =
        "<b>Glory to <color=red>PATROL</color>!</b>";

    private const string GoodJob = "<color=green>Good job</color> - another soul saved!";
    private const string BadJob = "<b><color=red>CITATION</color>!</b>";

    private void Start()
    {
        _uiMarquee = FindAnyObjectByType<UIMarquee>();

        _uiMarquee.SetText($"{ObjectiveText} {IntroText}");
        StartCoroutine(TextRoutine());

        IEnumerator TextRoutine()
        {
            var wait = new WaitForSeconds(3f);

            do
            {
                yield return wait;
                if (_uiMarquee.IsVisible) _uiMarquee.SetText(ObjectiveText, overrideCurrent: false, speed: 60);
            } while (true);
        }
    }
}