using UnityEngine;

// Apuluokka värien alueiden syöttämiselle editorissa
[System.Serializable]
public struct ColorLevel
{
    public string name;
    // Tämän arvon alla olevat, ja seuraavana alempaa arvoa ylevät, pikselit väritetään
    [Range(0, 1)] public float threshold;
    public Color color;
}