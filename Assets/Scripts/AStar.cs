using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStar
{
    public AStar() { }

    public List<Connection> PathfindAStar(Graph aGraph, GameObject start, GameObject end, Heuristic myHeuristic)
    {
        Debug.Log($"[A*] Starting pathfinding from {start.name} → {end.name}");

        // Set up the start record.
        NodeRecord StartRecord = new NodeRecord
        {
            Node = start,
            Connection = null,
            CostSoFar = 0,
            EstimatedTotalCost = myHeuristic.Estimate(start, end)
        };

        // Create the lists.
        PathfindingList OpenList = new PathfindingList();
        PathfindingList ClosedList = new PathfindingList();

        // Add the start record to the open list.
        OpenList.AddNodeRecord(StartRecord);
        Debug.Log($"[A*] Added start node {start.name} to OpenList");

        // Iterate through and process each node.
        NodeRecord CurrentRecord = null;
        List<Connection> Connections;

        while (OpenList.GetSize() > 0)
        {
            CurrentRecord = OpenList.GetSmallestElement();
            Debug.Log($"[A*] Processing node: {CurrentRecord.Node.name}, CostSoFar={CurrentRecord.CostSoFar}, EstimatedTotal={CurrentRecord.EstimatedTotalCost}");

            // If it is the goal node, then terminate.
            if (CurrentRecord.Node.Equals(end))
            {
                Debug.Log($"[A*] Goal node {end.name} reached!");
                break;
            }

            // Otherwise get its outgoing connections.
            Connections = aGraph.GetConnections(CurrentRecord.Node);
            Debug.Log($"[A*] Found {Connections.Count} connections from {CurrentRecord.Node.name}");

            // Loop through each connection in turn.
            foreach (Connection aConnection in Connections)
            {
                GameObject EndNode = aConnection.ToNode;
                float EndNodeCost = CurrentRecord.CostSoFar + aConnection.Cost;

                NodeRecord EndNodeRecord;
                float EndNodeHeuristic;

                // --- CLOSED LIST CHECK ---
                if (ClosedList.Contains(EndNode))
                {
                    EndNodeRecord = ClosedList.Find(EndNode);
                    if (EndNodeRecord.CostSoFar <= EndNodeCost)
                    {
                        Debug.Log($"[A*] Skipping {EndNode.name} (in ClosedList, higher cost)");
                        continue;
                    }

                    ClosedList.RemoveNodeRecord(EndNodeRecord);
                    EndNodeHeuristic = EndNodeRecord.EstimatedTotalCost - EndNodeRecord.CostSoFar;
                    Debug.Log($"[A*] Reopening node {EndNode.name} from ClosedList");
                }
                // --- OPEN LIST CHECK ---
                else if (OpenList.Contains(EndNode))
                {
                    EndNodeRecord = OpenList.Find(EndNode);
                    if (EndNodeRecord.CostSoFar <= EndNodeCost)
                    {
                        Debug.Log($"[A*] Skipping {EndNode.name} (already in OpenList with lower cost)");
                        continue;
                    }

                    EndNodeHeuristic = EndNodeRecord.EstimatedTotalCost - EndNodeRecord.CostSoFar;
                    Debug.Log($"[A*] Updating node {EndNode.name} in OpenList with a better cost");
                }
                // --- NEW NODE ---
                else
                {
                    EndNodeRecord = new NodeRecord { Node = EndNode };
                    EndNodeHeuristic = myHeuristic.Estimate(EndNode, end);
                    Debug.Log($"[A*] Found new node {EndNode.name} (Heuristic={EndNodeHeuristic})");
                }

                // Update the record
                EndNodeRecord.CostSoFar = EndNodeCost;
                EndNodeRecord.Connection = aConnection;
                EndNodeRecord.EstimatedTotalCost = EndNodeCost + EndNodeHeuristic;

                if (!OpenList.Contains(EndNode))
                {
                    OpenList.AddNodeRecord(EndNodeRecord);
                    Debug.Log($"[A*] Added {EndNode.name} to OpenList with total={EndNodeRecord.EstimatedTotalCost}");
                }
            }

            // Move current record to closed list
            OpenList.RemoveNodeRecord(CurrentRecord);
            ClosedList.AddNodeRecord(CurrentRecord);
            Debug.Log($"[A*] Moved {CurrentRecord.Node.name} to ClosedList");
        }

        // --- END OF MAIN LOOP ---
        List<Connection> tempList = new List<Connection>();

        if (!CurrentRecord.Node.Equals(end))
        {
            Debug.LogWarning($"[A*] No path found from {start.name} to {end.name}!");
            return tempList;
        }

        // Build the path
        Debug.Log("[A*] Constructing path...");
        while (!CurrentRecord.Node.Equals(start))
        {
            tempList.Add(CurrentRecord.Connection);
            CurrentRecord = ClosedList.Find(CurrentRecord.Connection.FromNode);
        }

        tempList.Reverse();
        Debug.Log($"[A*] Path construction complete. Path length = {tempList.Count} connections.");
        return tempList;
    }
}
