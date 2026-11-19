using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

//regras do jogo de memoria: o telao mostra a sequencia, o jogador repete, acertou = +1 cor, errou = perde vida
public class JogoMemoria : MonoBehaviour
{
    public enum TipoDica { Cor, Simbolo, Palavra }
    //(novos tipos sempre no FIM da lista, para nao baguncar as etapas ja configuradas)
    public enum TipoEtapa { Cores, Simbolos, Palavras, Mistura, PalavrasESimbolos }

    [System.Serializable]
    public class Etapa
    {
        public int quantidadeBotoes = 4;
        public TipoEtapa tipo = TipoEtapa.Cores;
        [Min(0)] public int tamanhoInicial = 0;
        [Min(0)] public int tamanhoMaximo = 0;
        [TextArea(2, 4)] public string mensagemNoTelao = "";
        public float tempoMensagem = 3.5f;
        public bool infinita = false;
    }

    //um item da sequencia: o botao certo e como a dica aparece
    struct Passo
    {
        public int resposta; //indice do botao certo
        public TipoDica tipo;
        public int corDaLetra; //so para Palavra: indice do botao cuja cor pinta a letra
    }

    enum Estado { Menu, Mostrando, Esperando, Fim }

    [Header("Referências (arraste da Hierarchy)")]
    public BotaoColorido[] botoes;
    public PainelDisplay painel;
    public PainelDisplay[] outrosPaineis;
    public MonstroDescendo monstro;
    public TMP_Text textoHud;
    public TMP_Text textoAviso;
    public UnityEngine.UI.Graphic[] coracoes;
    public Color corCoracaoCheio = new Color(0.85f, 0.05f, 0.08f);
    public Color corCoracaoVazio = new Color(0.15f, 0.15f, 0.15f, 0.45f);

    [Header("Morte (opcional)")]
    public CanvasGroup telaMorte;
    public float tempoTelaMorte = 2.5f;
    public float fadeTelaMorte = 0.4f;
    public AudioSource musicaFundo;

    [Header("Sons (opcionais)")]
    public AudioSource audioJogo;
    public AudioClip somDica; //bip de cada cor da sequencia
    public AudioClip somAcerto;
    public AudioClip somErro;
    public AudioClip somNovaFase; //comeco de etapa / botoes novos
    public AudioClip somFimDeJogo;
    [Header("Conquista (easter egg da máquina de garra)")]
    public CanvasGroup painelConquista;
    public TMP_Text textoConquista;
    public AudioClip somConquista;
    public float tempoConquista = 4f;

    [Header("Sons do bote")]
    public AudioSource somDano;
    [Range(1f, 3f)] public float volumeDano = 1.5f;
    public AudioClip somGarra;
    [Range(0f, 3f)] public float volumeGarra = 1.5f;
    public float atrasoDano = 0.12f;

    [Header("Etapas (cada uma pode liberar botões novos)")]
    public Etapa[] etapas =
    {
        new Etapa { quantidadeBotoes = 4, tipo = TipoEtapa.Cores },
        new Etapa { quantidadeBotoes = 6, tipo = TipoEtapa.Cores },
    };

    [Header("Regras")]
    public int vidasIniciais = 3;
    public int tamanhoInicial = 3;
    public int tamanhoMaximo = 7;
    [Min(1f)] public float pesoBotoesNovos = 3f;

    [Header("Tempo (segundos)")]
    public float tempoDicaInicial = 0.9f;
    public float tempoDicaMinimo = 0.35f;
    public float reducaoPorRodada = 0.03f;
    public float pausaEntreDicas = 0.25f;
    public float tempoCerto = 1.2f;

    Estado estado = Estado.Menu;
    readonly List<Passo> sequencia = new List<Passo>();
    readonly List<PainelDisplay> paineis = new List<PainelDisplay>(); //frente + outros
    float volumeMusica = 0.2f;
    int posicao; //qual item da sequencia o jogador deve apertar agora
    int rodada; //conta todas as rodadas do jogo (deixa as dicas mais rapidas)
    int etapaAtual;
    int botoesAtivos;
    int vidas;
    int pontos;
    int recorde;
    TampaTubo tampa;

