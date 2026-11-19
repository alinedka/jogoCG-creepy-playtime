using System.Collections;
using UnityEngine;

//mao que voa ate a mira presa num cabo, aperta e volta
public class MaoEsticavel : MonoBehaviour
{
    [Header("Peças (arraste da Hierarchy)")]
    public Transform mao;
    public Transform origem;
    public Transform cabo;

    [Header("Movimento")]
    public float velocidadeIda = 9f;
    public float velocidadeVolta = 7f;
    public float pausaNoAlvo = 0.1f;
    public float espessuraCabo = 0.015f;

    [Header("Som (opcional)")]
    public AudioSource somDisparo;
    [Min(0f)] public float inicioSom = 0f;
    [Min(0f)] public float duracaoSom = 0f;

    Vector3 posicaoRepouso;
    Quaternion rotacaoRepouso;
    bool ocupada;

    //mao no ar, nao pode disparar de novo
    public bool Ocupada => ocupada;

    void Start()
    {
        if (mao != null)
        {
            //posicao "de descanso" da mao, relativa a camera
            posicaoRepouso = mao.localPosition;
            rotacaoRepouso = mao.localRotation;
        }
        AtualizarCabo();
    }

    //lanca a mao ate o alvo e chama aoChegar quando ela chega
    public bool Disparar(Vector3 alvo, System.Action aoChegar) => Disparar(alvo, aoChegar, null);

    //o alvo acompanha o objeto "seguir" enquanto a mao voa
    public bool Disparar(Vector3 alvo, System.Action aoChegar, Transform seguir)
    {
        if (ocupada || mao == null) return false;
        StartCoroutine(RotinaDisparo(alvo, aoChegar, seguir));
        return true;
    }

    IEnumerator RotinaDisparo(Vector3 alvoInicial, System.Action aoChegar, Transform seguir)
    {
        Vector3 alvo = alvoInicial;
        Vector3 noObjeto = seguir != null ? seguir.InverseTransformPoint(alvoInicial) : Vector3.zero;
        ocupada = true;
        float volumeOriginal = somDisparo != null ? somDisparo.volume : 1f;
        if (somDisparo != null && somDisparo.clip != null)
        {
            somDisparo.Stop();
            somDisparo.volume = volumeOriginal;
            //comeca no trecho escolhido do arquivo (se o arquivo for comprido)
            somDisparo.time = Mathf.Clamp(inicioSom, 0f, Mathf.Max(0f, somDisparo.clip.length - 0.05f));
            somDisparo.Play();
            if (duracaoSom > 0f) StartCoroutine(PararSom(duracaoSom, volumeOriginal));
        }

        //ida: anda em linha reta ate o alvo
        while (true)
        {
            if (seguir != null) alvo = seguir.TransformPoint(noObjeto); //o alvo se mexe junto com o objeto
            if ((mao.position - alvo).sqrMagnitude <= 0.0001f) break;
            mao.position = Vector3.MoveTowards(mao.position, alvo, velocidadeIda * Time.deltaTime);
            yield return null;
        }

        aoChegar?.Invoke();
        //fica "grudada" no ponto durante a pausa (se o objeto se mexer, a mao vai junto)
        for (float t = 0f; t < pausaNoAlvo; t += Time.deltaTime)
        {
            if (seguir != null) mao.position = seguir.TransformPoint(noObjeto);
            yield return null;
        }

        //volta para o canto da tela (posicao da camera)
        while ((mao.localPosition - posicaoRepouso).sqrMagnitude > 0.0001f)
        {
            mao.localPosition = Vector3.MoveTowards(mao.localPosition, posicaoRepouso, velocidadeVolta * Time.deltaTime);
            yield return null;
        }

        mao.localPosition = posicaoRepouso;
        mao.localRotation = rotacaoRepouso;
        ocupada = false;

        //sem duracao definida: o som acaba quando a mao volta
        if (duracaoSom <= 0f && somDisparo != null && somDisparo.isPlaying)
            StartCoroutine(PararSom(0f, volumeOriginal));
    }

    //some com o som da mao depois de um tempo
    IEnumerator PararSom(float depois, float volumeOriginal)
    {
        if (depois > 0f) yield return new WaitForSeconds(depois);
        const float fade = 0.1f;
        for (float t = 0f; t < fade && somDisparo.isPlaying; t += Time.deltaTime)
        {
            somDisparo.volume = Mathf.Lerp(volumeOriginal, 0f, t / fade);
            yield return null;
        }
        somDisparo.Stop();
        somDisparo.volume = volumeOriginal;
    }

    //lateUpdate: roda depois da camera mexer, entao o cabo nunca fica "atrasado"
    void LateUpdate()
    {
        AtualizarCabo();
    }

    //estica o cabo entre o punho e a mao
    void AtualizarCabo()
    {
        if (cabo == null || origem == null || mao == null) return;

        Vector3 a = origem.position;
        Vector3 b = mao.position;
        Vector3 direcao = b - a;
        float comprimento = direcao.magnitude;

        //com a mao em casa o cabo fica escondido
        bool visivel = ocupada && comprimento > 0.05f;
        if (cabo.gameObject.activeSelf != visivel) cabo.gameObject.SetActive(visivel);
        if (!visivel) return;

        //o Cylinder do Unity tem 2 de altura no eixo Y: escala Y = metade do comprimento
        cabo.position = (a + b) * 0.5f;
        cabo.rotation = Quaternion.FromToRotation(Vector3.up, direcao);
        Vector3 escalaPai = cabo.parent != null ? cabo.parent.lossyScale : Vector3.one;
        cabo.localScale = new Vector3(espessuraCabo / escalaPai.x, comprimento * 0.5f / escalaPai.y, espessuraCabo / escalaPai.z);
    }
}
