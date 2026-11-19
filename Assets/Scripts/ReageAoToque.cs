using System.Collections;
using UnityEngine;

//brinquedo balanca e amassa quando a mao encosta
public class ReageAoToque : MonoBehaviour
{
    public float inclinacao = 18f;
    public float amassar = 0.15f;
    public float duracao = 0.7f;
    public AudioSource som;
    public AudioClip[] sonsApertar;
    [Range(0f, 3f)] public float volume = 1f;

    [Header("Voz macabra (bichinhos ganhos na máquina)")]
    public AudioClip[] vozes;
    public AudioSource fonteVoz;
    [Range(0f, 1f)] public float chanceVoz = 1f;
    public float atrasoVoz = 0.35f;
    [Range(0f, 3f)] public float volumeVoz = 1.5f;
    public Vector2 tomVoz = new Vector2(0.75f, 0.9f);
    float falandoAte;
    public Vector2 variacaoTom = new Vector2(0.9f, 1.2f);

    Quaternion rotOriginal;
    Vector3 escalaOriginal;
    bool balancando;

    public void Tocar(Vector3 deOnde)
    {
        AudioClip clip = (sonsApertar != null && sonsApertar.Length > 0)
            ? sonsApertar[Random.Range(0, sonsApertar.Length)]
            : (som != null ? som.clip : null);
        if (som != null && clip != null)
        {
            som.pitch = Random.Range(variacaoTom.x, variacaoTom.y);
            som.PlayOneShot(clip, volume);
        }
        //fala (sem uma fala atropelar a outra)
        if (vozes != null && vozes.Length > 0 && som != null && Time.time >= falandoAte && Random.value < chanceVoz)
        {
            AudioClip voz = vozes[Random.Range(0, vozes.Length)];
            if (voz != null)
            {
                falandoAte = Time.time + atrasoVoz + voz.length;
                StartCoroutine(Falar(voz));
            }
        }

        if (balancando) return;
        StartCoroutine(Balancar(deOnde));
    }

    IEnumerator Falar(AudioClip voz)
    {
        yield return new WaitForSeconds(atrasoVoz);
        AudioSource fonte = fonteVoz != null ? fonteVoz : som;
        //o pitch do AudioSource vale para tudo que ele toca: a voz usa o seu proprio tom
        fonte.pitch = Random.Range(tomVoz.x, tomVoz.y);
        fonte.PlayOneShot(voz, volumeVoz);
    }

    IEnumerator Balancar(Vector3 deOnde)
    {
        balancando = true;
        rotOriginal = transform.localRotation;
        escalaOriginal = transform.localScale;

        //inclina para longe de quem tocou
        Vector3 empurrao = transform.position - deOnde; empurrao.y = 0f;
        Vector3 eixo = Vector3.Cross(Vector3.up, empurrao.sqrMagnitude > 0.0001f ? empurrao.normalized : transform.forward);
        Vector3 eixoLocal = transform.parent != null ? transform.parent.InverseTransformDirection(eixo) : eixo;

        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = t / duracao;
            float onda = Mathf.Sin(k * Mathf.PI * 3f) * (1f - k); //vai e volta, cada vez menos
            transform.localRotation = Quaternion.AngleAxis(inclinacao * onda, eixoLocal) * rotOriginal;
            float a = 1f - amassar * Mathf.Abs(onda);
            transform.localScale = new Vector3(escalaOriginal.x / Mathf.Sqrt(a), escalaOriginal.y * a, escalaOriginal.z / Mathf.Sqrt(a));
            yield return null;
        }
        transform.localRotation = rotOriginal;
        transform.localScale = escalaOriginal;
        balancando = false;
    }
}