    static readonly Color preto = new Color(0.05f, 0.05f, 0.05f);
    static readonly Color branco = Color.white;
    static readonly Color verdeTexto = new Color(0.05f, 0.45f, 0.1f);
    static readonly Color vermelhoTexto = new Color(0.7f, 0.03f, 0.03f);

    //vez do jogador repetir a sequencia
    public bool EsperandoJogador => estado == Estado.Esperando;
    public int Rodada => rodada;
    //tamanho da sequencia atual
    public int TamanhoSequencia => sequencia.Count;

    //as maos so funcionam na vez do jogador ou fora da partida
    public bool PodeApertar => estado == Estado.Esperando || estado == Estado.Menu || estado == Estado.Fim;

    //antes de comecar ou depois do fim: pode andar pela sala
    public bool ForaDaPartida => estado == Estado.Menu || estado == Estado.Fim;

    //aviso de conquista da maquina de garra
    public void MostrarConquista(string texto)
    {
        if (painelConquista == null) return;
        if (rotinaConquista != null) StopCoroutine(rotinaConquista);
        rotinaConquista = StartCoroutine(RotinaConquista(texto));
    }

    Coroutine rotinaConquista;

    IEnumerator RotinaConquista(string texto)
    {
        if (textoConquista != null) textoConquista.text = texto;
        Tocar(somConquista);
        yield return StartCoroutine(Desvanecer(painelConquista, 1f, 0.3f));
        yield return new WaitForSeconds(tempoConquista);
        yield return StartCoroutine(Desvanecer(painelConquista, 0f, 0.8f));
        rotinaConquista = null;
    }

    void Start()
    {
        recorde = Recordes.Melhor(); //o maior da tabela de recordes
        tampa = FindAnyObjectByType<TampaTubo>();
        if (musicaFundo != null) volumeMusica = musicaFundo.volume;
        if (telaMorte != null) { telaMorte.alpha = 0f; telaMorte.blocksRaycasts = false; }
        if (painelConquista != null) { painelConquista.alpha = 0f; painelConquista.blocksRaycasts = false; }

        //junta todos os paineis numa lista so (o da frente e sempre o numero 0)
        paineis.Clear();
        if (painel != null) paineis.Add(painel);
        if (outrosPaineis != null)
            foreach (PainelDisplay p in outrosPaineis)
                if (p != null && !paineis.Contains(p)) paineis.Add(p);

        etapaAtual = 0;
        AtualizarBotoesAtivos();
        MostrarMenu();
    }

    //entrada (chamado pelo ControleJogador)

    //o jogador apertou um botao
    public void JogadorApertou(int indice)
    {
        if (indice < 0 || indice >= botoesAtivos || botoes[indice] == null) return;

        switch (estado)
        {
            case Estado.Menu:
            case Estado.Fim:
                //espera a tampa do tubo terminar de fechar
                if (tampa != null && !tampa.Fechada) break;
                botoes[indice].Acender(0.2f, true);
                NovoJogo();
                break;

            case Estado.Esperando:
                botoes[indice].Acender(0.25f, true);
                ConferirResposta(indice);
                break;

            //enquanto a sequencia esta sendo mostrada, os botoes nao respondem
            case Estado.Mostrando:
                break;
        }
    }

    //o monstro chegou no jogador, conta como erro
    public void TempoEsgotado()
    {
        if (estado != Estado.Esperando) return;
        StartCoroutine(RotinaErro(sequencia[posicao].resposta, "DEMOROU!"));
    }

    //fluxo do jogo

    void MostrarMenu()
    {
        estado = Estado.Menu;
        MensagemEmTodos("APERTE UM\nBOTÃO", preto, painel.corNeutra, painel.brilhoNeutro);
        Avisar("Mire em um botão e clique para começar  •  WASD: andar  •  M: menu");
        AtualizarHud();
    }

    void NovoJogo()
    {
        rodada = 1;
        vidas = vidasIniciais;
        pontos = 0;
        etapaAtual = 0;
        if (monstro != null) monstro.Reiniciar();
        StartCoroutine(RotinaIniciarEtapa());
    }

    Etapa EtapaAtual => etapas[Mathf.Clamp(etapaAtual, 0, etapas.Length - 1)];

    //tamanhos da sequencia: a etapa pode ter os seus; 0 = usa o geral
    int TamanhoInicialAtual => EtapaAtual.tamanhoInicial > 0 ? EtapaAtual.tamanhoInicial : tamanhoInicial;
    int TamanhoMaximoAtual => EtapaAtual.tamanhoMaximo > 0 ? EtapaAtual.tamanhoMaximo : tamanhoMaximo;

