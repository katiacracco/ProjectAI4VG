using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
	public Transform agent, target;
	private Grid grid;
	private List<Node> path;
	private DecisionTree dt;
	private int counter = 0;
	public float ray;
	private bool isValid = false;

    void Start()
    {
		grid = GetComponent<Grid>();

		// Define actions
        DTAction a1 = new DTAction(TakeStep);
        DTAction a2 = new DTAction(RecalculatePath);

        // Define decision
        DTDecision d1 = new DTDecision(StepValidation);

        // Link action with decisions
        d1.AddLink(true, a1);
        d1.AddLink(false, a2);

        // Setup DecisionTree at the root node
        dt = new DecisionTree(d1);

        // Start checking the path
		FindPath(agent.position,target.position);
    }

	void FixedUpdate() {
		if (agent.position != target.position && counter < path.Count) // check on counter to avoid ArgumentOutOfBound exception
        {
            dt.walk();
        }
	}

    void FindPath(Vector3 startPos, Vector3 targetPos)
    {
		Node startNode = grid.NodeFromWorldPoint(startPos);
		Node targetNode = grid.NodeFromWorldPoint(targetPos);

		List<Node> open = new List<Node>(); // set of nodes to be evaluated
		List<Node> closed = new List<Node>(); // set of nodes already evaluated
		open.Add(startNode);

		// finding the node in the open set with lower fCost
		while (open.Count > 0) {
			Node currentNode = open[0];
			// loop on all nodes in open set
			for (int i=1; i<open.Count; i++) {
				if (open[i].fCost < currentNode.fCost || open[i].fCost == currentNode.fCost && open[i].hCost < currentNode.hCost) {
					currentNode = open[i];
				}
			}

			open.Remove(currentNode);
			closed.Add(currentNode);

			// if we reach the destination
			if (currentNode == targetNode) {
				RetracePath(startNode, targetNode);
				return;
			}
			// otherwise we need to loop on each of the neighbour nodes of the current node
			foreach (Node neighbour in grid.GetNeighbours(currentNode)) {
				if (!neighbour.walkable || closed.Contains(neighbour)) {
					continue;
				}

				// checking if the new path to the neighbour is shorter then the old path
				int newCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
				if (newCostToNeighbour < neighbour.gCost || !open.Contains(neighbour)) {
					neighbour.gCost = newCostToNeighbour;
					neighbour.hCost = GetDistance(neighbour, targetNode);
					neighbour.parent = currentNode;

					if (!open.Contains(neighbour)) {
						open.Add(neighbour);
					}
				}
			}

		} // end while
    } // end for

	// retrace the path from the end node to the starting node and reverse it
	void RetracePath(Node startNode, Node endNode) {
		path = new List<Node>();
		Node currentNode = endNode;

		while (currentNode != startNode) {
			path.Add(currentNode);
			currentNode = currentNode.parent;
		}
		path.Reverse();

		grid.path = path;
	}

	// return the distance between two nodes
	int GetDistance(Node nodeA, Node nodeB) {
		int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
		int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

		if (dstX > dstY)
			return 14*dstY + 10*(dstX-dstY); // 14 = sqrt(2) * 10
		return 14*dstX + 10*(dstY-dstX);
	}

    // ACTIONS

	// the agent do a step
    public object TakeStep(object o)
    {
		float speed = 1.0f;
        Vector3 newPos = path[counter].worldPosition + new Vector3(0,1.1f,0);
				Vector3 verticalAdj = new Vector3 (target.position.x, agent.position.y, target.position.z);
				agent.LookAt(verticalAdj);
        agent.position = Vector3.MoveTowards(agent.position, newPos, speed * Time.deltaTime);
				agent.position = newPos;
        counter++;
        return null;
    }

	// the path is recalculated from the actual position of the agent to the destination
    public object RecalculatePath(object o)
    {
				grid.ModifyNode(path[counter]);
        FindPath(agent.position, target.position);
				counter = 0;
        return null;
    }

    // DECISION

	// check if the following step is valid
    public object StepValidation(object o)
    {
				isValid = !(Physics.CheckSphere(path[counter].worldPosition, ray, grid.unwalkableMask));
        return isValid;
    }

}
