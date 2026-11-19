using UnityEngine;

//encurta bracos e pernas do monstro depois da animacao sem deixar eles finos
[DefaultExecutionOrder(-10)] //roda antes do MonstroDescendo (que centraliza o monstro)
public class AjusteOssos : MonoBehaviour
{
    [System.Serializable]
    public class Grupo
    {
        public string nome = "Pernas";
        public Transform[] ossosDeCima;
        public Transform[] ossosDoMeio;
        [Range(0.1f, 1.5f)] public float comprimento = 0.5f;
        [Range(0.3f, 3f)] public float grossura = 1f;
    }

    public Grupo[] grupos;

    Vector3[][] eixos; //eixo de comprimento de cada osso do meio

    void Start()
    {
        if (grupos == null) return;
        eixos = new Vector3[grupos.Length][];
        for (int g = 0; g < grupos.Length; g++)
        {
            var meio = grupos[g].ossosDoMeio ?? new Transform[0];
            eixos[g] = new Vector3[meio.Length];
            for (int i = 0; i < meio.Length; i++) eixos[g][i] = EixoDoComprimento(meio[i]);
        }
    }

    static Vector3 EixoDoComprimento(Transform osso)
    {
        if (osso == null || osso.childCount == 0) return Vector3.up;
        Vector3 p = osso.GetChild(0).localPosition;
        Vector3 a = new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
        if (a.x >= a.y && a.x >= a.z) return Vector3.right;
        if (a.y >= a.z) return Vector3.up;
        return Vector3.forward;
    }

    void LateUpdate()
    {
        if (grupos == null || eixos == null) return;
        for (int g = 0; g < grupos.Length; g++)
        {
            Grupo gr = grupos[g];
            float c = gr.comprimento;
            if (gr.ossosDeCima != null)
                foreach (Transform o in gr.ossosDeCima)
                    if (o != null) o.localScale = Vector3.one * c;

            if (gr.ossosDoMeio == null) continue;
            for (int i = 0; i < gr.ossosDoMeio.Length && i < eixos[g].Length; i++)
            {
                Transform m = gr.ossosDoMeio[i];
                if (m == null) continue;
                Vector3 e = eixos[g][i];
                //comprimento continua (ja diminuiu pelo pai); a grossura volta para "grossura"
                m.localScale = e * 1f + (Vector3.one - e) * (gr.grossura / c);
            }
        }
    }
}
