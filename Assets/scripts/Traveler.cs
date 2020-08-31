using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A traveler
/// </summary>
public class Traveler : MonoBehaviour
{
    PathFoundEvent pathFoundEvent = new PathFoundEvent();
    PathTraversalCompleteEvent pathTraversalCompleteEvent = new PathTraversalCompleteEvent();
    SortedLinkedList<SearchNode<Waypoint>> searchList = new SortedLinkedList<SearchNode<Waypoint>>(); 
    Dictionary<GraphNode<Waypoint>, SearchNode<Waypoint>> searchDict = new Dictionary<GraphNode<Waypoint>, SearchNode<Waypoint>>();

    GraphNode<Waypoint> startNode;
    GraphNode<Waypoint> endNode;

    LinkedListNode<SearchNode<Waypoint>> targetNode;
    LinkedList<SearchNode<Waypoint>> targets;

    SearchNode<Waypoint> curSearchNodeNeighbor = null;
    SearchNode<Waypoint> curSearchNode = null;
    GraphNode<Waypoint> curGraphNode = null;

    Rigidbody2D rb2d;

    const float BaseImpulseForceMagnitude = 2.0f;
    const float movementSpeed = 3.0f;

    [SerializeField]
    GameObject explosion;

    /// <summary>
    /// Use this for initialization
    /// </summary>
    void Start()
	{
        rb2d = GetComponent<Rigidbody2D>();

        Graph<Waypoint> graph = GraphBuilder.Graph;
        startNode = graph.Find(GameObject.FindWithTag("Start").GetComponent<Waypoint>());
        endNode = graph.Find(GameObject.FindWithTag("End").GetComponent<Waypoint>());

        float distance;

        foreach (GraphNode<Waypoint> node in graph.Nodes)
        {
            SearchNode<Waypoint> searchNode = new SearchNode<Waypoint>(node);
            if (node == startNode)
            {
                searchNode.Distance = 0;
            }
            searchList.Add(searchNode);
            searchDict[node] = searchNode;
        }

        while (searchList.Count > 0)
        {
            curSearchNode = searchList.ElementAt(0);
            searchList.RemoveFirst();
            curGraphNode = curSearchNode.GraphNode;
            searchDict.Remove(curGraphNode);

            if (curGraphNode == endNode)
            {
                targets = ConvertPathToString(curSearchNode);
                //PrintPathToString(targets);
            }

            foreach (GraphNode<Waypoint> curGraphNodeNeighbor in curGraphNode.Neighbors)
            {
                if (searchDict.ContainsKey(curGraphNodeNeighbor))
                {
                    distance = curSearchNode.Distance + curGraphNode.GetEdgeWeight(curGraphNodeNeighbor);
                    curSearchNodeNeighbor = searchDict[curGraphNodeNeighbor];

                    if (distance < curSearchNodeNeighbor.Distance)
                    {
                        curSearchNodeNeighbor.Distance = distance;
                        curSearchNodeNeighbor.Previous = curSearchNode;
                        searchList.Reposition(curSearchNodeNeighbor);
                    }
                }
            }
        }

        EventManager.AddPathFoundInvoker(this);
        EventManager.AddPathTraversalCompleteInvoker(this);

        SetTarget(ConvertPathToString(curSearchNode).First);
    }
	
    /// <summary>
    /// Adds the given listener for the PathFoundEvent
    /// </summary>
    /// <param name="listener">listener</param>
    public void AddPathFoundListener(UnityAction<float> listener)
    {
        pathFoundEvent.AddListener(listener);
    }

    /// <summary>
    /// Adds the given listener for the PathTraversalCompleteEvent
    /// </summary>
    /// <param name="listener">listener</param>
    public void AddPathTraversalCompleteListener(UnityAction listener)
    {
        pathTraversalCompleteEvent.AddListener(listener);
    }

