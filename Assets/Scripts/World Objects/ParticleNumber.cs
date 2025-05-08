using TMPro;
using UnityEngine;

public class ParticleNumber : ParticleObject
{
    [SerializeField] private TextMeshPro textMesh;
    public void SetText(string text)
    {
        textMesh.text = text;
    }
}
