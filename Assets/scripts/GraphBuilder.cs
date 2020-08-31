using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the graph
/// </summary>
public class GraphBuilder : MonoBehaviour
{
    static Graph<Waypoint> graph = new Graph<Waypoint>();
    
    private float boundaryX = 3.5f;
    private float boundaryY = 3.0f;

    /// <summary>
    /// Awake is called before Start
    /// </summary>
    void Awake()
    {
        // get the nodes waypoints component
        Waypoint start = GameObject.FindWithTag("Start").GetComponent<Waypoint>();
        Waypoint end = GameObject.FindWithTag("End").GetComponent<Waypoint>();
        GameObject[] waypoints = GameObject.FindGameObjectsWithTag("Waypoint");

        // add nodes (all waypoints, including start and end) to graph
        graph.AddNode(start);
        graph.AddNode(end);

        foreach (GameObject waypoint in waypoints)
        {
            graph.AddNode(waypoint.GetComponent<Waypoint>());
        }

        // add edges to graph
        foreach (GraphNode<Waypoint> curNode in graph.Nodes)
        {
            foreach (GraphNode<Waypoint> otherNode in graph.Nodes)
            {
                float distanceX = Mathf.Abs(curNode.Value.Position.x - otherNode.Value.Position.x);
                float distanceY = Mathf.Abs(curNode.Value.Position.y - otherNode.Value.Position.y);

                if ((distanceX <= boundaryX) && (distanceY <= boundaryY))
                {
                    // add edge
                    float distance = Vector3.Distance(curNode.Value.Position, otherNode.Value.Position);
                    graph.AddEdge(curNode.Value, otherNode.Value, distance);
                }
            }
        }
    }

    /// <summary>
    /// Gets the graph
    /// </summary>
    /// <value>graph</value>
    public static Graph<Waypoint> Graph
    {
        get { return graph; }
    }
}
