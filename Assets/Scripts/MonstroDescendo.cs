using System.Collections;
using UnityEngine;

//monstro no tubo: desce com o tempo, sobe com os acertos, da o bote no erro e o jumpscare no fim
public class MonstroDescendo : MonoBehaviour
{
    public JogoMemoria jogo;

    [Header("Descida")]
    public float distanciaDescida = 3.3f;
    public float tempoParaDescer = 12f;
    public float aceleracaoPorRodada = 0.08f;
    public float tempoMinimo = 6f;
    public float tempoPorCor = 0.8f;

    [Header("Alívio")]
    public float recuoPorAcerto = 0.12f;
    public float recuoPorRodada = 0.5f;

    [Header("Erros")]
    [Range(0f, 1f)] public float descidaPorErro = 0.3f;
    public float rapidezDescida = 1.2f;
    public float distanciaBoteErro = 0.6f;
    public float duracaoBoteErro = 0.08f;
    public float rapidezBote = 10f;
    [Range(0f, 3f)] public float volumeBote = 0.6f;

    [Header("Ataque final (fim de jogo)")]
    public float distanciaBote = 0.6f;
    public float rapidezAtaqueFinal = 18f;

    [Header("Balanço")]
    public float balancoGraus = 4f;
    public float velocidadeBalanco = 1.3f;

    [Header("Avisos (opcionais)")]
    public Light luzPerigo;
    public float intensidadeMaxima = 6f;
    public AudioSource somAproximando;
    public AudioSource somBote;

    [Header("Animação (opcional)")]
    public Animator animador;
    public bool forcarOlharNoBote = true;
    public float tempoOlharBote = 1.8f;
    public float rapidezOlharBote = 14f;
    public float atrasoBote = 0.5f;
    ControleJogador controle;
    public bool patadaNoBote = true;
    public float duracaoPatada = 0.6f;
    public float aberturaPatada = 0.5f;
    public bool patadaUmaMao = true;
    [Range(0.05f, 0.9f)] public float picoPatada = 0.15f;
    public string gatilhoAtaque = "Ataque";
    public string estadoParado = "Parado";

    [Header("Susto final (jumpscare no fim de jogo)")]
    public Transform modelo;
    public Transform pontoDoRosto;
    public Transform cameraJogador;
    public float distanciaSusto = 0.45f;
    public float ajusteAlturaSusto = -0.06f;
    public float duracaoAvanco = 0.1f;
    public float duracaoSusto = 1.6f;
    public float tremorCamera = 0.05f;
    public AudioSource somGrito;
    [Range(1f, 3f)] public float volumeGrito = 1.8f;
    public float duracaoGrito = 2f;
    public float sumicoGrito = 0.5f;

    public bool manterCentralizado = true;
    public float alturaRostoNoTubo = 0.6f;
    public Transform pontoCentro;

    [Header("Olhar para o jogador")]
    public bool olharParaJogador = true;
    public Transform ossoCabeca;
    public Transform ossoPescoco;
    [Range(0f, 1f)] public float parteDoPescoco = 0.4f;
    [Range(0f, 1f)] public float forcaOlhar = 1f;

    //tempo do susto antes do fim de jogo
    public float DuracaoSusto => (modelo != null && pontoDoRosto != null) ? duracaoSusto : 0f;

    float perigo; //parte do TEMPO: 0 = ainda tem tempo, 1 = chegou no jogador
    float nivelErros; //parte dos ERROS: 0 = nenhum erro, cada erro soma "descidaPorErro"
    float descidaAtual; //quantos metros ja desceu (suavizado)
    bool ataqueFinal;
    bool emSusto; //durante o jumpscare o Update nao mexe no monstro
    bool emBote; //durante o bote do erro ele chega rapido na nova altura
    float extraBote; //metros a mais da investida do bote
    Vector3 rostoLocalInicial; //onde o rosto fica (em relacao a este objeto): no eixo do tubo
    //susto: o modelo e movido para o rosto seguir este alvo a cada frame (depois da animacao)
    bool seguirSusto;
    Vector3 alvoSusto;
    Quaternion rotSusto;
    Vector3 rostoFrenteLocal = Vector3.forward;
    Vector3 rostoCimaLocal = Vector3.up;
    Vector3 modeloPosOriginal;
    Quaternion modeloRotOriginal;
    Vector3 posicaoInicial;
    Quaternion rotacaoInicial;

