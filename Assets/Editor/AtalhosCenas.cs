using UnityEditor;
using UnityEditor.SceneManagement;

//so no editor: menu "Cenas" para trocar entre o menu e a sala
public static class AtalhosCenas
{
    const string CenaMenu = "Assets/Scenes/Menu.unity";
    const string CenaJogo = "Assets/Scenes/SampleScene.unity";

    //atalhos com Alt+Shift (Ctrl+Shift+numero ja e usado pelo Unity em "Selection Groups")
    [MenuItem("Cenas/Abrir Menu principal #&1")] //atalho Alt+Shift+1
    static void AbrirMenu() => Abrir(CenaMenu);

    [MenuItem("Cenas/Abrir Sala do jogo #&2")] //atalho Alt+Shift+2
    static void AbrirJogo() => Abrir(CenaJogo);

    [MenuItem("Cenas/Jogar a partir do Menu #&0")] //atalho Alt+Shift+0
    static void JogarDoMenu()
    {
        if (EditorApplication.isPlaying) return;
        //pergunta se quer salvar a cena atual, abre o Menu e aperta Play
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(CenaMenu);
        EditorApplication.isPlaying = true;
    }

    static void Abrir(string caminho)
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Cenas", "Pare o Play antes de trocar de cena.", "OK");
            return;
        }
        //se a cena atual tem mudancas, o Unity pergunta se quer salvar
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(caminho);
    }
}
