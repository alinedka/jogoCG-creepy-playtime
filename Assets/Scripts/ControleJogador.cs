using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

//camera do jogador: olhar com o mouse, mira, maos, andar e pular fora da partida
public class ControleJogador : MonoBehaviour
{
    public JogoMemoria jogo;

    [Header("Olhar com o mouse")]
    public float sensibilidade = 0.1f;
    [Range(0f, 90f)] public float limiteCima = 90f;
    [Range(0f, 89f)] public float limiteBaixo = 45f;

    [Header("Voltar ao menu")]
    public string cenaDoMenu = "Menu";

    [Header("Mira")]
    public float alcance = 4f;

    [Header("Andar pela sala (easter egg: só antes de começar ou depois do fim)")]
    public bool podeAndar = true;
    public float velocidadeAndar = 3f;
    public float velocidadeVoltar = 8f;
    public float raioJogador = 0.35f;
    public Vector2 limiteMin = new Vector2(-8.4f, -9.7f);
    public Vector2 limiteMax = new Vector2(8.4f, 7.3f);
    public Transform[] obstaculos;
    public Transform[] obstaculosEscorregadios;
    //objetos vazados (estantes): bloqueia so onde tem colisor, da para passar por baixo
    public Transform[] obstaculosVazados;
    public float velocidadeEscorregar = 3f;
    public float folgaCabeca = 0.25f;
    public Transform plataforma;
    public float alturaPlataforma = 0.2f;
    public bool podePular = true;
    public float forcaPulo = 6.5f;
    public float gravidade = 20f;
    public float alturaOlhos = 1.4f;
    public float degrau = 0.25f;

    [Header("Máquina de garra (easter egg)")]
    public float alcanceMaquina = 2.5f;

    [Header("Mãos esticáveis (opcional)")]
    public MaoEsticavel maoEsquerda;
    public MaoEsticavel maoDireita;

    //angulos da camera no comeco (Rotation X e Y do Inspector)
    float ladosInicial;
    float anguloLados;
    //inclinacao atual (Rotation X): negativo = olhando para cima, positivo = para baixo
    float inclinacao;
    BotaoColorido botaoNaMira;
    MaquinaGarra maquinaNaMira;
    ReageAoToque toqueNaMira; //brinquedo que balanca quando a mao encosta
    Transform seguirNaMira; //parte do monstro na mira (a mao segue ela enquanto voa)
    Transform olharAlvo; //enquanto tem alvo, a camera e forcada a olhar para ele
    float olharAte; //ate quando (Time.time)
    float olharRapidez;
    float pes; //altura dos PES do jogador (0 = chao da sala)
    float pesVisto; //altura dos pes usada na camera (suaviza os degraus)
    float velocidadePulo; //velocidade para cima (+) ou para baixo (-)
    Vector3 pontoNaMira; //onde o raio da mira bateu (ou o ponto no alcance maximo)
    Vector3 posInicial; //centro da plataforma (onde o jogador joga)

    //cada obstaculo: retangulo no chao (visto de cima) + altura do topo
    struct Bloqueio { public Rect area; public float topo; public bool escorrega; }
    readonly System.Collections.Generic.List<Bloqueio> bloqueios = new System.Collections.Generic.List<Bloqueio>();
    readonly System.Collections.Generic.HashSet<Collider> colisoresVazados = new System.Collections.Generic.HashSet<Collider>();
    readonly Collider[] tocando = new Collider[16];