    //liga os botoes da etapa atual e esconde os outros
    void AtualizarBotoesAtivos()
    {
        int quantos = etapas != null && etapas.Length > 0 ? EtapaAtual.quantidadeBotoes : botoes.Length;
        botoesAtivos = Mathf.Clamp(quantos, 1, botoes.Length);

        //botoes de COR ficam sempre na cor deles; botoes de SIMBOLO sao pretos com o simbolo branco
        for (int i = 0; i < botoes.Length; i++)
        {
            if (botoes[i] == null) continue;
            botoes[i].Mostrar(i < botoesAtivos);
            bool temSimbolo = botoes[i].simbolo != null;
            botoes[i].ModoSimbolo(temSimbolo, temSimbolo);
        }
    }

    IEnumerator RotinaIniciarEtapa()
    {
        estado = Estado.Mostrando;
        AtualizarBotoesAtivos();
        AtualizarHud();

        //o telao mostra so "ETAPA N"
        Tocar(somNovaFase);
        MensagemEmTodos(TituloDaEtapa(), preto, painel.corNeutra, painel.brilhoNeutro);
        //a explicacao na legenda so aparece na Etapa 1; nas outras o jogador descobre sozinho
        Avisar(etapaAtual == 0 ? ExplicacaoDaEtapa() : "");
        yield return new WaitForSeconds(3f);

        //aviso especial da etapa no telao (etapa 4 = vale o que esta escrito)
        if (!string.IsNullOrEmpty(EtapaAtual.mensagemNoTelao))
        {
            Tocar(somNovaFase);
            MensagemEmTodos(EtapaAtual.mensagemNoTelao, preto, painel.corNeutra, painel.brilhoNeutro);
            yield return new WaitForSeconds(EtapaAtual.tempoMensagem);
        }

        //comeca uma sequencia nova com "tamanhoInicial" cores
        sequencia.Clear();
        for (int i = 0; i < TamanhoInicialAtual; i++)
            AdicionarPasso();

        StartCoroutine(RotinaMostrarSequencia());
    }

    //mostra a sequencia inteira no telao
    IEnumerator RotinaMostrarSequencia()
    {
        estado = Estado.Mostrando;
        AtualizarHud();

        LimparTodos();
        Avisar("Preste atenção...");
        yield return new WaitForSeconds(0.8f);

        float tempoDica = Mathf.Max(tempoDicaMinimo, tempoDicaInicial - reducaoPorRodada * (rodada - 1));

        foreach (Passo passo in sequencia)
        {
            MostrarDica(passo);
            yield return new WaitForSeconds(tempoDica);
            LimparTodos();
            yield return new WaitForSeconds(pausaEntreDicas);
        }

        //agora e a vez do jogador
        posicao = 0;
        estado = Estado.Esperando;
        MensagemEmTodos("SUA VEZ", preto, painel.corNeutra, painel.brilhoNeutro);
        AvisarProgresso();
    }

    //coloca mais uma cor no final da sequencia
    void AdicionarPasso()
    {
        Passo p = new Passo();

        //como esta dica vai aparecer no telao
        switch (EtapaAtual.tipo)
        {
            case TipoEtapa.Cores: p.tipo = TipoDica.Cor; break;
            case TipoEtapa.Simbolos: p.tipo = TipoDica.Simbolo; break;
            case TipoEtapa.Palavras: p.tipo = TipoDica.Palavra; break;
            case TipoEtapa.PalavrasESimbolos: p.tipo = Random.value < 0.5f ? TipoDica.Palavra : TipoDica.Simbolo; break; //palavra OU simbolo
            default: p.tipo = Random.value < 0.5f ? TipoDica.Cor : TipoDica.Simbolo; break; //mistura: cor OU simbolo
        }

        //separa os botoes ligados em botoes de COR e botoes de SIMBOLO
        var deCor = new List<int>();
        var deSimbolo = new List<int>();
        for (int i = 0; i < botoesAtivos; i++)
            (botoes[i].simbolo != null ? deSimbolo : deCor).Add(i);

        //dica de simbolo -> um botao de simbolo; dica de cor/palavra -> um botao de cor
        if (p.tipo == TipoDica.Simbolo && deSimbolo.Count == 0) p.tipo = TipoDica.Cor;
        if (p.tipo != TipoDica.Simbolo && deCor.Count == 0) p.tipo = TipoDica.Simbolo;
        var candidatos = p.tipo == TipoDica.Simbolo ? deSimbolo : deCor;
        p.resposta = SortearComPeso(candidatos);

        //na palavra, a cor da letra e de OUTRO botao de cor (para confundir)
        p.corDaLetra = p.resposta;
        if (deCor.Count > 1)
            while (p.corDaLetra == p.resposta)
                p.corDaLetra = deCor[Random.Range(0, deCor.Count)];

        sequencia.Add(p);
    }

