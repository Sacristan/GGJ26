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

    private const string ObjectiveText =
        "Incinerate the <b><color=green>Infected</color></b>! Save the rest! The world counts on us...";

    private const string IntroText =
        "<b>Glory to <color=red>PATROL</color>!</b>";

    private const string GoodJob = "<color=green>Good job</color> - another soul saved!";
    private const string BadJob = "<b><color=red>CITATION</color>!</b>";

    private int peopleSaved = 0;
    private int citations = 0;

    private bool passiveShowStats = false;

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

                if (!_uiMarquee.IsVisible)
                {
                    if (passiveShowStats)
                    {
                        _uiMarquee.SetText(
                            $"Saved: <color=green>{peopleSaved}</color> Citations: <color=red>{citations}</color>",
                            overrideCurrent: false, speed: 40);
                    }

                    else
                    {
                        _uiMarquee.SetText(ObjectiveText, overrideCurrent: false, speed: 60);
                    }

                    passiveShowStats = !passiveShowStats;
                }
            } while (true);
        }
    }
}