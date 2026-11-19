using TMPro;
using UnityEngine;

//telao da parede: mostra a cor, o simbolo ou a palavra da dica
public class PainelDisplay : MonoBehaviour
{
    [Header("Ladrilhos (arraste todos os cubos da grade)")]
    public Renderer[] ladrilhos;

    [Header("Cores do painel")]
    public Color corNeutra = new Color(0.92f, 0.92f, 0.9f);
    public Color corFundoEscuro = new Color(0.12f, 0.12f, 0.12f);
    public float brilhoNeutro = 0.25f;
    public float brilhoCor = 0.15f;

    [Header("Texto e símbolo (na frente do painel)")]
    public TMP_Text texto;
    public Renderer simbolo;
    public Color corBordaTexto = new Color(0.12f, 0.12f, 0.12f);
    [Range(0f, 1f)] public float larguraBordaTexto = 0.35f;

    [Header("Som (opcional)")]
    public AudioSource som;

    static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int idEmission = Shader.PropertyToID("_EmissionColor");
    static readonly int idBaseMap = Shader.PropertyToID("_BaseMap");

    Material[] materiais;
    Material materialSimbolo;

    void Awake()
    {
        //pega uma copia do material de cada ladrilho para poder mudar a cor
        materiais = new Material[ladrilhos.Length];
        for (int i = 0; i < ladrilhos.Length; i++)
        {
            if (ladrilhos[i] == null) continue;
            materiais[i] = ladrilhos[i].material;
            materiais[i].EnableKeyword("_EMISSION");
        }

        if (simbolo != null)
            materialSimbolo = simbolo.material;

        //contorno nas letras (o TextMeshPro cria uma copia do material so para este texto)
        if (texto != null)
        {
            texto.outlineColor = corBordaTexto;
            texto.outlineWidth = larguraBordaTexto;
        }

        Limpar();
    }

    //volta ao normal: cor neutra, sem texto e sem simbolo
    public void Limpar()
    {
        PintarLadrilhos(corNeutra, brilhoNeutro);
        EsconderTextoESimbolo();
    }

    //cor: o painel acende na cor e escreve o nome dela por cima
    public void MostrarCor(Color cor, string nome = null)
    {
        PintarLadrilhos(cor, brilhoCor);
        if (simbolo != null) simbolo.enabled = false;
        if (texto != null)
        {
            //a letra tem a MESMA cor do telao; a borda cinza e que deixa ler
            texto.text = nome ?? "";
            texto.color = cor;
        }
    }

    //simbolo: fundo escuro e o simbolo no meio
    public void MostrarSimbolo(Texture textura)
    {
        PintarLadrilhos(corFundoEscuro, 0f);
        if (texto != null) texto.text = "";

        if (simbolo != null)
        {
            simbolo.enabled = textura != null;
            if (materialSimbolo != null && textura != null)
                materialSimbolo.SetTexture(idBaseMap, textura);
        }
    }

    //palavra (efeito stroop): o telao acende numa cor e escreve o nome de outra
    public void MostrarPalavra(string palavra, Color corDoTelao)
    {
        MostrarCor(corDoTelao, palavra);
    }

    //mostra um texto qualquer
    public void MostrarMensagem(string mensagem, Color corDaLetra, Color corDoFundo, float brilhoFundo)
    {
        PintarLadrilhos(corDoFundo, brilhoFundo);
        if (simbolo != null) simbolo.enabled = false;

        if (texto != null)
        {
            texto.text = mensagem;
            texto.color = corDaLetra;
        }
    }

    //toca um som saindo do painel
    public void TocarSom(AudioClip clip)
    {
        if (som == null) return;
        if (clip != null) som.PlayOneShot(clip);
        else if (som.clip != null) som.Play(); //usa o clip do proprio AudioSource
    }

    void PintarLadrilhos(Color cor, float brilho)
    {
        if (materiais == null) return;

        foreach (Material m in materiais)
        {
            if (m == null) continue;
            //_BaseColor multiplica a textura (por isso a textura dos ladrilhos deve ser clara/cinza)
            m.SetColor(idBaseColor, cor);
            m.SetColor(idEmission, cor * brilho);
        }
    }

    void EsconderTextoESimbolo()
    {
        if (texto != null) texto.text = "";
        if (simbolo != null) simbolo.enabled = false;
    }

    void OnDestroy()
    {
        if (materiais != null)
            foreach (Material m in materiais)
                if (m != null) Destroy(m);

        if (materialSimbolo != null)
            Destroy(materialSimbolo);
    }
}
