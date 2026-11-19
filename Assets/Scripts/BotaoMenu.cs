using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

//botao do menu: cresce e muda a cor do texto quando o mouse passa por cima
public class BotaoMenu : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text texto;
    public Color corNormal = Color.white;
    public Color corDestaque = new Color(1f, 0.85f, 0.2f);
    public float escalaDestaque = 1.15f;
    public float velocidade = 12f;

    [Header("Som (opcional)")]
    public AudioSource audioMenu;
    public AudioClip somPassar;

    bool emCima;

    void Start()
    {
        if (texto != null) texto.color = corNormal;
    }

    void Update()
    {
        //lerp: a cada quadro chega um pouco mais perto do alvo = animacao suave
        float alvo = emCima ? escalaDestaque : 1f;
        float passo = 1f - Mathf.Exp(-velocidade * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * alvo, passo);

        if (texto != null)
            texto.color = Color.Lerp(texto.color, emCima ? corDestaque : corNormal, passo);
    }

    public void OnPointerEnter(PointerEventData dados)
    {
        emCima = true;
        if (audioMenu != null && somPassar != null)
            audioMenu.PlayOneShot(somPassar);
    }

    public void OnPointerExit(PointerEventData dados)
    {
        emCima = false;
    }

    void OnDisable()
    {
        //se o botao some com o mouse em cima, volta ao normal da proxima vez
        emCima = false;
        transform.localScale = Vector3.one;
    }
}
