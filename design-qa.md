# Design QA — tema de marca no app e cadastro

- Referência visual principal: `C:\Users\ALEXSS~1\AppData\Local\Temp\codex-clipboard-8fa923f0-5ca4-4d3c-9b1b-7798e9490318.png`
- Referências das telas corrigidas: `codex-clipboard-f4239a9d-d202-4eb4-8519-afa426234892.png`, `codex-clipboard-f6c3616a-488d-4b75-847e-bd9efe75155b.png`, `codex-clipboard-7b5c090a-cf74-4f52-9817-61f11dc45eb0.png`, `codex-clipboard-8a0943da-afef-4610-8c04-e1a76cbeba0f.png` e `codex-clipboard-c3b0cb37-aa5c-4db8-9c60-a53f95c24463.png`.
- Evidências locais: `.codex/qa/login-desktop-brand.png`, `.codex/qa/login-mobile-brand.png` e `.codex/qa/signup-neutral-fallback.png`.
- Quadro de comparação: `.codex/qa/login-reference-comparison.png`.
- Viewports verificados: 1559 x 729 px e 390 x 844 px.

## Resultado

- O preto/grafite do login agora é a superfície dominante no app e no cadastro.
- Turquesa ficou reservado para foco, confirmação e estado positivo; dourado para ação principal e seleção.
- Cards de pedidos, caixa, horários e cardápio deixaram de usar grandes preenchimentos verdes.
- Estados destrutivos mantêm diferenciação vermelha e não foram neutralizados pelo novo tema.
- Login e cadastro usam somente campos `type="password"`; os controles "Ver", "Mostrar" e "Ocultar" foram removidos.
- O login não apresenta overflow horizontal no viewport móvel.
- O cadastro em modo de indisponibilidade da API preserva o mesmo tema e não apresenta overflow.
- Build de produção concluído, incluindo lint e checagem de tipos do Next.js.

## Limitação controlada

- A API local de planos não estava ativa durante a inspeção visual, portanto o cadastro foi renderizado no estado seguro de indisponibilidade. A estrutura completa do formulário foi validada pelo build e os mesmos seletores finais cobrem seus cards, planos, campos e botões.

## Pendências

- Nenhuma diferença P0, P1 ou P2 encontrada no escopo solicitado.
- P3: posições dos elementos decorativos animados variam entre capturas por projeto.

final result: passed
