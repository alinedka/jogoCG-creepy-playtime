using System.Collections;
using UnityEngine;

//easter egg: maquina de garra, 1o clique a garra anda, 2o clique ela desce e tenta pegar um bichinho
public class MaquinaGarra : MonoBehaviour
{
    [Header("Peças da máquina (filhos do prefab)")]
    public Transform carrinho;
    public Transform pistaoCima;
    public Transform pistaoBaixo;
    public Transform[] dedos;

    [Header("Movimento")]
    public float limiteCarrinho = 0.36f;
    public float velocidadeCarrinho = 0.45f;
    public float descida = 0.3f;
    public float velocidadeDescida = 0.5f;
    public float anguloFechar = 25f;
    public float xPremio = 0.3f;
    public Vector3 fundoBuraco = new Vector3(0.3f, 1.1f, 0.33f);

    [Header("Bichinhos")]
    public Transform[] bichinhos;
    public float tolerancia = 0.09f;
    [Range(0f, 1f)] public float chancePegar = 0.75f;
    public Transform saidaPremio;
    public Transform lugarTrofeu;
    public float escalaTrofeu = 1.1f;

    [Header("Sons (do pacote Claw Machine)")]
    public AudioSource fonte;
    public AudioClip somMovendo;
    public AudioClip somDescer;
    public AudioClip somPegou;
    public AudioClip somSoltou;
    public AudioClip somPerdeu;

    [Header("Jogo")]
    public JogoMemoria jogo;

    enum Estado { Parada, Andando, Ocupada }
    Estado estado = Estado.Parada;
    float x; //posicao atual do carrinho
    float sentido = 1f; //para a direita = 1, para a esquerda = -1
    Vector3 cimaInicial, baixoInicial;
    Quaternion[] dedosIniciais;

    public bool Ocupada => estado == Estado.Ocupada;

    void Start()
    {
        if (jogo == null) jogo = FindAnyObjectByType<JogoMemoria>();
        if (carrinho != null) x = carrinho.localPosition.x;
        if (pistaoCima != null) cimaInicial = pistaoCima.localPosition;
        if (pistaoBaixo != null) baixoInicial = pistaoBaixo.localPosition;
        dedosIniciais = new Quaternion[dedos != null ? dedos.Length : 0];
        for (int i = 0; i < dedosIniciais.Length; i++) dedosIniciais[i] = dedos[i].localRotation;

        //bichinhos ja ganhos em outras partidas ficam na plataforma
        foreach (Transform b in bichinhos)
        {
            if (b == null) continue;
            int i = Conquistas.Indice(b.name);
            if (i >= 0) ColocarNoTrofeu(b, i);
        }
    }

    //a mao chegou na maquina
    public void Clicar()
    {
        if (estado == Estado.Parada)
        {
            estado = Estado.Andando;
            TocarLoop(somMovendo);
        }
        else if (estado == Estado.Andando)
        {
            StartCoroutine(Jogada());
        }
    }

    void Update()
    {
        if (estado != Estado.Andando || carrinho == null) return;

        //se a partida comecou, a garra para onde esta
        if (jogo != null && !jogo.ForaDaPartida)
        {
            estado = Estado.Parada;
            PararLoop();
            return;
        }

        x += sentido * velocidadeCarrinho * Time.deltaTime;
        if (x > limiteCarrinho) { x = limiteCarrinho; sentido = -1f; }
        if (x < -limiteCarrinho) { x = -limiteCarrinho; sentido = 1f; }
        PosicionarCarrinho(x);
    }

    IEnumerator Jogada()
    {
        estado = Estado.Ocupada;
        PararLoop();
        Tocar(somDescer);

        //desce, fecha os dedos
        yield return MoverPistao(1f);
        Transform alvo = BichinhoEmbaixo();
        bool pegou = alvo != null && Random.value < chancePegar;
        yield return MoverDedos(1f);
        if (pegou)
        {
            alvo.SetParent(pistaoBaixo, true);
            Tocar(somPegou);
        }

        //sobe
        yield return MoverPistao(0f);

        if (!pegou)
        {
            Tocar(somPerdeu);
            yield return MoverDedos(0f);
            estado = Estado.Parada;
            yield break;
        }

        //leva ate o buraco do premio e solta
        TocarLoop(somMovendo);
        while (!Mathf.Approximately(x, xPremio))
        {
            x = Mathf.MoveTowards(x, xPremio, velocidadeCarrinho * Time.deltaTime);
            PosicionarCarrinho(x);
            yield return null;
        }
        PararLoop();
        yield return MoverDedos(0f);
        alvo.SetParent(transform, true);
        yield return Voar(alvo, transform.TransformPoint(fundoBuraco), alvo.rotation, alvo.localScale, 0.35f, 0f);
        Tocar(somSoltou);
        yield return new WaitForSeconds(0.4f);

        //sai pela porta do premio e vai para a plataforma
        Vector3 porta = saidaPremio != null
            ? saidaPremio.position + transform.forward * 0.15f
            : transform.TransformPoint(new Vector3(xPremio, 0.7f, 0.65f));
        alvo.position = porta;
        yield return new WaitForSeconds(0.3f);

        bool primeiro = Conquistas.Adicionar(alvo.name);
        int indice = Conquistas.Indice(alvo.name);
        Vector3 destino; Quaternion rotDestino;
        PosicaoTrofeu(indice, out destino, out rotDestino);
        yield return Voar(alvo, destino, rotDestino, Vector3.one * escalaTrofeu, 1.2f, 1.5f);
        ColocarNoTrofeu(alvo, indice);

        if (jogo != null)
        {
            int total = Conquistas.Quantidade;
            jogo.MostrarConquista(primeiro
                ? "CONQUISTA DESBLOQUEADA!\nCaçador de bichinhos"
                : "+1 BICHINHO\n" + total + " na coleção");
        }

        estado = Estado.Parada;
    }

