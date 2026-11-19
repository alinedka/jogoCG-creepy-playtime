using System.Collections;
using UnityEngine;

//botao em volta da plataforma: brilha, acende a luz, toca o som e afunda quando e apertado
public class BotaoColorido : MonoBehaviour
{
    [Header("Identidade do botão")]
    public string nomeCor = "VERDE";

    public Color cor = Color.green;

    public Texture simbolo;

    [Header("Partes do botão (arraste da Hierarchy)")]
    public Renderer rendererTampa;

    public Transform tampa;

    public Light luz;

    public AudioSource somBotao;

    public GameObject suporte;

    [Header("Modo símbolo (etapa dos SÍMBOLOS)")]
    public Renderer quadSimbolo;
    public Color corModoSimbolo = new Color(0.06f, 0.06f, 0.07f);
    public Color corDoSimbolo = Color.white;

    [Header("Brilho (intensidade da Emission)")]
    public float brilhoApagado = 0.15f;
    public float brilhoNaMira = 0.6f;
    public float brilhoAceso = 4f;

    [Header("Animação do aperto")]
    public float profundidadeAperto = 0.02f;

    //ids das propriedades dos shaders urp
    static readonly int idEmission = Shader.PropertyToID("_EmissionColor");
    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int idBaseMap = Shader.PropertyToID("_BaseMap");
    static readonly int idSmoothness = Shader.PropertyToID("_Smoothness");

    Material material;
    Material materialSimbolo;
    Color corBaseOriginal;
    float lisuraOriginal;
    bool modoSimbolo;
    Vector3 posicaoOriginalTampa;
    float intensidadeOriginalLuz;
    Coroutine rotinaAcender;
    bool aceso;
    bool naMira;

    void Awake()
    {
        if (rendererTampa != null)
        {
            //.material cria uma copia so deste botao, assim cada um brilha sozinho
            material = rendererTampa.material;
            material.EnableKeyword("_EMISSION");
            corBaseOriginal = material.GetColor(idBaseColor);
            lisuraOriginal = material.GetFloat(idSmoothness);
        }

        if (quadSimbolo != null)
        {
            materialSimbolo = quadSimbolo.material; //copia so deste botao
            if (simbolo != null) materialSimbolo.SetTexture(idBaseMap, simbolo);
            materialSimbolo.SetColor(idBaseColor, corDoSimbolo);
            quadSimbolo.enabled = false; //comeca no modo cor
        }

        if (tampa != null)
            posicaoOriginalTampa = tampa.localPosition;

        if (luz != null)
        {
            intensidadeOriginalLuz = luz.intensity;
            luz.enabled = false;
        }

        AtualizarBrilho();
    }

    //mostrarSimbolo false = so a cor, esconderCor true = tampa preta com o simbolo branco
    public void ModoSimbolo(bool mostrarSimbolo, bool esconderCor = true)
    {
        modoSimbolo = mostrarSimbolo && esconderCor;
        if (material != null)
        {
            material.SetColor(idBaseColor, modoSimbolo ? corModoSimbolo : corBaseOriginal);
            //fosco no modo simbolo: uma tampa preta e lisa refletiria o ceu e pareceria branca
            material.SetFloat(idSmoothness, modoSimbolo ? 0.05f : lisuraOriginal);
        }
        if (quadSimbolo != null)
        {
            if (materialSimbolo != null && simbolo != null) materialSimbolo.SetTexture(idBaseMap, simbolo);
            quadSimbolo.enabled = mostrarSimbolo && simbolo != null;
        }
        AtualizarBrilho();
    }

    //mostra ou esconde o botao e a haste quando a etapa libera botoes novos
    public void Mostrar(bool visivel)
    {
        gameObject.SetActive(visivel);
        if (suporte != null) suporte.SetActive(visivel);
    }

    //se o botao sumir no meio de um aperto, volta tudo ao normal
    void OnDisable()
    {
        rotinaAcender = null;
        aceso = false;
        naMira = false;
        if (tampa != null) tampa.localPosition = posicaoOriginalTampa;
        if (luz != null) luz.enabled = false;
        AtualizarBrilho();
    }

    //acende o botao por um tempo, com afundar a tampa desce
    public void Acender(float duracao, bool afundar, bool tocarSom = true)
    {
        if (rotinaAcender != null)
            StopCoroutine(rotinaAcender);
        rotinaAcender = StartCoroutine(RotinaAcender(duracao, afundar, tocarSom));
    }

    //a mira entrou ou saiu do botao
    public void DefinirMira(bool valor)
    {
        if (naMira == valor) return;
        naMira = valor;
        AtualizarBrilho();
    }

    IEnumerator RotinaAcender(float duracao, bool afundar, bool tocarSom)
    {
        aceso = true;
        AtualizarBrilho();

        if (luz != null)
        {
            luz.color = modoSimbolo ? Color.white : cor; //no modo simbolo a luz nao entrega a cor
            luz.intensity = intensidadeOriginalLuz;
            luz.enabled = true;
        }

        if (tocarSom && somBotao != null && somBotao.clip != null)
            somBotao.PlayOneShot(somBotao.clip);

        if (afundar && tampa != null)
            tampa.localPosition = posicaoOriginalTampa + Vector3.down * profundidadeAperto;

        yield return new WaitForSeconds(duracao);

        if (tampa != null)
            tampa.localPosition = posicaoOriginalTampa;

        if (luz != null)
            luz.enabled = false;

        aceso = false;
        AtualizarBrilho();
        rotinaAcender = null;
    }

    void AtualizarBrilho()
    {
        if (material == null) return;

        if (modoSimbolo)
        {
            //no modo simbolo a tampa fica preta e sem brilho, so um cinza fraco na mira
            float cinza = aceso ? 0.5f : (naMira ? 0.12f : 0f);
            material.SetColor(idEmission, new Color(cinza, cinza, cinza));
            return;
        }

        float intensidade = aceso ? brilhoAceso : (naMira ? brilhoNaMira : brilhoApagado);
        //cor HDR: multiplicar por um valor > 1 faz o Bloom do Global Volume "estourar" a luz
        material.SetColor(idEmission, cor * intensidade);
    }

    void OnDestroy()
    {
        //a copia do material criada pelo .material nao e apagada sozinha
        if (material != null)
            Destroy(material);
        if (materialSimbolo != null)
            Destroy(materialSimbolo);
    }
}