    //sorteia um botao com mais chance para os que acabaram de ser liberados
    int SortearComPeso(List<int> candidatos)
    {
        int primeiroNovo = etapaAtual > 0 ? Mathf.Clamp(etapas[etapaAtual - 1].quantidadeBotoes, 0, botoes.Length) : 0;
        float total = 0f;
        foreach (int i in candidatos) total += (etapaAtual > 0 && i >= primeiroNovo) ? pesoBotoesNovos : 1f;

        float sorteio = Random.value * total;
        foreach (int i in candidatos)
        {
            sorteio -= (etapaAtual > 0 && i >= primeiroNovo) ? pesoBotoesNovos : 1f;
            if (sorteio <= 0f) return i;
        }
        return candidatos[candidatos.Count - 1];
    }

    void MostrarDica(Passo passo)
    {
        BotaoColorido certo = botoes[passo.resposta];

        //so os TELOES mostram a dica (todos iguais): os botoes nao acendem durante a sequencia
        foreach (PainelDisplay alvo in paineis)
        {
            switch (passo.tipo)
            {
                case TipoDica.Cor:
                    alvo.MostrarCor(certo.cor, certo.nomeCor); //cor + o nome dela escrito
                    break;

                case TipoDica.Simbolo:
                    alvo.MostrarSimbolo(certo.simbolo);
                    break;

                case TipoDica.Palavra:
                    //escreve a cor CERTA, mas o telao acende em OUTRA cor (para confundir)
                    alvo.MostrarPalavra(certo.nomeCor, botoes[passo.corDaLetra].cor);
                    break;
            }
        }

        //bip da dica (som do telao da frente, se tiver AudioSource; senao o som geral)
        if (painel != null && painel.som != null)
            painel.TocarSom(somDica);
        else
            Tocar(somDica);
    }

    void ConferirResposta(int indice)
    {
        Passo esperado = sequencia[posicao];

        if (indice != esperado.resposta)
        {
            StartCoroutine(RotinaErro(esperado.resposta));
            return;
        }

        posicao++;
        if (monstro != null) monstro.Acertou();
        if (posicao >= sequencia.Count)
            StartCoroutine(RotinaAcertouRodada());
        else
            AvisarProgresso();
    }

    IEnumerator RotinaAcertouRodada()
    {
        estado = Estado.Mostrando;
        pontos += sequencia.Count * 10 * (etapaAtual + 1);
        if (monstro != null) monstro.RodadaCompleta();
        AtualizarHud();

        yield return new WaitForSeconds(0.3f);
        Tocar(somAcerto);
        MensagemEmTodos("CERTO!", verdeTexto, painel.corNeutra, painel.brilhoNeutro);
        Avisar("Muito bem!");
        yield return new WaitForSeconds(tempoCerto);

        //o som de acerto dura so o tempo do "CERTO!" na tela (some com um fade rapido)
        yield return StartCoroutine(SumirSomDoJogo(0.15f));

        rodada++;

        //etapa infinita: nunca passa de etapa, so vai aumentando a sequencia
        if (sequencia.Count >= TamanhoMaximoAtual && !EtapaAtual.infinita)
        {
            //etapa completa: passa para a proxima (ou venceu o jogo)
            etapaAtual++;
            if (etapaAtual >= etapas.Length)
                StartCoroutine(RotinaVitoria());
            else
                StartCoroutine(RotinaIniciarEtapa());
        }
        else
        {
            //mesma sequencia + 1 cor no final
            AdicionarPasso();
            StartCoroutine(RotinaMostrarSequencia());
        }
    }

