using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

//menu principal: jogar (com nome), como jogar, recordes e sair
public class MenuPrincipal : MonoBehaviour
{
    public string cenaDoJogo = "SampleScene";

    public GameObject painelComoJogar;

    public GameObject grupoBotoes;

    [Header("Nome do jogador")]
    public GameObject painelNome;
    public TMP_InputField campoNome;

    [Header("Recordes")]
    public GameObject painelRecordes;
    public TMP_Text textoRecordes;

    [Header("Balanço da câmera (opcional)")]
    public Transform cameraMenu;
    public float balanco = 2f;
    public float velocidadeBalanco = 0.3f;

    Quaternion rotacaoInicial;

    void Start()
    {
        //o jogo prende o mouse; no menu ele precisa estar solto e visivel
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (painelComoJogar != null) painelComoJogar.SetActive(false);
        if (painelNome != null) painelNome.SetActive(false);
        if (painelRecordes != null) painelRecordes.SetActive(false);

        if (cameraMenu != null)
            rotacaoInicial = cameraMenu.localRotation;
    }

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado != null)
        {
            //esc fecha qualquer painel aberto
            if (teclado.escapeKey.wasPressedThisFrame)
                FecharPaineis();

            //enter no painel do nome = comecar
            if (painelNome != null && painelNome.activeSelf &&
                (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame))
                ComecarJogo();
        }

        if (cameraMenu != null)
        {
            //seno = vai e volta suave, sem nunca parar de repente
            float t = Time.time * velocidadeBalanco;
            float lados = Mathf.Sin(t) * balanco;
            float cimaBaixo = Mathf.Sin(t * 0.7f) * balanco * 0.3f;
            cameraMenu.localRotation = rotacaoInicial * Quaternion.Euler(cimaBaixo, lados, 0f);
        }
    }

    //jogar (com nome)

    public void Jogar()
    {
        //sem painel de nome montado: entra direto
        if (painelNome == null)
        {
            SceneManager.LoadScene(cenaDoJogo);
            return;
        }

        AbrirPainel(painelNome);
        if (campoNome != null)
        {
            //ja sugere o ultimo nome usado
            string ultimo = Recordes.NomeAtual;
            campoNome.text = ultimo == "JOGADOR" ? "" : ultimo;
            campoNome.Select();
            campoNome.ActivateInputField();
        }
    }

    public void ComecarJogo()
    {
        Recordes.NomeAtual = campoNome != null ? campoNome.text : "";
        SceneManager.LoadScene(cenaDoJogo);
    }

    public void FecharPainelNome() => FecharPaineis();

    //outros paineis

    public void AbrirComoJogar() => AbrirPainel(painelComoJogar);
    public void FecharComoJogar() => FecharPaineis();

    public void AbrirRecordes()
    {
        if (textoRecordes != null) textoRecordes.text = Recordes.TextoDaTabela();
        AbrirPainel(painelRecordes);
    }

    public void FecharRecordes() => FecharPaineis();

    void AbrirPainel(GameObject painel)
    {
        if (painel == null) return;
        FecharPaineis();
        painel.SetActive(true);
        if (grupoBotoes != null) grupoBotoes.SetActive(false);
    }

    void FecharPaineis()
    {
        if (painelComoJogar != null) painelComoJogar.SetActive(false);
        if (painelNome != null) painelNome.SetActive(false);
        if (painelRecordes != null) painelRecordes.SetActive(false);
        if (grupoBotoes != null) grupoBotoes.SetActive(true);
    }

    public void Sair()
    {
#if UNITY_EDITOR
        //dentro do Unity, Application.Quit nao faz nada: entao paramos o Play
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
