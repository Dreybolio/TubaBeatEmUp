using System.Collections;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    [SerializeField] private float timeUntilDespawn = 10.0f;
    [SerializeField] private GameObject model;

    private void Start()
    {
        StartCoroutine(C_DespawnAfterTime());
    }
    public abstract void OnPickUp(GameObject player);
    private IEnumerator C_DespawnAfterTime()
    {
        yield return new WaitForSeconds(timeUntilDespawn);
        for (int i = 0; i < 5; i++)
        {
            model.SetActive(true);
            yield return new WaitForSeconds(0.25f);
            model.SetActive(false);
            yield return new WaitForSeconds(0.25f);
        }
        Destroy(gameObject);
    }
}
