using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NPCSpawner : MonoBehaviour
{
    public static NPCSpawner Instance { get; private set; }

    [SerializeField] private InspectLoc inspectLoc;
    [SerializeField] private InspectLoc exitLoc;
    [SerializeField] private InspectLoc[] queueLocs;
    [SerializeField] private NPC[] npcToSpawn;

    private List<NPC> queuedNPCs = new List<NPC>();
    private NPC currentInspectingNPC;

    public InspectLoc ExitLoc => exitLoc;

    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < queueLocs.Length; i++)
        {
            SpawnNPCInQueue(i);
        }

        yield return null;

        MoveToInspectPoint();
    }

    public void MoveToInspectPoint()
    {
        if (currentInspectingNPC != null)
        {
            Debug.LogWarning("Inspect location is occupied!");
            return;
        }

        if (queuedNPCs.Count == 0)
        {
            Debug.LogWarning("No NPCs in queue!");
            return;
        }

        // Move first NPC to inspect location
        currentInspectingNPC = queuedNPCs[0];
        queuedNPCs.RemoveAt(0);

        // TODO: Call NPC method to move to inspect location
        currentInspectingNPC.MoveTo(inspectLoc);
        SayArrivalLineOnceReady(currentInspectingNPC);
        
        // Move queue forward
        MoveQueueForward();

        // Spawn new NPC if there's space
        if (queuedNPCs.Count < queueLocs.Length)
        {
            SpawnNPCInQueue(queuedNPCs.Count);
        }
    }

    void SayArrivalLineOnceReady(NPC npc)
    {
        
        StartCoroutine(Routine());

        IEnumerator Routine()
        {
            yield return new WaitUntil(() => npc.Locomotion.IsCloseEnoughToTarget());
            npc.ArrivedAtInspection();//TODO dirty af
            npc.speech.Say(CharacterSpeech.SpeechType.Arrive, delay: 1f);
        }
    }

    private void FreeInspectPoint()
    {
        currentInspectingNPC = null;
        MoveToInspectPoint();
    }

    private void MoveQueueForward()
    {
        for (int i = 0; i < queuedNPCs.Count; i++)
        {
            if (queuedNPCs[i] != null)
            {
                queuedNPCs[i].MoveTo(queueLocs[i]);
            }
        }
    }

    public void NPCHandled(NPC npc)
    {
        Destroy(npc.gameObject);
        FreeInspectPoint();
    }

    public void MarkSafe(NPC npc)
    {
        FreeInspectPoint();
    }

    private void SpawnNPCInQueue(int slotIndex)
    {
        Debug.Log($"Spawning NPC: @ {slotIndex}");

        if (slotIndex >= queueLocs.Length) return;

        NPC newNPC = Instantiate(
            npcToSpawn[Random.Range(0, npcToSpawn.Length)],
            queueLocs[slotIndex].transform.position,
            queueLocs[slotIndex].transform.rotation
        );

        queuedNPCs.Add(newNPC);
    }
}