    //garra acertando + gemido de dor logo depois
    IEnumerator RotinaSomDano()
    {
        //espera o monstro dar o bote (a camera vira para ele antes)
        if (monstro != null && monstro.AtrasoBote > 0f) yield return new WaitForSeconds(monstro.AtrasoBote);
        if (somGarra != null && volumeGarra > 0f) somDano.PlayOneShot(somGarra, volumeGarra);
        if (somDano.clip == null) yield break;
        if (atrasoDano > 0f) yield return new WaitForSeconds(atrasoDano);
        somDano.PlayOneShot(somDano.clip, volumeDano);
    }

    IEnumerator RotinaErro(int respostaCerta, string titulo = "ERRADO")
    {
        estado = Estado.Mostrando;
        vidas--;
        AtualizarHud();

        //o monstro desce mais um degrau (devagar) e fica mais baixo
        if (monstro != null && vidas > 0) monstro.Errou();
        //o jogador se machuca com o bote (na ultima vida quem grita e o monstro)
        if (vidas > 0 && somDano != null)
            StartCoroutine(RotinaSomDano());

        Tocar(somErro);
        //fundo branco: vermelho/verde no fundo confundiriam com as cores dos botoes
        MensagemEmTodos(titulo, vermelhoTexto, painel.corNeutra, painel.brilhoNeutro);
        Avisar("O certo era o botão " + botoes[respostaCerta].nomeCor);

        //mostra qual era o botao certo
        yield return new WaitForSeconds(0.4f);
        botoes[respostaCerta].Acender(0.8f, false, false);
        yield return new WaitForSeconds(1.4f);

        if (vidas <= 0)
            StartCoroutine(RotinaFimDeJogo("FIM DE JOGO", true));
        else
            StartCoroutine(RotinaMostrarSequencia()); //mostra a MESMA sequencia de novo
    }

    IEnumerator RotinaVitoria()
    {
        if (monstro != null) monstro.Reiniciar(); //o monstro volta la para cima
        yield return RotinaFimDeJogo("VOCÊ\nVENCEU!", false);
    }

    IEnumerator RotinaFimDeJogo(string titulo, bool perdeu)
    {
        estado = Estado.Mostrando;
        Tocar(somFimDeJogo);

        //guarda nome + pontos na tabela de recordes (top 10)
        int lugar = pontos > 0 ? Recordes.Adicionar(Recordes.NomeAtual, pontos) : 0;
        bool novoRecorde = lugar == 1;
        recorde = Recordes.Melhor();

        if (perdeu && monstro != null)
        {
            //susto: teloes apagam, a musica para e o monstro pula na cara do jogador
            LimparTodos();
            Avisar("");
            //a musica para no susto (some rapido e pausa; volta de onde parou depois da tela de morte)
            if (musicaFundo != null) StartCoroutine(PararMusica(0.12f));
            monstro.AtaqueFinal();
            yield return new WaitForSeconds(Mathf.Max(0f, monstro.DuracaoSusto - fadeTelaMorte));

            //tela preta com "VOCE MORREU" (o grito continua)
            yield return StartCoroutine(Desvanecer(telaMorte, 1f, fadeTelaMorte));
        }

        //prepara o fim de jogo (atras da tela preta, se ela estiver ligada)
        if (perdeu)
            MensagemEmTodos(titulo + "\n" + pontos + " PONTOS", branco, preto, 0f);
        else
            MensagemEmTodos(titulo + "\n" + pontos + " PONTOS", verdeTexto, painel.corNeutra, painel.brilhoNeutro);
        Avisar(novoRecorde ? "NOVO RECORDE!" : lugar > 0 ? lugar + "º lugar nos recordes!" : "Recorde: " + recorde);
        AtualizarHud();

        if (perdeu && monstro != null)
        {
            //segura a tela preta, some com o grito e volta para a sala
            yield return new WaitForSeconds(tempoTelaMorte);
            monstro.PararGrito(0.8f);
            yield return StartCoroutine(Desvanecer(telaMorte, 0f, 0.8f));
            if (musicaFundo != null) { musicaFundo.UnPause(); StartCoroutine(MudarVolume(musicaFundo, volumeMusica, 1.5f)); }
        }

        //espera um pouco para o jogador nao reiniciar sem querer
        yield return new WaitForSeconds(2f);
        etapaAtual = 0;
        AtualizarBotoesAtivos(); //volta para os botoes da etapa 1
        if (monstro != null) monstro.Reiniciar(); //o monstro sobe de volta (a tampa do tubo fecha depois)
        estado = Estado.Fim;
        Avisar((novoRecorde ? "NOVO RECORDE! " : "") + "Clique em um botão para jogar de novo  •  WASD: andar  •  M: menu");
    }

