# Creepy Playtime

Trabalho de Computação Gráfica — SENAC.

Jogo de memória em primeira pessoa, com clima de terror, dentro de uma fábrica de brinquedos abandonada.

O jogador fica no centro de uma plataforma redonda, cercado de botões. Os telões das quatro paredes mostram uma sequência de cores, e o jogador repete a sequência esticando as mãos até os botões. Em cima dele, dentro de um tubo no teto, um monstro desce aos poucos. Errar ou demorar faz o monstro dar o bote, e perder as três vidas termina em um jumpscare.
## Baixar e jogar (Windows)

1. Baixe o **Creepy Playtime.zip** na página de [Releases](https://github.com/alinedka/jogoCG-creepy-playtime/releases/latest).
2. Clique com o botão direito no arquivo e escolha **Extrair tudo**.
3. Abra a pasta extraída e dê dois cliques em **Creepy Playtime.exe**.

Se o Windows mostrar "O Windows protegeu o computador", clique em **Mais informações → Executar assim mesmo**. O aviso aparece porque o jogo não tem assinatura digital.

## Abrir o projeto no Unity

1. Instale o **Unity 6.0 (6000.0.85f1)** pelo Unity Hub.
2. Baixe ou clone este repositório. No Unity Hub, clique em **Add → Add project from disk** e escolha a pasta do projeto.
3. Abra o projeto. Na primeira vez o Unity demora alguns minutos importando tudo.
4. Abra a cena `Assets/Scenes/Menu.unity` (ou use o menu **Cenas → Jogar a partir do Menu**, atalho Alt+Shift+0).
5. Clique em **Play**.

Para gerar o executável: **File → Build Profiles**, plataforma **Windows**, e clique em **Build**. As cenas Menu e SampleScene já estão na lista.

## Como jogar

| Comando | Ação |
|---|---|
| Clique esquerdo / direito | lança a mão esquerda / direita até o ponto da mira |
| W A S D + Espaço | andar e pular pela sala (só antes de começar ou depois do fim de jogo) |
| Esc | solta o mouse |
| M | volta ao menu |

No menu, clique em **Jogar**, digite seu nome e clique em **Começar**. Na sala, aperte qualquer botão para começar a partida. A tampa do tubo abre e o monstro aparece.

Os telões mostram a sequência. Quando aparecer **SUA VEZ**, repita a sequência apertando os botões na mesma ordem. A cada acerto a sequência ganha uma cor e o monstro sobe um pouco. Cada etapa começa com 3 cores e vai até 6; na etapa 4 não tem limite. Errar ou demorar tira uma vida.

### Etapas

1. **Cores:** 4 botões; o telão mostra a cor com o nome escrito.
2. **Cores:** surgem 2 botões novos atrás do jogador (violeta e laranja).
3. **Cores e símbolos:** surgem 3 botões pretos com símbolos.
4. **Palavras e símbolos:** o telão acende em uma cor e escreve o nome de outra; vale o que está escrito. Essa etapa não tem fim.

Os pontos vão para uma tabela de recordes com o nome do jogador, que aparece no menu em **Recordes**.

### Pontos

Cada sequência completa vale **número de cores × 10 × número da etapa**. Errar não tira pontos.

| Etapa | Sequências | Pontos de cada uma | Total da etapa |
|---|---|---|---|
| 1 | 3, 4, 5, 6 cores | 30, 40, 50, 60 | 180 |
| 2 | 3, 4, 5, 6 cores | 60, 80, 100, 120 | 360 |
| 3 | 3, 4, 5, 6 cores | 90, 120, 150, 180 | 540 |
| 4 | 3, 4, 5, 6, 7... | 120, 160, 200, 240, 280... | sem fim |

Quem passa as três primeiras etapas chega na etapa 4 com 1080 pontos. Não tem como zerar o jogo: o objetivo é durar o máximo possível e entrar no top 10 dos recordes.

### Tempo

O monstro só desce na vez do jogador; enquanto o telão mostra a sequência, ele fica parado. Se ele chegar até o jogador, conta como erro ("DEMOROU!").

O tempo para ele chegar é **12 s (8% menor a cada rodada, nunca abaixo de 6 s) + 0,8 s por cor da sequência**.

| Rodada | Etapa | Cores | Tempo |
|---|---|---|---|
| 1 | 1 | 3 | 14,4 s |
| 5 | 2 | 3 | 11,0 s |
| 9 | 3 | 3 | 8,6 s |
| 13 em diante | 4 | 3, 4, 5... | 8,4 s, 9,2 s, 10 s... |

O relógio não zera a cada rodada:
- cada botão certo devolve 12% do tempo;
- cada sequência completa devolve 50% do tempo;
- quem foi devagar começa a próxima rodada com o monstro um pouco mais perto.

### Vidas

São **3 vidas**, e não dá para ganhar vida de volta. Errar um botão ou demorar tira uma vida: a câmera vira para o monstro, ele dá o bote, e o telão repete a mesma sequência. A cada erro o monstro fica 30% mais baixo de vez. Na terceira vida perdida vem o jumpscare e o fim de jogo.

### Easter egg

Fora da partida dá para andar pela sala. Nas três máquinas de garra dá para tentar pegar um bichinho: o primeiro clique movimenta a garra e o segundo faz ela descer. O primeiro bichinho desbloqueia uma conquista, e os bichinhos ganhos ficam na plataforma e "falam" quando são tocados.

## Pastas do projeto

- `Assets/Scenes`: cenas do menu e da sala.
- `Assets/Scripts`: códigos do jogo.
- `Assets/Monstro`: modelo, animações e Animator do monstro.
- `Assets/Materiais Menu` e `Assets/Materiais Sala`: nossos materiais.
- `Assets/Sons`, `Assets/Simbolos`, `Assets/Fonts`: sons, imagens e fontes.
- `Assets/Pacotes`: modelos e texturas baixados (créditos no relatório).
