# Mundo de Penny

Jogo 2D de aventura narrativa desenvolvido em Godot com C#. Explore os cenários, converse com personagens, colete itens e enfrente ondas de slimes.

## Requisitos

- Godot 4.7.2 com suporte a C#/.NET
- .NET 8 SDK

## Como executar

1. Abra `project.godot` no Godot 4.7.2 .NET.
2. Execute o projeto com **F6** (ou **F5** para iniciar a cena principal).

Para compilar os scripts C# pela linha de comando:

```powershell
dotnet build "Mundo de Penny.csproj"
```

## Controles

| Ação | Teclas |
| --- | --- |
| Mover | WASD ou setas |
| Interagir | E |
| Atacar | J |
| Dash | Shift |
| Voltar/fechar uma tela | Esc |

O menu principal oferece **Jogar**, **Opções** e **Sair**. Em **Opções**, é possível ajustar o volume, alternar tela cheia e abrir **Créditos**.

## Conteúdo

- Prólogo, diálogos e cenas de exploração.
- Combate com projéteis, dash, slimes e itens colecionáveis.
- Ondas de inimigos em `outside` e `outside_dark`; a área escura inclui estrelas e um chefe.
- Encontro com Emmi após o chefe e transição para a cena final.

## Música

As faixas incluídas no projeto são usadas nestes momentos:

| Faixa | Momento |
| --- | --- |
| `Doll Dance.wav` | Prólogo |
| `Evans Full.wav` | Menu principal |
| `Splatters.wav` | Ondas em `outside` |
| `Death.wav` | Conclusão das ondas |
| `Letting Go Main.wav` | `outside_dark`, antes do chefe |
| `Lurking.wav` | Chefe de `outside_dark` |
| `In Legends.wav` | Game over |
| `I'll be Here (Full).wav` | Cena final |

## Créditos e licenças de áudio

**Visual Novel Audio Pack**

Compositor e sound designer: **Tim Reichert**

“I'll be Here” — vocal e letra: **Eliana Z.**

As músicas do pacote usadas neste projeto estão sob a licença **CC BY-NC 4.0**: é necessário dar crédito e o uso é restrito a projetos não comerciais. A licença permite adaptações, mas não o uso comercial das faixas originais ou adaptadas.

- [Creative Commons Attribution-NonCommercial 4.0](https://creativecommons.org/licenses/by-nc/4.0/)
- A licença de efeitos de interface e transições do pacote, quando utilizados, é [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

Esta atribuição cobre somente o áudio do Visual Novel Audio Pack; outros recursos do projeto podem ter licenças próprias.