    void Start()
    {
        if (jogo == null)
            jogo = FindAnyObjectByType<JogoMemoria>();

        posInicial = transform.position;
        pes = pesVisto = posInicial.y - alturaOlhos;
        //cada obstaculo vira um retangulo no chao com a altura do topo
        AdicionarBloqueios(obstaculos, false);
        AdicionarBloqueios(obstaculosEscorregadios, true);
        if (obstaculosVazados != null)
            foreach (Transform v in obstaculosVazados)
                if (v != null)
                    foreach (Collider c in v.GetComponentsInChildren<Collider>()) colisoresVazados.Add(c);

        //deltaAngle transforma 350° em -10°, para o limite de cima/baixo funcionar
        Vector3 inicial = transform.localEulerAngles;
        inclinacao = Mathf.DeltaAngle(0f, inicial.x);
        ladosInicial = inicial.y;
        PrenderMouse(true);
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Keyboard teclado = Keyboard.current;

        //esc solta o cursor
        if (teclado != null && teclado.escapeKey.wasPressedThisFrame)
            PrenderMouse(false);

        //m volta ao menu principal (a cena do menu solta o mouse sozinha)
        if (teclado != null && teclado.mKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(cenaDoMenu);
            return;
        }

        bool mousePreso = Cursor.lockState == CursorLockMode.Locked;

        if (olharAlvo != null && Time.time < olharAte)
        {
            //forcado a olhar (bote do monstro): o mouse nao mexe
            ForcarOlhar();
        }
        else if (Time.time < travadaAte)
        {
            //camera congelada no jumpscare: o mouse nao mexe
        }
        else if (mouse != null)
        {
            olharAlvo = null;
            if (mousePreso)
            {
                Vector2 delta = mouse.delta.ReadValue() * sensibilidade;
                anguloLados += delta.x;
                anguloLados = Mathf.Repeat(anguloLados, 360f); //da a volta sem o numero crescer para sempre

                //o limite conta a partir do horizonte
                inclinacao = Mathf.Clamp(inclinacao - delta.y, -limiteCima, limiteBaixo);

                //giro y sempre no eixo vertical para o horizonte nao entortar
                transform.localRotation = Quaternion.Euler(inclinacao, ladosInicial + anguloLados, 0f);
            }
            else if (mouse.leftButton.wasPressedThisFrame)
            {
                //primeiro clique depois do Esc so prende o mouse de novo
                PrenderMouse(true);
                return;
            }
        }

        AtualizarMira();

        if (jogo == null) return;

        Andar(teclado, mousePreso);

        if (!mousePreso || mouse == null) return;

        //maos travadas enquanto o telao mostra a sequencia (so liberam no "SUA VEZ")
        if (!jogo.PodeApertar) return;

        //clique esquerdo = mao esquerda; clique direito = mao direita
        if (mouse.leftButton.wasPressedThisFrame)
            Apertar(maoEsquerda);
        if (mouse.rightButton.wasPressedThisFrame)
            Apertar(maoDireita);
    }

    //lanca a mao ate a mira, o botao e apertado quando ela chega
    void Apertar(MaoEsticavel mao)
    {
        //easter egg: maquina de garra (so fora da partida e de perto)
        if (botaoNaMira == null && maquinaNaMira != null && jogo.ForaDaPartida &&
            Vector3.Distance(transform.position, pontoNaMira) <= alcanceMaquina)
        {
            MaquinaGarra maquina = maquinaNaMira;
            if (maquina.Ocupada) return;
            if (mao == null) { maquina.Clicar(); return; }
            if (!mao.Ocupada) mao.Disparar(pontoNaMira, maquina.Clicar);
            return;
        }

        int indice = botaoNaMira != null ? System.Array.IndexOf(jogo.botoes, botaoNaMira) : -1;

        //sem mao montada: aperta direto (jeito antigo)
        if (mao == null)
        {
            if (indice >= 0) jogo.JogadorApertou(indice);
            return;
        }

        if (mao.Ocupada) return; //essa mao ainda esta no ar

        if (indice >= 0)
            mao.Disparar(pontoNaMira, () => jogo.JogadorApertou(indice));
        else if (toqueNaMira != null)
        {
            //encostou num brinquedo: ele balanca quando a mao chega
            ReageAoToque brinquedo = toqueNaMira;
            Vector3 deOnde = transform.position;
            mao.Disparar(pontoNaMira, () => brinquedo.Tocar(deOnde));
        }
        else
            mao.Disparar(pontoNaMira, null, seguirNaMira); //sem botao: a mao vai ate o que a mira acertou e volta
    }

