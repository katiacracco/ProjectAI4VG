using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public LayerMask unwalkableMask;
    private Node[,] grid;

    int gridSizeX, gridSizeY;

	  public Transform obstaclePrefab;
    public Vector2 mapSize;

	  private List<Coord> allTileCoords;
    private Queue<Coord> shuffledTileCoords;

    public int seed;
	  private List<Vector3> obstacles;

    void Awake()
    {
        gridSizeX = Mathf.RoundToInt(mapSize.x);
		    gridSizeY = Mathf.RoundToInt(mapSize.y);
	      CreateObstacles();
        CreateGrid();
    }


    // create some pseudo-random obstacles on the map
	public void CreateObstacles() {
		// creating a queue with shuffled coords
        allTileCoords = new List<Coord>();
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                allTileCoords.Add(new Coord(x, y));
            }
        }
        allTileCoords.Remove(new Coord(0, 0));
        allTileCoords.Remove(new Coord(9, 9));
        shuffledTileCoords = new Queue<Coord>(ShuffleArray(allTileCoords.ToArray(), seed));

        // destroying old tiles at every new generation
        string holderName = "Generated Map";
        if (transform.Find(holderName))
        {
            DestroyImmediate(transform.Find(holderName).gameObject);
        }

        Transform mapHolder = new GameObject(holderName).transform;
        mapHolder.parent = transform;

		int obstacleCount = 10;
		obstacles = new List<Vector3>();
        for (int i = 0; i < obstacleCount; i++)
        {
            Coord randomCoord = GetRandomCoord();
            Vector3 obstaclePosition = CoordToPosition(randomCoord.x, randomCoord.y);

            /*
            Coord randomCoord;
            Vector3 obstaclePosition;
            do
            {
	            randomCoord = new Coord(UnityEngine.Random.Range(0, 9), UnityEngine.Random.Range(0, 9));
	            obstaclePosition = CoordToPosition(randomCoord.x, randomCoord.y);
            } while (obstacles.Contains(obstaclePosition) || randomCoord.isEqual(0,0) || randomCoord.isEqual(9,9));
            */

            Transform newObstacle = Instantiate(obstaclePrefab, obstaclePosition + Vector3.up * .5f, Quaternion.identity) as Transform;
            newObstacle.parent = mapHolder;
			      obstacles.Add(obstaclePosition);
        }
	}

	public struct Coord
    {
        public int x;
        public int y;

        public Coord(int _x, int _y)
        {
            x = _x;
            y = _y;
        }

        public bool isEqual(int a, int b)
        {
	        return a == x && b == y;
        }
    }

	// take the first element of the queue, which is returned, and put it back in tail
	public Coord GetRandomCoord()
    {
        Coord randomCoord = shuffledTileCoords.Dequeue();
        shuffledTileCoords.Enqueue(randomCoord);
        return randomCoord;
    }

	// take the tile's coordinates (from 0 to 9) and map them into the world positions (from -5 to +5)
	Vector3 CoordToPosition(int x, int y)
    {
        return new Vector3(-mapSize.x / 2 + 0.5f + x, 0, -mapSize.y / 2 + 0.5f + y);
    }


	// apply a random shuffle to the array with the coordinates of all the tiles
	public static T[] ShuffleArray<T>(T[] array, int seed) // T is a generic type
    {
        System.Random prng = new System.Random(seed);

        for (int i = 0; i < array.Length - 1; i++)
        {
            int randomIndex = prng.Next(i, array.Length);
            T tmp = array[randomIndex];
            array[randomIndex] = array[i];
            array[i] = tmp;
        }

        return array;
    }

	// create a node for every tile
	public void CreateGrid()
	{
		grid = new Node[gridSizeX, gridSizeY];
		Vector3 worldPoint;
		bool walkable;

		for (int x = 0; x < gridSizeX; x++)
		{
			for (int y = 0; y < gridSizeY; y++)
			{
				worldPoint = new Vector3(-gridSizeX/2 + 0.5f + x, 0, -gridSizeY/2 + 0.5f + y);
				walkable = !obstacles.Contains(worldPoint);
				grid[x, y] = new Node(walkable, worldPoint, x, y);
			}
		}
	}

	// given a node, return the list of neighbours
	public List<Node> GetNeighbours(Node node) {
		List<Node> neighbours = new List<Node>();

		for (int x=-1; x<=1; x++) {
			for (int y=-1; y<=1; y++) {
				if (x==0 && y==0) {
					continue;
				}

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX>=0 && checkX<gridSizeX && checkY>=0 && checkY<gridSizeY) {
					neighbours.Add(grid[checkX, checkY]);
				}
			}
		}
		return neighbours;
	}

	// transform the world position (from -5 to +5) into the grid's coordinates (from 0 to 9)
	public Node NodeFromWorldPoint(Vector3 worldPosition)
	{
		float percentX = (worldPosition.x + mapSize.x / 2) / mapSize.x;
		float percentY = (worldPosition.z + mapSize.y / 2) / mapSize.y;
		percentX = Mathf.Clamp01(percentX); // return a number between 0 and 1
		percentY = Mathf.Clamp01(percentY);

		int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
		int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
		return grid[x, y];
	}

	public List<Node> path;

	public void ModifyNode(Node n) {
		grid[n.gridX, n.gridY].walkable = false;
	}

	// function that color the map:
	// red if the node is not walkable, black if the node is on the path, white in all other cases
    void OnDrawGizmos()
    {
	    if (grid != null)
        {
            foreach (Node n in grid)
            {
                Gizmos.color = (n.walkable) ? Color.white : Color.red;
                if (path != null) {
					if (path.Contains(n)) {
						Gizmos.color = Color.black;
					}
				}
                Gizmos.DrawCube(n.worldPosition, Vector3.one * (1-.1f));
            }
        }
    }
}
