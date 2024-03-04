using UnityEngine;

// Apuluokka v�rien alueiden sy�tt�miselle editorissa
[System.Serializable]
public struct ColorLevel
{
    public string name;
    // T�m�n arvon alla olevat, ja seuraavana alempaa arvoa ylev�t, pikselit v�ritet��n
    [Range(0, 1)] public float threshold;
    public Color color;
}