    //raio do centro da camera para ver o que esta na mira
    void AtualizarMira()
    {
        BotaoColorido novo = null;
        maquinaNaMira = null;
        toqueNaMira = null;
        pontoNaMira = transform.position + transform.forward * alcance;

        //o monstro nunca atrapalha a mira dos botoes
        int camadaMonstro = LayerMask.NameToLayer("Monstro");
        int mascara = Physics.DefaultRaycastLayers;
        if (camadaMonstro >= 0) mascara &= ~(1 << camadaMonstro);

        float distancia = alcance;
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit acerto, alcance, mascara, QueryTriggerInteraction.Ignore))
        {
            novo = acerto.collider.GetComponentInParent<BotaoColorido>();
            //so vale apertar pela frente do botao
            if (novo != null && Vector3.Dot(transform.forward, novo.transform.up) > -0.1f)
                novo = null;
            maquinaNaMira = acerto.collider.GetComponentInParent<MaquinaGarra>();
            toqueNaMira = acerto.collider.GetComponentInParent<ReageAoToque>();
            pontoNaMira = acerto.point;
            distancia = acerto.distance;
        }

        //a mao para no monstro se ele estiver na frente (menos quando a mira e um botao)
        seguirNaMira = null;
        if (novo == null && camadaMonstro >= 0)
        {
            Physics.SyncTransforms(); //o monstro se mexe (animacao): colisoes na posicao deste frame
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit noMonstro, distancia, 1 << camadaMonstro, QueryTriggerInteraction.Ignore))
            {
                maquinaNaMira = null;
                toqueNaMira = null;
                pontoNaMira = noMonstro.point;
                seguirNaMira = noMonstro.collider.transform; //a mao acompanha essa parte do corpo enquanto voa
            }
        }

        if (novo != botaoNaMira)
        {
            if (botaoNaMira != null) botaoNaMira.DefinirMira(false);
            if (novo != null) novo.DefinirMira(true);
            botaoNaMira = novo;
        }
    }

    //congela a camera por alguns segundos (jumpscare)
    public void TravarCamera(float duracao)
    {
        olharAlvo = null;
        travadaAte = Time.time + duracao;
    }

    float travadaAte;

    //vira a camera a forca para o alvo (bote e tampa)
    public void OlharPara(Transform alvo, float duracao, float rapidez = 12f)
    {
        if (alvo == null) return;
        olharAlvo = alvo;
        olharAte = Time.time + duracao;
        olharRapidez = rapidez;
    }

    void ForcarOlhar()
    {
        Vector3 dir = olharAlvo.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();
        //alvo quase reto em cima/embaixo: nao gira para os lados (senao a camera rodopia)
        bool quaseVertical = new Vector2(dir.x, dir.z).magnitude < 0.15f;
        float ladosAlvo = quaseVertical ? anguloLados : Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg - ladosInicial;
        float inclinacaoAlvo = Mathf.Clamp(-Mathf.Asin(dir.y) * Mathf.Rad2Deg, -limiteCima, limiteBaixo);

        //vira rapido, mas suave (pelo caminho mais curto)
        float k = 1f - Mathf.Exp(-olharRapidez * Time.deltaTime);
        anguloLados += Mathf.DeltaAngle(anguloLados, ladosAlvo) * k;
        anguloLados = Mathf.Repeat(anguloLados, 360f);
        inclinacao = Mathf.Lerp(inclinacao, inclinacaoAlvo, k);
        transform.localRotation = Quaternion.Euler(inclinacao, ladosInicial + anguloLados, 0f);
    }

    //fora da partida anda com wasd, durante a partida volta para o centro
    void Andar(Keyboard teclado, bool mousePreso)
    {
        Vector3 pos = transform.position;

        if (podeAndar && jogo.ForaDaPartida)
        {
            if (mousePreso && teclado != null)
            {
                Vector2 dir = Vector2.zero;
                if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) dir.y += 1f;
                if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) dir.y -= 1f;
                if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) dir.x += 1f;
                if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) dir.x -= 1f;

                if (dir.sqrMagnitude > 0f)
                {
                    dir.Normalize();
                    //anda para onde esta olhando (na horizontal; subir/descer e com o pulo e a gravidade)
                    float giro = (ladosInicial + anguloLados) * Mathf.Deg2Rad;
                    Vector3 frente = new Vector3(Mathf.Sin(giro), 0f, Mathf.Cos(giro));
                    Vector3 direita = new Vector3(Mathf.Cos(giro), 0f, -Mathf.Sin(giro));
                    Vector3 passo = (frente * dir.y + direita * dir.x) * velocidadeAndar * Time.deltaTime;

                    //bateu? Tenta deslizar so em X ou so em Z (escorrega pela parede)
                    if (Livre(pos + passo)) pos += passo;
                    else if (Livre(pos + new Vector3(passo.x, 0f, 0f))) pos.x += passo.x;
                    else if (Livre(pos + new Vector3(0f, 0f, passo.z))) pos.z += passo.z;
                }
            }
        }
        else
        {
            Vector3 centro = new Vector3(posInicial.x, pos.y, posInicial.z);
            pos = Vector3.MoveTowards(pos, centro, velocidadeVoltar * Time.deltaTime);
        }

        //em cima de bichinho nao fica: escorrega para o lado
        pos = Escorregar(pos);

        //chao embaixo do jogador: piso, plataforma ou o topo de um objeto (caixa, barril)
        float chao = AlturaDoChao(pos, pes);
        bool apoiado = pes <= chao + 0.001f;

        //pulo (Espaco): so fora da partida e com os pes apoiados
        if (podePular && podeAndar && jogo.ForaDaPartida && mousePreso && teclado != null &&
            apoiado && teclado.spaceKey.wasPressedThisFrame)
            velocidadePulo = forcaPulo;

        //gravidade: sobe no pulo, cai quando sai da beirada
        if (!apoiado || velocidadePulo > 0f)
        {
            velocidadePulo -= gravidade * Time.deltaTime;
            float subida = velocidadePulo * Time.deltaTime;
            if (subida > 0f)
            {
                //cabeca no teto/duto: para de subir e comeca a cair
                float pode = EspacoAcima(new Vector3(pos.x, pes + alturaOlhos, pos.z), subida);
                if (pode < subida) { subida = pode; velocidadePulo = 0f; }
            }
            pes += subida;
            if (pes <= chao) { pes = chao; velocidadePulo = 0f; }
            pesVisto = pes;
        }
        else
        {
            pes = chao; //degrau pequeno sobe na hora
            velocidadePulo = 0f;
            pesVisto = Mathf.MoveTowards(pesVisto, pes, 1.5f * Time.deltaTime); //a camera acompanha suave
        }

        pos.y = pesVisto + alturaOlhos;
        transform.position = pos;
    }

    void AdicionarBloqueios(Transform[] grupos, bool escorrega)
    {
        if (grupos == null) return;
        foreach (Transform grupo in grupos)
            if (grupo != null)
                foreach (Renderer r in grupo.GetComponentsInChildren<Renderer>())
                {
                    if (EhVazado(r.transform)) continue;
                    Bounds b = r.bounds;
                    bloqueios.Add(new Bloqueio
                    {
                        area = Rect.MinMaxRect(b.min.x - raioJogador, b.min.z - raioJogador,
                                               b.max.x + raioJogador, b.max.z + raioJogador),
                        topo = b.max.y,
                        escorrega = escorrega
                    });
                }
    }

    //altura do chao no ponto (piso, plataforma ou topo de um objeto), bichinhos nao contam
    float AlturaDoChao(Vector3 p, float pesAgora)
    {
        float piso = posInicial.y - alturaOlhos - (NaPlataforma(p) ? 0f : alturaPlataforma);
        Vector2 xz = new Vector2(p.x, p.z);
        foreach (Bloqueio b in bloqueios)
            if (!b.escorrega && b.topo > piso && b.topo <= pesAgora + degrau && b.area.Contains(xz))
                piso = b.topo;
        return piso;
    }

    //caiu em cima de um bichinho, escorrega para o lado
    Vector3 Escorregar(Vector3 pos)
    {
        Vector2 xz = new Vector2(pos.x, pos.z);
        foreach (Bloqueio b in bloqueios)
        {
            if (!b.escorrega || pes >= b.topo || !b.area.Contains(xz)) continue;
            Vector2 fora = xz - b.area.center;
            if (fora.sqrMagnitude < 0.0001f) fora = new Vector2(transform.right.x, transform.right.z);
            Vector2 passo = fora.normalized * velocidadeEscorregar * Time.deltaTime;
            Vector3 novo = pos + new Vector3(passo.x, 0f, passo.y);
            if (novo.x >= limiteMin.x && novo.x <= limiteMax.x && novo.z >= limiteMin.y && novo.z <= limiteMax.y)
                pos = novo;
            break;
        }
        return pos;
    }

    //quanto a cabeca ainda pode subir antes de bater no teto
    float EspacoAcima(Vector3 olhos, float querSubir)
    {
        //ignore Raycast (maos, monstro, tampa) nao conta
        if (Physics.SphereCast(olhos, 0.2f, Vector3.up, out RaycastHit h, querSubir + folgaCabeca,
                               ~((1 << 2) | (1 << Mathf.Max(0, LayerMask.NameToLayer("Monstro")))), QueryTriggerInteraction.Ignore))
            return Mathf.Max(0f, h.distance - folgaCabeca);
        return querSubir;
    }

    //so bloqueia se o objeto for mais alto que os pes + degrau
    bool Livre(Vector3 p)
    {
        if (p.x < limiteMin.x || p.x > limiteMax.x || p.z < limiteMin.y || p.z > limiteMax.y) return false;
        Vector2 xz = new Vector2(p.x, p.z);
        foreach (Bloqueio b in bloqueios)
            if (b.topo > pes + (b.escorrega ? 0f : degrau) && b.area.Contains(xz)) return false;

        //estantes: corpo do jogador (dos pes + degrau ate os olhos) encostando nas colunas e prateleiras
        if (colisoresVazados.Count > 0)
        {
            Vector3 baixo = new Vector3(p.x, pes + degrau + raioJogador, p.z);
            Vector3 cima = new Vector3(p.x, pes + alturaOlhos, p.z);
            int n = Physics.OverlapCapsuleNonAlloc(baixo, cima, raioJogador, tocando, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (colisoresVazados.Contains(tocando[i])) return false;
        }
        return true;
    }

    bool EhVazado(Transform t)
    {
        if (obstaculosVazados == null) return false;
        foreach (Transform v in obstaculosVazados)
            if (v != null && t.IsChildOf(v)) return true;
        return false;
    }

    bool NaPlataforma(Vector3 p)
    {
        if (plataforma == null) return true;
        Vector3 c = plataforma.position;
        Vector3 meio = plataforma.lossyScale * 0.5f; //oval: metade da largura e do comprimento
        float dx = (p.x - c.x) / meio.x, dz = (p.z - c.z) / meio.z;
        return dx * dx + dz * dz <= 1f;
    }

    void PrenderMouse(bool prender)
    {
        Cursor.lockState = prender ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !prender;
    }
}
