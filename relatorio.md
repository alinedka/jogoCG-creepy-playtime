# Creepy Playtime — Relatório

Trabalho de Computação Gráfica 
Engine: Unity 6.0 (6000.0.85f1), Universal Render Pipeline (URP)


## Referências

- A mecânica de repetir a sequência de cores veio do jogo de cores de *Poppy Playtime* (Memorial Music) e do brinquedo *Genius*. Usando-os como inspiração.
- O clima de fábrica de brinquedos abandonada também veio de *Poppy Playtime*.
- O monstro é o modelo do jogo *Zoonomaly*, com as proporções modificadas.
- A etapa 4 usa o efeito Stroop: a cor do telão e a palavra escrita não combinam.

## Técnicas de Computação Gráfica usadas

**Cenário e modelagem**
- Cenas montadas no editor com modelos prontos e primitivas (cubos, cilindros, quads): fachada, sala, plataforma, tubo do monstro, tampa do tubo, janelas redondas.
- Instâncias de prefabs (barris, estantes, contêineres, máquinas de garra, brinquedos).

**Materiais e texturas**
- Materiais PBR do URP/Lit: cor base (albedo), mapa normal, metálico e suavidade (smoothness).
- Tiling de texturas em paredes, chão e asfalto.
- Materiais do pacote Alien Toys convertidos de Standard para URP.
- Material Unlit transparente para os símbolos dos botões.

**Iluminação e cor**
- Luzes pontuais na sala; luz direcional no menu.
- Emissão HDR nos botões e telões, combinada com o efeito Bloom.
- Skybox (céu de pôr do sol no menu).
- Névoa linear para dar profundidade e clima de sujeira.

**Pós-processamento (Global Volume)**
- Color Adjustments (filtro creme de "filme antigo" e menos saturação), Vignette, Film Grain, Chromatic Aberration e Bloom.

**Animação**
- Monstro com esqueleto Humanoid: animação de queda (Mixamo) e animação própria do modelo para o jumpscare.
- Ajustes de ossos depois da animação (LateUpdate): encurtar braços e pernas, virar a cabeça para a câmera, patada procedural esticando os braços até o jogador.
- Interpolações (Lerp, Slerp, SmoothStep e curvas exponenciais) em movimentos de câmera, mãos, tampa e monstro.

**Interação e física**
- Raycast do centro da tela para mirar os botões; o clique só vale pela frente do botão.
- Mãos esticáveis: o cabo é um cilindro escalado entre o punho e a mão a cada frame.
- Colisões em cápsulas presas aos ossos do monstro, que acompanham a animação.
- Caminhar com detecção de obstáculos por altura (subir em caixas e barris, escorregar dos bichinhos) e colisão da cabeça com o teto.

**Efeitos**
- Sistema de partículas de poeira na sala.
- Tremor de câmera no jumpscare.

**Áudio**
- Sons 2D e 3D, com variação de tom (pitch) a cada toque.
- Filtros de áudio (passa-baixa e eco) para a voz parecer sair de dentro do brinquedo.

**Interface**
- TextMeshPro em 2D (HUD, menus) e 3D (placa do título, texto dos telões), com contorno no texto.
- Menu com painéis (como jogar, recordes, nome do jogador), animação de hover nos botões e tela de morte com fade.

## Créditos

### Modelos e animações

