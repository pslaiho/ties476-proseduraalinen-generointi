using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public Renderer textureRenderer;

    // This can be set to true in the Inspector to automatically re-render the map on any change.
    public bool autoUpdate;

    // Feel free to make new variables for more colors.
    //public Color groundColor;
    //public Color oceanColor;

    // Lisätty värit, maaston tasojen värit listana, muut erikseen
    public ColorLevel[] colors;
    public Color forestColor;
    public Color buildingColor;
    public Color roadColor;

    [Header("General Settings")]
    public int mapWidth;
    public int mapHeight;
    public Vector2 offset;

    // Lisätyt muokattavat asetukset
    [Range(0, 1)] public float forestMin = 0.55f;
    [Range(0, 1)] public float forestMax = 0.7f;
    [Range(0, 1)] public float roadMin = 0.4f;
    [Range(0, 1)] public float roadMax = 0.7f;
    [Range(1, 9)] public int buildingSize = 3;
    [Range(0, 20)] public int buildingCount = 5;
    [Range(0, 20)] public int roadCount = 3;

    // See documentation for the different noise settings in the Noise.cs file.
    // Feel free to add Range sliders if you want to limit the values.
    [Header("Noise Settings")]
    public float noiseScale;
    public int octaves;
    [Range(0, 1)] public float persistence;
    public float lacunarity;
    public int mapSeed;
    // Rakennuksille ja metsälle omat satunnaisarvot
    public int buildingSeed;
    public int forestSeed;
    //[Range(0, 1)] public float groundLimit;

    public void GenerateMap()
    {
        // If you want to generate additional noisemaps, you can call the function many times with randomized seeds and different options.
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, mapSeed, noiseScale, octaves, persistence, lacunarity, offset);
        // Metsälle oma noiseMap 
        float[,] forestMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, forestSeed, noiseScale, octaves, persistence, lacunarity, offset);

        // This actually draws the map so don't remove it.
        DrawNoiseMap(noiseMap, forestMap);
    }

    /// <summary>
    /// There are used to clamp values in the inspector, since they break some parts of the map.
    /// </summary>
    void OnValidate()
    {
        if (mapWidth < 1) mapWidth = 1;
        if (mapHeight < 1) mapHeight = 1;
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
        if (Mathf.Abs(forestSeed) == Mathf.Abs(mapSeed)) forestSeed++;
        if (forestMax < forestMin) forestMax = forestMin;
        if (roadMax < roadMin) roadMax = roadMin;

    }

    public void DrawNoiseMap(float[,] noiseMap, float[,] forestMap)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        Color[] colourMap = new Color[width * height];

        // Talteen viitteet paikoista, joihin on mahdollista rakentaa rakennus
        List<Vector2Int> potentialBuilds = new List<Vector2Int>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Here we set each colour of each pixel to groundColor.
                // colourMap[y * width + x] = groundColor;

                // You can add if-else clauses or other bits of logic to add
                // different colors based on the noise map's value.

                // This is how you can get the value at a location in the noise map.
                float noise = noiseMap[x, y];
                float forestNoise = forestMap[x, y];

                // Tarkistetaan, onko paikassa tilaa rakentaa rakennus
                if (CheckBuilding(noiseMap, x, y)) 
                    potentialBuilds.Add(new Vector2Int(x, y));  

                // Metsää paikkoihin, joissa molemmat noiseMapit ovat halutulla välillä
                if ((forestMin <= forestNoise && forestNoise <= forestMax) && (forestMin <= noise && noise <= forestMax))
                    colourMap[y * width + x] = forestColor;
                else
                {
                    // Käydään väritaulukko alhaalta ylös, alin sopiva valitaan
                    for (int i = 0; i < colors.Length; i++)
                        if (noise <= colors[i].threshold)
                        {
                            colourMap[y * width + x] = colors[i].color;
                            break;
                        }
                }
            }
        }

        System.Random r = new(buildingSeed);

        // Listataan karttaan tulevat rakennukset
        List<Vector2Int> buildings = new();

        // Valitaan satunnaiset pisteet mahdollisista rakennuksista, joihin laitetaan rakennus
        for (int c = 0; c < buildingCount; c++)
        {
            // jos ei ole mahdollisia jäljellä, lopetetaan ennenaikaisesti
            if (potentialBuilds.Count < 1)  
                break; 

            int i = r.Next(0, potentialBuilds.Count);
            Vector2Int pot = potentialBuilds[i];
            BuildBuilding(colourMap, width, pot.x, pot.y);
            buildings.Add(pot);

            // Suodatetaan pois ne, jotka olisivat liian lähellä rakennettua rakennusta
            potentialBuilds = potentialBuilds.Where(
                b => Mathf.Abs(b.x - pot.x) > buildingSize + 1 
                || Mathf.Abs(b.y - pot.y) > buildingSize + 1
            ).ToList();
        }

        // Rakennetaan teitä joidenkin rakennusten välille
        AStar pathfinder = new(noiseMap, roadMin, roadMax);
        int roads = 0;
        for (int i = 0; i < buildings.Count - 1 && roads < roadCount; i++)
        {
            for (int j = i + 1; j < buildings.Count && roads < roadCount; j++)
            {
                // tiet halutaan alkavan rakennusten keskeltä, talojen koordinaatit säilötty oikeasta yläkulmasta
                Vector2Int offset = new(buildingSize / 2, buildingSize / 2);

                // Valitaan talot, joiden välille rakennetaan tie
                Vector2Int houseFrom = buildings[i] + offset;
                Vector2Int houseTo = buildings[j] + offset;

                List<Vector2Int> path = pathfinder.FindPath(houseFrom, houseTo);
                // Tehdään tie vain, jos se on mahdollinen
                if (path.Count > 0)
                {
                    for (int p = 0; p < path.Count; p++)
                        // Ei haluta tietä rakennuksen päälle
                        if (colourMap[path[p].y * width + path[p].x] != buildingColor)
                            colourMap[path[p].y * width + path[p].x] = roadColor;
                    roads++;
                }
            }
        }

        // These just set colors to the texture and apply it. No need to touch these.
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colourMap);
        texture.Apply();
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(width * 0.1f, 1f, height * 0.1f);
    }

    /// <summary>
    /// Tarkistaa, voiko kohtaan asettaa talon, talot ovat usean koordinaatin laajuisia neliöitä
    /// </summary>
    /// <param name="noiseMap">Kartan noiseMap</param>
    /// <param name="x0">Talon alkukoordinaatti X</param>
    /// <param name="y0">Talon alkukoordinaatti Y</param>
    /// <returns>Onko tilaa talolle</returns>
    public bool CheckBuilding(float[,] noiseMap, int x0, int y0)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        for (int x1 = 0; x1 < buildingSize; x1++)
        {
            for (int y1 = 0; y1 < buildingSize; y1++)
            {
                int x = x0 + x1;
                int y = y0 + y1;

                if (x < 0 || x >= width || y < 0 || y >= height)
                    return false;

                float value = noiseMap[x, y];
                if (forestMin >= value || value >= forestMax)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Rakentaa kohtaan neliön muotoisen talon
    /// </summary>
    /// <param name="colourMap">Kartan Väritaulukko</param>
    /// <param name="buildingColor">Rakennuksen väri</param>
    /// <param name="width">Värikartan leveys</param>
    /// <param name="x0">Talon alkukoordinaatti X</param>
    /// <param name="y0">Talon alkukoordinaaatti Y</param>
    public void BuildBuilding(Color[] colourMap, int width, int x0, int y0)
    {
        for (int x1 = 0; x1 < buildingSize; x1++)
        {
            for (int y1 = 0; y1 < buildingSize; y1++)
            {
                int x = x0 + x1;
                int y = y0 + y1;

                colourMap[y * width + x] = buildingColor;
            }
        }
    }
}