    void Start()
    {
        if (jogo == null)
            jogo = FindAnyObjectByType<JogoMemoria>();

        posicaoInicial = transform.localPosition;
        rotacaoInicial = transform.localRotation;
        //o rosto fica no eixo deste objeto (centro do tubo), um pouco abaixo dele
        rostoLocalInicial = transform.InverseTransformPoint(transform.position + Vector3.down * alturaRostoNoTubo);
        if (modelo != null)
        {
            modeloPosOriginal = modelo.localPosition;
            modeloRotOriginal = modelo.localRotation;
            //direcoes do rosto no osso da cabeca (o modelo olha para +Z, com o topo da cabeca em +Y)
            if (pontoDoRosto != null)
            {
                rostoFrenteLocal = pontoDoRosto.InverseTransformDirection(modelo.forward);
                rostoCimaLocal = pontoDoRosto.InverseTransformDirection(modelo.up);
            }
        }
    }

    //depois da animacao: mantem o monstro centralizado no tubo (ou com o rosto na camera no susto)
    void LateUpdate()
    {
        if (modelo == null || pontoDoRosto == null) return;

        if (emSusto)
        {
            if (!seguirSusto) return;
            //gira o modelo para o rosto ficar virado para o jogador
            Vector3 frente = pontoDoRosto.TransformDirection(rostoFrenteLocal);
            Vector3 cima = pontoDoRosto.TransformDirection(rostoCimaLocal);
            Quaternion atual = Quaternion.LookRotation(frente, cima);
            Quaternion desejada = Quaternion.LookRotation(rotSusto * Vector3.forward, rotSusto * Vector3.up);
            modelo.rotation = (desejada * Quaternion.Inverse(atual)) * modelo.rotation;
            modelo.position += alvoSusto - pontoDoRosto.position;
            return;
        }

        if (olharParaJogador) OlharParaCamera();
        AplicarPatada();

        if (!manterCentralizado) return;
        Transform centro = pontoCentro != null ? pontoCentro : pontoDoRosto;
        Vector3 desejado = transform.TransformPoint(rostoLocalInicial);
        modelo.position += desejado - centro.position;
    }

    float tempoPatada = -1f; //< 0 = sem patada
    bool patadaComEsquerda;

    //patada: estica os bracos ate o jogador e volta (por cima da animacao)
    void AplicarPatada()
    {
        if (tempoPatada < 0f) return;
        tempoPatada += Time.deltaTime;
        float u = tempoPatada / Mathf.Max(0.05f, duracaoPatada);
        if (u >= 1f) { tempoPatada = -1f; return; }
        if (animador == null || !animador.isHuman) return;
        Transform cam = cameraJogador != null ? cameraJogador : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) return;

        //estica de uma vez (ate o "pico") e volta devagar
        float w = u < picoPatada
            ? Mathf.Sin(u / picoPatada * Mathf.PI * 0.5f) //ida explosiva
            : Mathf.SmoothStep(0f, 1f, 1f - (u - picoPatada) / (1f - picoPatada)); //volta suave

