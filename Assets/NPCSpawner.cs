using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [SerializeField] private InspectLoc inspectLoc;
    [SerializeField] private InspectLoc[] queueLocs;
    [SerializeField] private NPC[] npcToSpawn;

    private List<NPC> queuedNPCs = new List<NPC>();
    private NPC currentInspectingNPC;

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

        // Move queue forward
        MoveQueueForward();

        // Spawn new NPC if there's space
        if (queuedNPCs.Count < queueLocs.Length)
        {
            SpawnNPCInQueue(queuedNPCs.Count);
        }
    }

    public void FreeInspectPoint()
    {
        if (currentInspectingNPC != null)
        {
            Destroy(currentInspectingNPC.gameObject);
            currentInspectingNPC = null;
        }
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