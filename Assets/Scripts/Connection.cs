using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Connection
{
private float cost = 0;
public float Cost
{
get
{
if (cost == 0)
{
cost = Vector3.Distance(FromNode.transform.position,
ToNode.transform.position);
}
return cost;
}
set { cost = value; }
}
private GameObject fromNode;
public GameObject FromNode
{
get { return fromNode; }
set
{
fromNode = value;
cost = 0;
}
}
private GameObject toNode;
public GameObject ToNode
{
get { return toNode; }
set
{
toNode = value;
cost = 0;
}
}

// ---------------- ACO PHEROMONE SYSTEM ----------------
private float pheromone = 1.0f; // Default pheromone level
public float Pheromone
{
get { return pheromone; }
set { pheromone = Mathf.Max(0f, value); } // Ensure non-negative
}

// Default constructor.
public Connection()
{
pheromone = 1.0f; // Default initial pheromone
}
}