| Recurso | Autor / origem | Licença |
|---|---|---|
| Monstro ("Zoonomaly Monster 2 Rig") | NO DONT EAT ME CASEOH (Sketchfab) — https://sketchfab.com/3d-models/zoonomaly-monster-2-rig-cc97e7b14b3444d383c7d8f2183fd708. Personagem do jogo *Zoonomaly*, com as proporções modificadas | CC BY 4.0 |
| Animação "Falling" | Adobe Mixamo | Uso livre em projetos (termos do Mixamo) |
| Ventilation duct set - Low poly | MatthewWorksSF (Sketchfab) | CC BY 4.0 (modificado: materiais envelhecidos) |
| Sewer Pipe (tubo do monstro) | Brandon Funk (Sketchfab) — https://sketchfab.com/3d-models/sewer-pipe-3b6cc4c654a14593b8ce907361352d62 | CC BY 4.0 |
| Claw Machine Free (máquinas de garra e pelúcias) | ithappy (Unity Asset Store) | Licença padrão da Asset Store |
| Horror Plush Toys - Spooky Alien Mascots (pasta Alien_Toys) | Serhats Marketplace — Unity Asset Store | Licença padrão da Asset Store |
| Factory Training Industry Sample (barris, estantes, contêineres, cones) | Unity Technologies | Licença padrão da Asset Store |
| FREE Industrial building/Garage (portão da fachada) | Reinforcedcc — Unity Asset Store | Licença padrão da Asset Store |
| Low-poly Industrial Pack (tanques, telhados, chão e teto) | Donthrukat — Unity Asset Store | Licença padrão da Asset Store |
| European window pack vol 01 (materiais das janelas redondas) | Track11 — Unity Asset Store | Licença padrão da Asset Store |

### Texturas e céu

| Recurso | Autor / origem | Licença |
|---|---|---|
| Yughues Free Sand Materials (areia do chão) | Nobiax / Yughues — Unity Asset Store | Licença padrão da Asset Store |
| Textures FREE (pasta TexturesPart01, asfalto) | Objective Environment — Unity Asset Store | Licença padrão da Asset Store |
| Floors and Walls Vol 5 (parede de pedra da sala) | Unimodels — Unity Asset Store | Licença padrão da Asset Store |
| Tileable Bricks Wall (tijolos) | Game-Ready Studios — Unity Asset Store | Licença padrão da Asset Store |
| Stylized Skyboxes FREE (céu do menu) | Staggart Creations — Unity Asset Store | Licença padrão da Asset Store |

### Fontes

| Fonte | Autor | Licença |
|---|---|---|
| Bangers | Vernon Adams (Google Fonts) | SIL Open Font License |
| Jolly Lodger | Font Diner (Google Fonts) | SIL Open Font License |
| VT323 | Peter Hull (Google Fonts) | SIL Open Font License |

### Sons

| Som no jogo | Arquivo | Origem |
|---|---|---|
| Música da sala | capaholiczsfx-toy-store-atmosphere | Pixabay (capaholiczsfx) |
| Música do menu | somdefundo.mp3 | Pixabay (alex_jauk) — industrial ambience |
| Dica do telão | trocadecor.mp3 | Pixabay (soundshelfstudio) — sci-fi system blip |
| Mão esticando | maoesticando2.mp3 | Pixabay (freesound_community) — servo noises |
| Acerto / erro | success.mp3 / error.mp3 | Pixabay (freesound_community / dragon-studio) |
| Apertar botão | apertarbotao.mp3 | Pixabay (spinopel) — power off button |
| Bote do monstro | bote.mp3 | Pixabay (nastynewfie) — monster growl 2 |
| Grito do jumpscare | grito.mp3 | Pixabay (jusatti890) — scream horror sfx |
| Garra no bote | garra.wav | Pixabay (yodguard) — scorpion claw attack 2, editado no Clipchamp e cortado |
| Dor ao perder vida | freesound_community-male_hurt7 | Pixabay (freesound_community) |
| Tampa do tubo | 697758 metal resonant scrape | Freesound (sounddesignforyou) — CC0 (domínio público) |
| Apertar pelúcia | floraphonic tiny/baby squeak toy | Pixabay (floraphonic) |
| Apertar pelúcia | skiffy2_cortado.wav | Pixabay (freesound_community) — editado (cortado) |
| Voz "I love you" | voz_iloveyou.wav | Pixabay (shut_up_ghost) — editado (cortado) |
| Risada | dragon-studio creepy laugh 2 | Pixabay (dragon-studio) |
| Voz "I'm scared" | voz_imscared.wav | Pixabay (bubuchickz) — editado (cortado e mais alto) |
| Sons da máquina de garra | Claw_Movement, Claw_Down, Toy_Drop, Lose_Sound, Win_Sound, Button | pacote Claw Machine Free (ithappy) |

### Ferramentas

- Unity 6 (URP, Input System, TextMeshPro, glTFast para importar os modelos .glb).
- Photopea (edição das imagens dos símbolos).