        bool esquerda = !patadaUmaMao || patadaComEsquerda;
        bool direita = !patadaUmaMao || !patadaComEsquerda;
        if (esquerda) EsticarBraco(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, cam, w);
        if (direita) EsticarBraco(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, cam, w);
    }

    void EsticarBraco(HumanBodyBones cima, HumanBodyBones meio, HumanBodyBones mao, Transform cam, float w)
    {
        Transform a = animador.GetBoneTransform(cima);
        Transform b = animador.GetBoneTransform(meio);
        Transform h = animador.GetBoneTransform(mao);
        if (a == null || b == null || h == null) return;

        //cada mao mira um pouco para o lado dela, em volta da cabeca do jogador
        Vector3 lado = a.position - pontoDoRosto.position;
        lado.y = 0f;
        Vector3 alvo = cam.position + (lado.sqrMagnitude > 0.0001f ? lado.normalized * aberturaPatada : Vector3.zero);

        a.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(b.position - a.position, alvo - a.position), w) * a.rotation;
        b.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(h.position - b.position, alvo - b.position), w) * b.rotation;
    }

    //gira pescoco e cabeca (depois da animacao) para o rosto apontar para a camera
    void OlharParaCamera()
    {
        Transform cam = cameraJogador != null ? cameraJogador : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) return;
        Transform cabeca = ossoCabeca != null ? ossoCabeca : pontoDoRosto.parent;
        if (cabeca == null) return;
        Transform pescoco = ossoPescoco != null ? ossoPescoco : cabeca.parent;

        if (pescoco != null && parteDoPescoco > 0f)
            pescoco.rotation = Quaternion.Slerp(Quaternion.identity, GiroAteCamera(cam), parteDoPescoco * forcaOlhar) * pescoco.rotation;
        cabeca.rotation = Quaternion.Slerp(Quaternion.identity, GiroAteCamera(cam), forcaOlhar) * cabeca.rotation;
    }

    //giro (no mundo) que leva a direcao do rosto ate a direcao da camera
    Quaternion GiroAteCamera(Transform cam)
    {
        Vector3 frente = pontoDoRosto.TransformDirection(rostoFrenteLocal);
        Vector3 paraCamera = cam.position - pontoDoRosto.position;
        return Quaternion.FromToRotation(frente, paraCamera);
    }

    void Update()
    {
        if (emSusto) return; //a rotina do susto controla o monstro

        //so desce enquanto o jogador esta respondendo
        if (jogo != null && jogo.EsperandoJogador && !ataqueFinal)
        {
            perigo += Time.deltaTime / TempoDaRodada();
            if (perigo >= 1f)
            {
                perigo = 1f;
                jogo.TempoEsgotado(); //conta como erro; o jogo chama Errou()
            }
        }

        //os erros travam parte da descida e o tempo usa o resto
        float alvo;
        if (ataqueFinal)
            alvo = distanciaDescida + distanciaBote;
        else
        {
            float porTempo = Mathf.Pow(perigo, 1.5f); //curva: devagar no comeco, rapido no final
            alvo = (nivelErros + (1f - nivelErros) * porTempo) * distanciaDescida;
        }

        //lerp exponencial: chega suave no alvo (devagar normalmente, rapido so no susto final)
        float rapidez = ataqueFinal ? rapidezAtaqueFinal : (emBote ? rapidezBote : rapidezDescida);
        descidaAtual = Mathf.Lerp(descidaAtual, alvo, 1f - Mathf.Exp(-rapidez * Time.deltaTime));
        transform.localPosition = posicaoInicial + Vector3.down * (descidaAtual + extraBote);

        //balanca pendurado, mais forte quanto maior o perigo
        float t = Time.time * velocidadeBalanco;
        float forca = 0.3f + perigo;
        transform.localRotation = rotacaoInicial * Quaternion.Euler(
            Mathf.Sin(t * 0.8f) * balancoGraus * 0.5f * forca,
            0f,
            Mathf.Sin(t) * balancoGraus * forca);

        //avisos: luz e som crescem com a proximidade
        float proximidade = Mathf.Clamp01(descidaAtual / Mathf.Max(0.01f, distanciaDescida));
        if (luzPerigo != null)
            luzPerigo.intensity = proximidade * proximidade * intensidadeMaxima;
        if (somAproximando != null)
        {
            somAproximando.volume = proximidade;
            somAproximando.pitch = 0.8f + proximidade * 0.6f;
        }
    }

    float TempoDaRodada()
    {
        int rodada = jogo != null ? Mathf.Max(1, jogo.Rodada) : 1;
        float tempo = tempoParaDescer * Mathf.Pow(1f - aceleracaoPorRodada, rodada - 1);
        int cores = jogo != null ? jogo.TamanhoSequencia : 0;
        return Mathf.Max(tempoMinimo, tempo) + tempoPorCor * cores;
    }

    //chamados pelo JogoMemoria

    public void Acertou()
    {
        perigo = Mathf.Max(0f, perigo - recuoPorAcerto);
    }

    public void RodadaCompleta()
    {
        perigo = Mathf.Max(0f, perigo - recuoPorRodada);
    }

    public void Reiniciar()
    {
        tempoPatada = -1f;
        if (emSusto)
        {
            StopAllCoroutines();
            emSusto = false;
            seguirSusto = false;
            transform.localPosition = posicaoInicial;
            transform.localRotation = rotacaoInicial;
            if (modelo != null) { modelo.localPosition = modeloPosOriginal; modelo.localRotation = modeloRotOriginal; }
        }
        ataqueFinal = false;
        perigo = 0f;
        nivelErros = 0f;
        emBote = false;
        extraBote = 0f;
        if (animador != null)
        {
            animador.ResetTrigger(gatilhoAtaque);
            animador.Play(estadoParado, 0, 0f);
        }
    }

    //o jogador errou ou demorou: o monstro fica mais baixo
    public void Errou()
    {
        if (ataqueFinal) return;
        perigo = 0f; //o relogio da demora recomeca

        //primeiro a camera vira para o monstro
        bool vaiOlhar = false;
        if (forcarOlharNoBote && pontoDoRosto != null)
        {
            if (controle == null) controle = FindAnyObjectByType<ControleJogador>();
            if (controle != null) { controle.OlharPara(pontoDoRosto, tempoOlharBote, rapidezOlharBote); vaiOlhar = true; }
        }
        //depois ele da o bote
        StartCoroutine(RotinaEsperarEBote(vaiOlhar ? atrasoBote : 0f));
    }

    //tempo entre o erro e o bote (a camera vira antes)
    public float AtrasoBote => (forcarOlharNoBote && pontoDoRosto != null) ? atrasoBote : 0f;

    IEnumerator RotinaEsperarEBote(float espera)
    {
        if (espera > 0f) yield return new WaitForSeconds(espera);
        if (ataqueFinal) yield break;
        DarBote();
    }

    void DarBote()
    {
        nivelErros = Mathf.Clamp01(nivelErros + descidaPorErro);
        if (somBote != null && somBote.clip != null) somBote.PlayOneShot(somBote.clip, volumeBote);
        if (patadaNoBote)
        {
            tempoPatada = 0f;
            patadaComEsquerda = Random.value < 0.5f;
        }
        StartCoroutine(RotinaBoteErro());
    }

    //bote: desce rapido e volta para a nova altura
    IEnumerator RotinaBoteErro()
    {
        emBote = true;
        for (float t = 0f; t < duracaoBoteErro; t += Time.deltaTime)
        {
            float k = t / duracaoBoteErro;
            extraBote = k * k * distanciaBoteErro; //acelera
            yield return null;
        }
        extraBote = distanciaBoteErro;
        const float volta = 0.35f;
        for (float t = 0f; t < volta; t += Time.deltaTime)
        {
            extraBote = Mathf.Lerp(distanciaBoteErro, 0f, t / volta);
            yield return null;
        }
        extraBote = 0f;
        emBote = false;
    }

    //faz o grito sumir aos poucos
    public void PararGrito(float duracao)
    {
        AudioSource grito = somGrito != null ? somGrito : somBote;
        if (grito == null || !grito.isPlaying) return;
        if (rotinaGrito != null) StopCoroutine(rotinaGrito);
        rotinaGrito = StartCoroutine(SumirSom(grito, duracao));
    }

    Coroutine rotinaGrito;
    float volumeOriginalGrito = -1f;

    IEnumerator SumirSom(AudioSource fonte, float duracao)
    {
        if (volumeOriginalGrito < 0f) volumeOriginalGrito = fonte.volume;
        float inicio = fonte.volume;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            //curva: cai mais no comeco e vai "espairando" bem devagar ate sumir (sem corte seco)
            float resta = 1f - t / duracao;
            fonte.volume = inicio * resta * resta;
            yield return null;
        }
        fonte.Stop();
        fonte.volume = volumeOriginalGrito;
        rotinaGrito = null;
    }

    //deixa o grito tocar "duracaoGrito" segundos e depois some com ele
    IEnumerator RotinaCortarGrito()
    {
        yield return new WaitForSeconds(duracaoGrito);
        PararGrito(sumicoGrito);
    }

    public void AtaqueFinal()
    {
        ataqueFinal = true;
        if (animador != null) animador.SetTrigger(gatilhoAtaque);

        //grito com volume extra (PlayOneShot aceita volume acima de 1)
        AudioSource grito = somGrito != null ? somGrito : somBote;
        if (grito != null && grito.clip != null)
        {
            if (rotinaGrito != null) { StopCoroutine(rotinaGrito); rotinaGrito = null; }
            if (volumeOriginalGrito >= 0f) grito.volume = volumeOriginalGrito;
            grito.PlayOneShot(grito.clip, volumeGrito);
            if (duracaoGrito > 0f) StartCoroutine(RotinaCortarGrito());
        }

        //com o modelo e o "rosto" configurados, faz o jumpscare; senao, so desce de vez
        if (modelo != null && pontoDoRosto != null)
        {
            //a camera fica congelada durante o susto: a cara dele nao sai do centro da tela
            if (controle == null) controle = FindAnyObjectByType<ControleJogador>();
            if (controle != null) controle.TravarCamera(duracaoSusto + 0.3f);
            StartCoroutine(RotinaSusto());
        }
    }

    //jumpscare: o rosto para na frente da camera, a camera treme e ele volta
    IEnumerator RotinaSusto()
    {
        emSusto = true;
        Transform cam = cameraJogador != null ? cameraJogador : (Camera.main != null ? Camera.main.transform : null);
        if (cam == null) { emSusto = false; yield break; }

        //o modelo voa ate a camera e o LateUpdate segura o rosto no alvo
        Vector3 camOriginal = cam.position;
        Vector3 rostoInicio = pontoDoRosto.position;
        Quaternion rotInicio = modelo.rotation;
        Quaternion rotFim = Quaternion.LookRotation(-cam.forward, -cam.up); //rosto (+Z) para a camera, de cabeca para baixo
        Vector3 alvoFinal = camOriginal + cam.forward * distanciaSusto + cam.up * ajusteAlturaSusto;
        if (luzPerigo != null) luzPerigo.intensity = intensidadeMaxima;
        seguirSusto = true;

        //avanca rapido ate a camera
        for (float t = 0f; t < duracaoAvanco; t += Time.deltaTime)
        {
            float k = t / duracaoAvanco;
            k = k * k;
            alvoSusto = Vector3.Lerp(rostoInicio, alvoFinal, k);
            rotSusto = Quaternion.Slerp(rotInicio, rotFim, k);
            yield return null;
        }
        alvoSusto = alvoFinal;
        rotSusto = rotFim;

        //fica na cara do jogador com a camera tremendo
        float resto = Mathf.Max(0f, duracaoSusto - duracaoAvanco - 0.4f);
        for (float t = 0f; t < resto; t += Time.deltaTime)
        {
            float forca = tremorCamera * (1f - t / resto);
            cam.position = camOriginal + Random.insideUnitSphere * forca;
            yield return null;
        }
        cam.position = camOriginal;

        //volta para o tubo
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            float k = t / 0.4f;
            alvoSusto = Vector3.Lerp(alvoFinal, rostoInicio, k);
            rotSusto = Quaternion.Slerp(rotFim, rotInicio, k);
            yield return null;
        }

        seguirSusto = false;
        modelo.localPosition = modeloPosOriginal;
        modelo.localRotation = modeloRotOriginal;
        transform.localPosition = posicaoInicial;
        transform.localRotation = rotacaoInicial;
        descidaAtual = 0f;
        perigo = 0f;
        nivelErros = 0f;
        ataqueFinal = false;
        if (animador != null) animador.Play(estadoParado, 0, 0f);
        emSusto = false;
    }
}
