using System.Collections;
using UnityEngine;

//tampa do tubo do monstro: fechada fora da partida, abre no meio quando comeca
public class TampaTubo : MonoBehaviour
{
    public JogoMemoria jogo;
    //as duas metades da tampa
    public Transform[] dobradicas;
    //quanto cada metade anda para abrir (no espaco local dela)
    public Vector3 deslocamentoAberto = new Vector3(-2.95f, 0f, 0f);
    public float tempoAbrir = 1.4f;
    public float tempoFechar = 2.5f;
    //espera depois do fim de jogo antes de fechar (o monstro sobe antes)
    public float esperaParaFechar = 1f;

    [Header("Camera do jogador")]
    public bool forcarOlhar = true;
    public Transform alvoOlhar;
    //para onde a camera volta depois (texto do telao da frente)
    public Transform alvoDepois;
    public float tempoOlhandoCima = 2.3f;
    public float tempoVoltando = 0.9f;
    public float rapidezOlhar = 5f;

    [Header("Som")]
    public AudioSource som;
    public AudioClip somAbrir;
    //vazio = usa o mesmo som de abrir
    public AudioClip somFechar;
    [Range(0f, 3f)] public float volume = 1.5f;

    Vector3[] posFechadas;
    bool aberta;
    float abertura; //fechada = 0, aberta = 1
    Coroutine rotina;
    Renderer[] partesMonstro;

    //o jogo so recomeca com a tampa toda fechada
    public bool Fechada => !aberta && rotina == null;

    void Start()
    {
        if (jogo == null) jogo = FindAnyObjectByType<JogoMemoria>();
        posFechadas = new Vector3[dobradicas != null ? dobradicas.Length : 0];
        for (int i = 0; i < posFechadas.Length; i++) posFechadas[i] = dobradicas[i].localPosition;
        MonstroDescendo monstro = FindAnyObjectByType<MonstroDescendo>();
        if (monstro != null) partesMonstro = monstro.GetComponentsInChildren<Renderer>();
        //com a tampa fechada o monstro fica escondido para a mao nao vazar
        MostrarMonstro(false);
    }

    void MostrarMonstro(bool mostrar)
    {
        if (partesMonstro == null) return;
        foreach (Renderer r in partesMonstro) if (r != null) r.enabled = mostrar;
    }

    void Update()
    {
        if (jogo == null || posFechadas == null || posFechadas.Length == 0) return;
        bool deveAbrir = !jogo.ForaDaPartida;
        if (deveAbrir == aberta) return;

        aberta = deveAbrir;
        if (rotina != null) StopCoroutine(rotina);
        rotina = StartCoroutine(aberta ? Abrir() : Fechar());
    }

    IEnumerator Abrir()
    {
        MostrarMonstro(true);
        Tocar(somAbrir);
        if (forcarOlhar) StartCoroutine(OlharParaCima());
        //destrava com um tranco, para um instante e vai abrindo devagar
        yield return Mover(0.04f, 0.25f);
        yield return new WaitForSeconds(0.35f);
        yield return Mover(1f, tempoAbrir);
    }

    IEnumerator OlharParaCima()
    {
        ControleJogador controle = FindAnyObjectByType<ControleJogador>();
        if (controle == null) yield break;
        controle.OlharPara(alvoOlhar != null ? alvoOlhar : transform, tempoOlhandoCima, rapidezOlhar);
        yield return new WaitForSeconds(tempoOlhandoCima);
        if (alvoDepois != null) controle.OlharPara(alvoDepois, tempoVoltando, rapidezOlhar * 1.6f);
    }

    IEnumerator Fechar()
    {
        yield return new WaitForSeconds(esperaParaFechar);
        Tocar(somFechar != null ? somFechar : somAbrir);
        yield return Mover(0f, tempoFechar);
        MostrarMonstro(false);
        rotina = null;
    }

    IEnumerator Mover(float alvo, float duracao)
    {
        float inicio = abertura;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            Aplicar(Mathf.Lerp(inicio, alvo, Mathf.SmoothStep(0f, 1f, t / duracao)));
            yield return null;
        }
        Aplicar(alvo);
    }

    void Aplicar(float k)
    {
        abertura = k;
        for (int i = 0; i < posFechadas.Length; i++)
            if (dobradicas[i] != null)
                dobradicas[i].localPosition = posFechadas[i] + dobradicas[i].localRotation * deslocamentoAberto * k;
    }

    void Tocar(AudioClip clip)
    {
        if (som != null && clip != null) som.PlayOneShot(clip, volume);
    }
}
