using System.Collections.Generic;
using UnityEngine;

//bichinhos pegos na maquina de garra, so na memoria enquanto o jogo esta aberto
public static class Conquistas
{
    static readonly List<string> pegos = new List<string>();

    //comeca vazio a cada play e a cada jogador novo no menu
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Zerar()
    {
        pegos.Clear();
        PlayerPrefs.DeleteKey("bichinhosPegos"); //versao antiga salvava no computador
    }

    //posicao do bichinho na lista (-1 = ainda nao foi pego)
    public static int Indice(string nome) => pegos.IndexOf(nome);

    public static int Quantidade => pegos.Count;

    //guarda um bichinho pego e devolve true se foi o primeiro
    public static bool Adicionar(string nome)
    {
        bool primeiro = pegos.Count == 0;
        if (!pegos.Contains(nome)) pegos.Add(nome);
        return primeiro;
    }
}
