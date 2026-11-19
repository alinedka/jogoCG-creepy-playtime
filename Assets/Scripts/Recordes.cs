using System.Collections.Generic;
using UnityEngine;

//top 10 de recordes e nome do jogador salvos no PlayerPrefs
public static class Recordes
{
    [System.Serializable]
    public class Entrada
    {
        public string nome;
        public int pontos;
    }

    [System.Serializable]
    class Tabela
    {
        public List<Entrada> lista = new List<Entrada>();
    }

    const string chaveTabela = "tabelaRecordes";
    const string chaveNome = "nomeJogador";
    public const int MaximoNaTabela = 10;

    //nome digitado no menu
    public static string NomeAtual
    {
        get { return PlayerPrefs.GetString(chaveNome, "JOGADOR"); }
        set
        {
            //sem "<" e ">": senao o TextMeshPro acharia que e uma tag de formatacao
            string limpo = value == null ? "" : value.Replace("<", "").Replace(">", "");
            string nome = string.IsNullOrWhiteSpace(limpo) ? "JOGADOR" : limpo.Trim().ToUpper();
            if (nome.Length > 12) nome = nome.Substring(0, 12);
            PlayerPrefs.SetString(chaveNome, nome);
            PlayerPrefs.Save();
        }
    }

    //lista do maior para o menor
    public static List<Entrada> Lista()
    {
        string json = PlayerPrefs.GetString(chaveTabela, "");
        if (string.IsNullOrEmpty(json)) return new List<Entrada>();
        Tabela t = JsonUtility.FromJson<Tabela>(json);
        return t != null && t.lista != null ? t.lista : new List<Entrada>();
    }

    //maior pontuacao (0 se vazia)
    public static int Melhor()
    {
        var l = Lista();
        return l.Count > 0 ? l[0].pontos : 0;
    }

    //coloca a partida na tabela e devolve a posicao (0 = nao entrou)
    public static int Adicionar(string nome, int pontos)
    {
        var lista = Lista();
        var nova = new Entrada { nome = nome, pontos = pontos };
        lista.Add(nova);
        lista.Sort((a, b) => b.pontos.CompareTo(a.pontos)); //maior primeiro
        if (lista.Count > MaximoNaTabela) lista.RemoveRange(MaximoNaTabela, lista.Count - MaximoNaTabela);

        PlayerPrefs.SetString(chaveTabela, JsonUtility.ToJson(new Tabela { lista = lista }));
        PlayerPrefs.Save();

        int posicao = lista.IndexOf(nova);
        return posicao >= 0 ? posicao + 1 : 0;
    }

    //texto da tabela: posicao, nome e pontos
    public static string TextoDaTabela()
    {
        var lista = Lista();
        if (lista.Count == 0) return "<align=center>Ninguém jogou ainda.\nSeja o primeiro!</align>";

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < lista.Count; i++)
            sb.Append("<pos=4%>").Append(i + 1).Append('.')
              .Append("<pos=16%>").Append(lista[i].nome)
              .Append("<pos=78%>").Append(lista[i].pontos).Append(" pts\n");
        return sb.ToString();
    }
}
