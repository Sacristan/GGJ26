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

    private const string GoodJob = "<color=green>Good job!</color>";
    private const string BadJob = "<b><color=red>CITATION!</color></b>";

    private int peopleSaved = 0;
    private int citations = 0;

    private bool passiveShowStats = false;

    private void Start()
    {
        NPC.OnSaved += NPCOnOnSaved;
        NPC.OnIncinerated += NPCOnOnIncinerated;

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
                            $"{GetSavedText()} {GetCitationsText()}",
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

    string GetSavedText() => $"Saved: <color=green>{peopleSaved}</color>";
    string GetCitationsText() => $"Citations: <color=red>{citations}</color>";

    private void OnDestroy()
    {
        NPC.OnSaved -= NPCOnOnSaved;
        NPC.OnIncinerated -= NPCOnOnIncinerated;
    }

    private void NPCOnOnIncinerated(NPC npc)
    {
        if (npc.IsInfected) GoodJob_Incinerated_Infected();
        else Citation_Incinerated_Uninfected();
    }

    private void NPCOnOnSaved(NPC npc)
    {
        if (npc.IsInfected) Citation_LetInfectedLive();
        else GoodJob_Saved();
    }

    void GoodJob_Incinerated_Infected()
    {
        _uiMarquee.SetText($"{GoodJob} world is a safer place! {GetSavedText()}", overrideCurrent: true, speed: 60);
    }

    void GoodJob_Saved()
    {
        peopleSaved++;
        _uiMarquee.SetText($"{GoodJob} A thankful citizen added to our flock! {GetSavedText()}", overrideCurrent: true, speed: 60);
    }

    void Citation_Incinerated_Uninfected()
    {
        citations++;
        _uiMarquee.SetText($"{BadJob} a healthy citizen was incinerated! {GetCitationsText()}", overrideCurrent: true, speed: 60);
    }

    void Citation_LetInfectedLive()
    {
        citations++;
        _uiMarquee.SetText($"{BadJob} an infected was allowed to endanger our citizens! {GetCitationsText()}",
            overrideCurrent: true, speed: 60);
    }
}