    //varios paineis

    void MensagemEmTodos(string mensagem, Color corDaLetra, Color corDoFundo, float brilho)
    {
        foreach (PainelDisplay p in paineis)
            p.MostrarMensagem(mensagem, corDaLetra, corDoFundo, brilho);
    }

    void LimparTodos()
    {
        foreach (PainelDisplay p in paineis)
            p.Limpar();
    }


    //textos e sons

    //no telao aparece so "ETAPA N" (sem o tipo embaixo)
    string TituloDaEtapa()
    {
        return "ETAPA " + (etapaAtual + 1);
    }

    string ExplicacaoDaEtapa()
    {
        switch (EtapaAtual.tipo)
        {
            case TipoEtapa.Cores: return "Decore as cores no telão e repita. A cada acerto entra mais uma no final!";
            case TipoEtapa.Simbolos: return "Agora sem cores! Aperte o botão que tem o SÍMBOLO que aparece no telão.";
            case TipoEtapa.Palavras: return "Vale o que está ESCRITO, não a cor do telão!";
            case TipoEtapa.PalavrasESimbolos: return "Palavras e símbolos! Na palavra, vale o que está ESCRITO.";
            default: return "Cores e símbolos misturados! Se aparecer um símbolo, aperte o botão que tem ele.";
        }
    }

    void AvisarProgresso()
    {
        //mostra quantos ja apertou, mas NAO o total (o tamanho da sequencia continua segredo)
        Avisar("Apertados: " + posicao);
    }

    void Avisar(string mensagem)
    {
        if (textoAviso != null)
            textoAviso.text = mensagem;
    }

    void AtualizarHud()
    {
        //coracoes: no menu ficam todos cheios; no jogo, um apaga a cada vida perdida
        if (coracoes != null)
        {
            int cheios = estado == Estado.Menu ? coracoes.Length : vidas;
            for (int i = 0; i < coracoes.Length; i++)
                if (coracoes[i] != null)
                    coracoes[i].color = i < cheios ? corCoracaoCheio : corCoracaoVazio;
        }

        if (textoHud == null) return;

        //sem sequencia e sem vidas no texto (as vidas sao os coracoes)
        if (estado == Estado.Menu)
            textoHud.text = "Recorde: " + recorde;
        else
            //so pontos e recorde (a etapa aparece no telao; o nome, na tabela de recordes)
            textoHud.text = "Pontos " + pontos + "    Recorde " + recorde;
    }

    void Tocar(AudioClip som)
    {
        if (audioJogo != null && som != null)
            audioJogo.PlayOneShot(som);
    }

    //muda o alpha da tela aos poucos
    IEnumerator Desvanecer(CanvasGroup grupo, float alvo, float duracao)
    {
        if (grupo == null) yield break;
        float inicio = grupo.alpha;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            grupo.alpha = Mathf.Lerp(inicio, alvo, t / duracao);
            yield return null;
        }
        grupo.alpha = alvo;
    }

    //muda o volume aos poucos
    IEnumerator MudarVolume(AudioSource fonte, float alvo, float duracao)
    {
        float inicio = fonte.volume;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            fonte.volume = Mathf.Lerp(inicio, alvo, t / duracao);
            yield return null;
        }
        fonte.volume = alvo;
    }

    //some com a musica rapido e pausa
    IEnumerator PararMusica(float duracao)
    {
        yield return StartCoroutine(MudarVolume(musicaFundo, 0f, duracao));
        musicaFundo.Pause();
    }

    //abaixa o som ate zero, para e volta o volume
    IEnumerator SumirSomDoJogo(float duracao)
    {
        if (audioJogo == null) yield break;
        float volumeOriginal = audioJogo.volume;
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            audioJogo.volume = Mathf.Lerp(volumeOriginal, 0f, t / duracao);
            yield return null;
        }
        audioJogo.Stop();
        audioJogo.volume = volumeOriginal;
    }
}