    /// <summary>
    /// Converts the given end node and path node information
    /// to a path from the start node to the end node
    /// </summary>
    /// <param name="path">path to convert</param>
    /// <returns>string for path</returns>
    static LinkedList<SearchNode<Waypoint>> ConvertPathToString(SearchNode<Waypoint> endSearchNode)
    {
        // build linked list for path in correct order
        LinkedList<SearchNode<Waypoint>> path = new LinkedList<SearchNode<Waypoint>>();
        path.AddFirst(endSearchNode);
        SearchNode<Waypoint> previous = endSearchNode.Previous;
        while (previous != null)
        {
            path.AddFirst(previous);
            previous = previous.Previous;
        }
        return path;
    }

    void Move(GraphNode<Waypoint> startNode, LinkedList<SearchNode<Waypoint>> path)
    {
        // Initializes the traveller
        transform.position = startNode.Value.Position;

        // Traverse through the path
        LinkedListNode<SearchNode<Waypoint>> currentNode = path.First;
        int nodeCount = 0;
        while (currentNode != null)
        {
            nodeCount++;
            currentNode = currentNode.Next;
            if (nodeCount < path.Count)
            {
                transform.position = Vector3.MoveTowards(transform.position, currentNode.Value.GraphNode.Value.Position, Time.deltaTime * movementSpeed);
            }
        }
    }

    void PrintPathToString(LinkedList<SearchNode<Waypoint>> path)
    {
        StringBuilder pathString = new StringBuilder();
        LinkedListNode<SearchNode<Waypoint>> currentNode = path.First;
        int nodeCount = 0;
        while (currentNode != null)
        {
            nodeCount++;
            pathString.Append(currentNode.Value.GraphNode.Value.Id);
            if (nodeCount < path.Count)
            {
                pathString.Append(" ");
            }
            currentNode = currentNode.Next;
        }
        print(pathString.ToString());
    }
    
    
    void SetTarget(LinkedListNode<SearchNode<Waypoint>> target)
    {
        pathFoundEvent.Invoke(curSearchNode.Distance);
        targetNode = target;
        GoToTargetPickup();
    }
    void GoToTargetPickup()
    {
        // calculate direction to target pickup and start moving toward it
        Vector2 direction = new Vector2(
            targetNode.Value.GraphNode.Value.Position.x - transform.position.x,
            targetNode.Value.GraphNode.Value.Position.y - transform.position.y);
        direction.Normalize();
        rb2d.velocity = Vector2.zero;
        rb2d.AddForce(direction * BaseImpulseForceMagnitude, ForceMode2D.Impulse);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // only respond if the collision is with the target pickup
        if (other.gameObject == targetNode.Value.GraphNode.Value.gameObject)
        {
            if ((targetNode.Value.GraphNode != startNode) && (targetNode.Value.GraphNode != endNode))
            {
                other.gameObject.GetComponent<SpriteRenderer>().color = Color.green;
            }


            rb2d.velocity = Vector2.zero;

            // go to next target if there is one
            if (targetNode.Next != null)
            {
                SetTarget(targetNode.Next);
            }
            else
            {
                GameObject graphBuilder = GameObject.Find("GraphBuilder");
                graphBuilder.GetComponent<EdgeRenderer>().StopDrawingEdges();
                ExplodeWaypoints();
            }
        }
    }

    void ExplodeWaypoints()
    {
        LinkedListNode<SearchNode<Waypoint>> currentNode = targets.First;
        LinkedListNode<SearchNode<Waypoint>> tempNode;
        Transform targetPosition; 
        while (currentNode != null)
        {
            tempNode = currentNode;
            currentNode = currentNode.Next;
            if ((tempNode.Value.GraphNode != startNode) && (tempNode.Value.GraphNode != endNode))
            {
                targetPosition = tempNode.Value.GraphNode.Value.gameObject.transform;
                Destroy(tempNode.Value.GraphNode.Value.gameObject);
                Instantiate(explosion, targetPosition);
            }
        }
    }   
}
