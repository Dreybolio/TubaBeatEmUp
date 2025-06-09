using System;
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class CameraSkybox : MonoBehaviour
{
    [SerializeField] private SkyboxLayer[] skyboxLayers;

    private Vector3 lastCamPos;
    private void Start()
    {
        if (Camera.main != null)
        {
            lastCamPos = Camera.main.transform.position;
        }
    }

    private void Update()
    {
        if (Camera.main == null) return;

        Vector3 camPos = Camera.main.transform.position;
        Vector3 delta = camPos - lastCamPos;
        foreach (SkyboxLayer layer in skyboxLayers)
        {
            if (layer.image == null) continue;
            float newX = layer.image.uvRect.position.x + (delta.x * layer.scrollFactor);
            if (newX < 0) newX += 1;
            else if (newX > 1) newX -= 1;

            newX = Mathf.Round(newX * 1000f) / 1000f;

            layer.image.uvRect = new Rect(new Vector2(newX, 0), layer.image.uvRect.size);
        }

        lastCamPos = camPos;
    }
}

[Serializable]
public struct SkyboxLayer
{
    public RawImage image;
    public float scrollFactor;
}