    //------------------------------------------------------------------

    void PosicionarCarrinho(float novoX)
    {
        Vector3 p = carrinho.localPosition;
        p.x = novoX;
        carrinho.localPosition = p;
    }

    //desce (1) ou sobe (0) o pistao
    IEnumerator MoverPistao(float alvo)
    {
        if (pistaoCima == null || pistaoBaixo == null) yield break;
        float inicio = (cimaInicial.y - pistaoCima.localPosition.y) / (descida * 0.5f);
        float duracao = Mathf.Abs(alvo - inicio) * descida / Mathf.Max(0.01f, velocidadeDescida);
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = Mathf.Lerp(inicio, alvo, t / duracao);
            pistaoCima.localPosition = cimaInicial + Vector3.down * descida * 0.5f * k;
            pistaoBaixo.localPosition = baixoInicial + Vector3.down * descida * 0.5f * k;
            yield return null;
        }
        pistaoCima.localPosition = cimaInicial + Vector3.down * descida * 0.5f * alvo;
        pistaoBaixo.localPosition = baixoInicial + Vector3.down * descida * 0.5f * alvo;
    }

    //fecha (1) ou abre (0) os dedos
    IEnumerator MoverDedos(float alvo)
    {
        for (float t = 0f; t < 0.25f; t += Time.deltaTime)
        {
            AplicarDedos(Mathf.Lerp(1f - alvo, alvo, t / 0.25f));
            yield return null;
        }
        AplicarDedos(alvo);
    }

    void AplicarDedos(float k)
    {
        for (int i = 0; i < dedosIniciais.Length; i++)
            if (dedos[i] != null)
                dedos[i].localRotation = dedosIniciais[i] * Quaternion.Euler(anguloFechar * k, 0f, 0f);
    }

    //bichinho mais perto da garra, se estiver perto o bastante
    Transform BichinhoEmbaixo()
    {
        Transform melhor = null;
        float melhorDist = tolerancia;
        Vector3 garra = transform.InverseTransformPoint(pistaoBaixo.position);
        foreach (Transform b in bichinhos)
        {
            if (b == null || b.parent != transform) continue; //ja foi pego
            Vector3 p = transform.InverseTransformPoint(b.position);
            float d = new Vector2(p.x - garra.x, p.z - garra.z).magnitude;
            if (d <= melhorDist) { melhor = b; melhorDist = d; }
        }
        return melhor;
    }

    //leva o bichinho em arco ate o destino
    IEnumerator Voar(Transform obj, Vector3 destino, Quaternion rotDestino, Vector3 escalaDestino, float duracao, float arco)
    {
        Vector3 p0 = obj.position; Quaternion r0 = obj.rotation; Vector3 e0 = obj.localScale;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duracao);
            obj.position = Vector3.Lerp(p0, destino, k) + Vector3.up * arco * Mathf.Sin(k * Mathf.PI);
            obj.rotation = Quaternion.Slerp(r0, rotDestino, k);
            obj.localScale = Vector3.Lerp(e0, escalaDestino, k);
            yield return null;
        }
        obj.position = destino; obj.rotation = rotDestino; obj.localScale = escalaDestino;
    }

    //lugar do bichinho ganho na plataforma (filhos Lugar1, Lugar2)
    void PosicaoTrofeu(int indice, out Vector3 pos, out Quaternion rot)
    {
        if (lugarTrofeu == null) { pos = transform.position; rot = transform.rotation; return; }
        int lugares = 0;
        foreach (Transform c in lugarTrofeu) if (c.name.StartsWith("Lugar")) lugares++;
        if (indice >= 0 && indice < lugares)
        {
            Transform lugar = lugarTrofeu.Find("Lugar" + (indice + 1));
            if (lugar != null) { pos = lugar.position; rot = lugar.rotation; return; }
        }
        int coluna = Mathf.Max(0, indice) % 5, linha = Mathf.Max(0, indice) / 5;
        pos = lugarTrofeu.position + lugarTrofeu.right * (coluna * 0.38f) - lugarTrofeu.forward * (linha * 0.4f);
        rot = lugarTrofeu.rotation;
    }

    void ColocarNoTrofeu(Transform b, int indice)
    {
        PosicaoTrofeu(indice, out Vector3 pos, out Quaternion rot);
        b.SetParent(lugarTrofeu != null ? lugarTrofeu : null, true);
        b.position = pos; b.rotation = rot;
        b.localScale = Vector3.one * escalaTrofeu; //lugarTrofeu tem escala 1
    }

    void Tocar(AudioClip som)
    {
        if (fonte != null && som != null) fonte.PlayOneShot(som);
    }

    void TocarLoop(AudioClip som)
    {
        if (fonte == null || som == null) return;
        fonte.clip = som; fonte.loop = true; fonte.Play();
    }

    void PararLoop()
    {
        if (fonte != null && fonte.loop) { fonte.Stop(); fonte.loop = false; }
    }
}
