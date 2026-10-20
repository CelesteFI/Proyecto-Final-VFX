using System.Collections;
using UnityEngine;

public class TemporarySummon : MonoBehaviour
{
    public float lifeTime = 5.0f;

    void Start()
    {
        StartCoroutine(DestruirConEfecto());
    }

    private IEnumerator DestruirConEfecto()
    {
        yield return new WaitForSeconds(lifeTime);

        // Encoger suavemente antes de desaparecer
        Vector3 escalaInicial = transform.localScale;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaInicial, Vector3.zero, t / 0.4f);
            yield return null;
        }

        Destroy(gameObject);
    }
}