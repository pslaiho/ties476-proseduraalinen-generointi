using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// A*-polun etsiminen kaksiulotteisesta float-taulukosta
// K‰ytet‰‰n kaksiulotteisen taulukon koordinaattien s‰ilˆmiseen Vector2Int-luokkaa
public class AStar
{
	public Node[,] nodes;

	/// <summary>
	/// Luo annetusta float-taulukosta olion, josta voidaan hakea polkuja
	/// </summary>
	/// <param name="grid">kaksiulotteinen float-taulukko</param>
	/// <param name="minValue">Raja-arvo, jota pienemmist‰ arvoista ei voi kulkea</param>
	/// <param name="maxValue">Raja-arvo, jota suuremmista ei voi kulkea</param>
	public AStar(float[,] grid, float minValue, float maxValue)
	{
		int width = grid.GetLength(0);
		int height = grid.GetLength(1);
		nodes = new Node[width, height];

		// K‰‰nnet‰‰n annettu float-taulukko k‰ytt‰m‰‰n algoritmin node-elementtej‰
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
                bool traversable = grid[x, y] >= minValue && grid[x, y] <= maxValue;
				nodes[x, y] = new Node(traversable, new Vector2Int(x, y));
			}
		}
	}

	/// <summary>
	/// Hakee Lyhimm‰n reitin kahden pisteen v‰lill‰
	/// </summary>
	/// <param name="start">Alkupiste</param>
	/// <param name="end">Loppupiste</param>
	/// <returns>Reitti listana taulukko-koordinaatteja</returns>
	public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
	{
		int width = nodes.GetLength(0);
		int height = nodes.GetLength(1);

		// Seuraavaksi tutkittavat solut, eli ne, joiden naapurissa on k‰yty
		List<Node> openSet = new List<Node>();
		// Jo tutkitut solut
		HashSet<Node> closedSet = new HashSet<Node>();

		openSet.Add(nodes[start.x, start.y]);

		while (openSet.Count > 0)
		{
			// Etsit‰‰n t‰ll‰ hetkill‰ solu, joka on suotuisin
			Node node = openSet[0];
			for (int i = 1; i < openSet.Count; i++)
				if (openSet[i].FCost() <= node.FCost())
					if (openSet[i].hCost < node.hCost)
						node = openSet[i];

			openSet.Remove(node);
			closedSet.Add(node);

			// Jos ollaan p‰‰sty maaliin
			if (node.pos == end)
				return GetPath(nodes[start.x, start.y], nodes[end.x, end.y]);

			// K‰yd‰‰n l‰pi solun naapurit
			for (int i = 0; i < dirs.Length; i++)
			{
				int x = node.pos.x + dirs[i].x;
				int y = node.pos.y + dirs[i].y;
				// Varmistetaan, ettei menn‰ rajojen yli
				if (x < 0 || x >= width || y < 0 || y >= height)
					continue;

				Node neighbor = nodes[x, y];

				// Voidaanko kulkea, eik‰ olla viel‰ kuljettu
				if (!neighbor.traversable || closedSet.Contains(neighbor))
					continue;

				// Lasketaan matka uuteen naapuriin
				int cost = node.gCost + GetDistance(node.pos, neighbor.pos);
				// Jos uusi reitti parempi kuin naapurin aikaisempi
				if (cost < neighbor.gCost || !openSet.Contains(neighbor))
				{
					neighbor.gCost = cost;
					neighbor.hCost = GetDistance(neighbor.pos, end);
					neighbor.parent = node;
					if (!openSet.Contains(neighbor))
						openSet.Add(neighbor);
				}
			}
		}
		// Jos reitti‰ ei lˆydy ollenkaan, palautuu tyhj‰ lista
		return new List<Vector2Int>();
	}

	/// <summary>
	/// Sen j‰lkeen, kun polku on lˆydetty, k‰‰nnet‰‰n se takaisin Vector2Int-muotoon
	/// </summary>
	/// <param name="startNode">Polun alkup‰‰</param>
	/// <param name="endNode">Polun loppup‰‰</param>
	/// <returns></returns>
	List<Vector2Int> GetPath(Node startNode, Node endNode)
	{
		List<Vector2Int> path = new();
		Node currentNode = endNode;

		// K‰yd‰‰n reitti l‰pi lopusta alkuun, polun soluilla aina viittaus edelliseen
		while (currentNode != startNode)
		{
			path.Add(currentNode.pos);
			currentNode = currentNode.parent;
		}
		path.Reverse();

		return path;

	}

	/// <summary>
	/// Hakee matkan kahden pisteen v‰lill‰, matka viereiseen 10, matka viistossa viereiseen 14
	/// </summary>
	/// <param name="a">ensimm‰inen piste</param>
	/// <param name="b">toinen piste</param>
	/// <returns>Matka pisteiden v‰lill‰</returns>
	int GetDistance(Vector2Int a, Vector2Int b)
	{
		int x = Mathf.Abs(a.x - b.x);
		int y = Mathf.Abs(a.y - b.y);

		if (x > y)
			return 14 * y + 10 * (x - y);
		return 14 * x + 10 * (y - x);
	}

	// Aputaulukko 8sta suuntavektorista, k‰ytet‰‰n hakemaan solun naapurit
	public Vector2Int[] dirs = {
		new Vector2Int(0, 1),
		new Vector2Int(1, 1),
		new Vector2Int(1, 0),
		new Vector2Int(1, -1),
		new Vector2Int(0, -1),
		new Vector2Int(-1, -1),
		new Vector2Int(-1, 0),
		new Vector2Int(-1, 1)
	};
}

// A*star-algoritmissa k‰ytett‰v‰ apuluokka, jossa s‰ilˆt‰‰n yhden solun tarvittavat tiedot
public class Node
{
	public bool traversable;
	public Vector2Int pos;

	// Et‰isyys alkupisteest‰
	public int gCost;
	// Et‰isyys loppupisteest‰
	public int hCost;
	// Polussa edellinen
	public Node parent;

	/// <summary>
	/// Yksi solu taulukossa
	/// </summary>
	/// <param name="traversable">Voiko solun l‰pi liikkua</param>
	/// <param name="pos">Solun koordinaatit</param>
	public Node(bool traversable, Vector2Int pos)
	{
		this.traversable = traversable;
		this.pos = pos;
	}

	/// <summary>
	/// Solun edullisuus, kannattaako solun kautta kulkea
	/// </summary>
	public int FCost()
	{
		return gCost + hCost;
	}
}
