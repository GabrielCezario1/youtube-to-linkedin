# PRD — Tech Content Agent

> **Público:** Desenvolvedores e Área de Negócio
> **Módulo:** Tech Content Agent
> **Versão:** 1.0
> **Data:** 2026-05-24

---

## Visão Geral

O **Tech Content Agent** é uma ferramenta pessoal de produtividade que transforma vídeos do YouTube em rascunhos de posts prontos para publicação no LinkedIn. O sistema foi projetado para um desenvolvedor solo que consome conteúdo técnico em vídeo e deseja reaproveitar esse aprendizado para construir sua presença profissional, sem gastar tempo na escrita manual de posts.

O usuário informa a URL de um vídeo do YouTube, o tipo de post desejado e o nível de participação que quer ter na criação. O sistema extrai automaticamente a transcrição do vídeo, identifica os pontos mais relevantes e produz um rascunho de post no formato adequado ao tipo escolhido. O resultado final é um texto pronto para revisão e publicação direta no LinkedIn.

O sistema opera em dois modos: no modo **Auto**, a inteligência artificial toma todas as decisões de estrutura e contexto e entrega o melhor rascunho possível; no modo **Consultado**, o sistema pausa após processar o vídeo e apresenta perguntas ao usuário para enriquecer o post antes de gerá-lo, com liberdade total para responder apenas o que desejar.

---

## Pré-requisitos

Não há guard de acesso nem autenticação. O sistema é de uso pessoal e opera localmente. O único pré-requisito funcional é que o vídeo informado seja público e possua transcrição disponível no YouTube. Caso contrário, o sistema informa o motivo e permite nova tentativa.

---

## Funcionalidade 1 — Formulário de Entrada

O formulário de entrada é o ponto de partida da experiência. Ele reúne em uma única tela as três informações necessárias para iniciar o processamento: o link do vídeo, o tipo de post desejado e o modo de participação. O usuário preenche os campos e aciona a geração — a partir daí, o sistema assume o controle do fluxo.

---

### 1.1 Campo de URL do YouTube

O usuário insere a URL completa de um vídeo público do YouTube com transcrição disponível.

```gherkin
Dado que o usuário acessa a tela principal
Quando o usuário insere uma URL válida do YouTube no campo correspondente
Então o campo aceita a entrada sem validação imediata
E a URL é submetida ao sistema no momento em que o usuário aciona "Gerar Post"
Exemplo: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
```

```gherkin
Dado que o usuário aciona "Gerar Post"
E o campo de URL está vazio ou contém uma URL inválida
Então o sistema exibe uma mensagem de validação descritiva próxima ao campo
E o processamento não é iniciado
```

---

### 1.2 Seleção do Tipo de Post

O usuário escolhe entre três formatos de post, cada um com estrutura e propósito distintos: **Storytelling** (experiência pessoal e aprendizado vivido), **Lista Prática** (conjunto de dicas, erros ou ferramentas) e **Opinião Provocativa** (questionamento de uma crença comum do mercado).

```gherkin
Dado que o usuário acessa a tela principal
Quando o usuário visualiza o seletor de tipo de post
Então as três opções são exibidas: Storytelling, Lista Prática e Opinião Provocativa
E nenhuma opção está pré-selecionada por padrão
```

```gherkin
Dado que o usuário aciona "Gerar Post"
E nenhum tipo de post foi selecionado
Então o sistema exibe uma mensagem de validação descritiva próxima ao seletor
E o processamento não é iniciado
```

---

### 1.3 Seleção do Modo de Participação

O usuário escolhe entre o modo **Auto** (a IA decide tudo) e o modo **Consultado** (o sistema pausa e faz perguntas antes de gerar o post).

```gherkin
Dado que o usuário acessa a tela principal
Quando o usuário visualiza o seletor de modo
Então as duas opções são exibidas: "Auto" e "Consultado"
E uma descrição curta de cada modo é exibida abaixo da opção para orientar a escolha
E nenhuma opção está pré-selecionada por padrão
```

```gherkin
Dado que o usuário aciona "Gerar Post"
E nenhum modo foi selecionado
Então o sistema exibe uma mensagem de validação descritiva próxima ao seletor
E o processamento não é iniciado
```

---

### 1.4 Acionamento do Processamento

Com os três campos preenchidos, o usuário aciona o botão "Gerar Post" para iniciar o workflow.

```gherkin
Dado que o usuário preencheu a URL, selecionou o tipo de post e o modo
Quando o usuário aciona "Gerar Post"
Então o formulário é bloqueado para edição
E o botão "Gerar Post" é desabilitado durante todo o processamento
E a interface transita para a área de progresso em tempo real
```

---

## Funcionalidade 2 — Acompanhamento de Progresso em Tempo Real

Após o acionamento, o sistema exibe o progresso do processamento em tempo real, permitindo ao usuário acompanhar cada etapa do workflow sem precisar aguardar em uma tela estática.

---

### 2.1 Etapas do Progresso

O progresso é exibido como uma lista de etapas sequenciais, cada uma com estado visual próprio: **em andamento**, **concluída** ou **com erro**.

As etapas exibidas são:
1. Extraindo transcrição do vídeo
2. Resumindo conteúdo
3. Gerando rascunho do post

```gherkin
Dado que o usuário acionou "Gerar Post" com dados válidos
Quando o sistema inicia o processamento
Então a etapa "Extraindo transcrição do vídeo" é exibida como "em andamento"
E as etapas seguintes são exibidas como pendentes
```

```gherkin
Dado que a etapa "Extraindo transcrição" foi concluída com sucesso
Quando o sistema avança para a próxima etapa
Então a etapa "Extraindo transcrição" é marcada como "concluída"
E a etapa "Resumindo conteúdo" passa para o estado "em andamento"
```

---

### 2.2 Cancelamento durante o Processamento

O usuário pode cancelar o processamento a qualquer momento antes da conclusão.

```gherkin
Dado que o processamento está em andamento
Quando o usuário aciona o botão "Cancelar"
Então o processamento é interrompido
E a interface retorna ao formulário de entrada com os valores anteriores preservados
```

---

## Funcionalidade 3 — Perguntas de Contexto (Modo Consultado)

No modo Consultado, após a conclusão da etapa de resumo, o sistema pausa o workflow e exibe um conjunto de perguntas ao usuário antes de gerar o post. As perguntas são compostas por uma base fixa definida pelo tipo de post selecionado mais perguntas adicionais que a IA pode adicionar com base no conteúdo específico do vídeo.

> ⚠️ Observação: O número máximo de perguntas dinâmicas adicionadas pela IA não foi definido. Recomenda-se limitar a 3 perguntas extras para não sobrecarregar o usuário.

---

### 3.1 Exibição das Perguntas

```gherkin
Dado que o usuário selecionou o modo Consultado
E a etapa de resumo foi concluída com sucesso
Quando o sistema está pronto para gerar o post
Então o progresso é pausado na etapa "Gerando rascunho do post"
E um card de perguntas é exibido abaixo das etapas de progresso
E cada pergunta é apresentada individualmente com campo de resposta em texto livre
E uma indicação visual deixa claro que todas as perguntas são opcionais
```

---

### 3.2 Resposta e Continuação

```gherkin
Dado que o card de perguntas está exibido
Quando o usuário responde algumas perguntas (ou nenhuma) e aciona "Continuar"
Então o sistema retoma o workflow com as respostas fornecidas
E a etapa "Gerando rascunho do post" avança para o estado "em andamento"
E o card de perguntas é ocultado
```

```gherkin
Dado que o card de perguntas está exibido
Quando o usuário aciona "Continuar" sem responder nenhuma pergunta
Então o sistema retoma normalmente
E a IA gera o post com base apenas no resumo do vídeo
```

---

## Funcionalidade 4 — Rascunho do Post

Após a conclusão do workflow, o sistema exibe o rascunho do post gerado pela IA. O rascunho segue a estrutura e as regras de formatação definidas pelo tipo de post selecionado, incluindo hook, corpo, insight principal e chamada para ação.

---

### 4.1 Exibição do Rascunho

```gherkin
Dado que o workflow foi concluído com sucesso
Quando a etapa "Gerando rascunho do post" é marcada como concluída
Então um card com o rascunho completo do post é exibido abaixo das etapas de progresso
E o rascunho exibe o texto formatado com espaçamento entre blocos
E o tipo de template utilizado é informado ao usuário (ex: "Template: Lista Prática")
```

---

### 4.2 Cópia do Rascunho

```gherkin
Dado que o rascunho está exibido
Quando o usuário aciona o botão "Copiar"
Então o conteúdo completo do post é copiado para a área de transferência
E uma confirmação visual temporária é exibida no botão (ex: "Copiado!")
```

---

### 4.3 Geração de Novo Post

Após visualizar o rascunho, o usuário pode iniciar uma nova geração com um vídeo diferente.

```gherkin
Dado que o rascunho está exibido
Quando o usuário aciona "Gerar Novo Post"
Então a interface é reiniciada ao estado inicial do formulário
E todos os campos são limpos
E o rascunho anterior é descartado
```

---

## Funcionalidade 5 — Tratamento de Erros

O sistema identifica e comunica falhas de forma descritiva, permitindo ao usuário entender o que ocorreu e tentar novamente sem precisar recarregar a página.

---

### 5.1 Vídeo Privado ou Inacessível

```gherkin
Dado que o usuário informou uma URL de vídeo privado ou removido
Quando o sistema tenta extrair a transcrição
Então a etapa "Extraindo transcrição do vídeo" é marcada com erro
E a mensagem "Não foi possível acessar este vídeo. Verifique se ele é público e tente novamente." é exibida
E um botão "Tentar Novamente" é exibido, retornando ao formulário com a URL preservada
```

---

### 5.2 Vídeo sem Transcrição Disponível

```gherkin
Dado que o usuário informou uma URL de vídeo sem transcrição ou legenda disponível
Quando o sistema tenta extrair a transcrição
Então a etapa "Extraindo transcrição do vídeo" é marcada com erro
E a mensagem "Este vídeo não possui transcrição disponível. Tente com outro vídeo." é exibida
E um botão "Tentar com outro vídeo" é exibido, retornando ao formulário com o campo de URL limpo
```

---

### 5.3 Falha na Geração pela IA

```gherkin
Dado que a transcrição foi extraída com sucesso
Quando ocorre uma falha durante o resumo ou a geração do post
Então a etapa com falha é marcada com erro
E a mensagem "Ocorreu um erro ao processar o conteúdo. Tente novamente." é exibida
E um botão "Tentar Novamente" é exibido, reiniciando o workflow a partir da etapa com falha
```

---

## Comportamentos Gerais

| Comportamento | Descrição |
|---|---|
| **Bloqueio do formulário** | O formulário é desabilitado durante todo o processamento para evitar alterações acidentais |
| **Carregamento progressivo** | Cada etapa do workflow atualiza a interface em tempo real, sem necessidade de recarregar a página |
| **Falha parcial** | Cada etapa do workflow pode falhar independentemente; o erro é exibido na etapa específica sem apagar o progresso anterior |
| **Sem persistência** | Nenhum dado é salvo entre sessões — ao recarregar a página, a interface retorna ao estado inicial |
| **Modo Consultado — perguntas opcionais** | Todas as perguntas do modo Consultado são opcionais; o usuário pode continuar sem responder nenhuma |
| **Reinício limpo** | A ação "Gerar Novo Post" restaura completamente o estado inicial da interface |

---

## Resumo das Seções

```
┌─────────────────────────────────────────────────────────────┐
│                    Tech Content Agent                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   [URL do YouTube                                      ]    │
│                                                             │
│   Tipo de Post: ( ) Storytelling  ( ) Lista  ( ) Opinião   │
│                                                             │
│   Modo:         ( ) Auto          ( ) Consultado            │
│                                                             │
│                              [ Gerar Post ▶ ]              │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   ✅ Extraindo transcrição do vídeo                         │
│   ✅ Resumindo conteúdo                                     │
│   ⏳ Gerando rascunho do post...          [ Cancelar ]      │
│                                                             │
├──────────────────────────────── [apenas modo consultado] ───┤
│                                                             │
│   ❓ Qual foi o aprendizado principal?                      │
│   [___________________________________________________]     │
│                                                             │
│   ❓ Para quem é este post?                                 │
│   [___________________________________________________]     │
│                                                             │
│                              [ Continuar ▶ ]               │
│                                                             │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   📝 Rascunho — Template: Lista Prática          [Copiar]  │
│                                                             │
│   5 erros que cometi ao usar Inteligência Artificial        │
│   para escrever código em produção.                         │
│   ...                                                       │
│                                                             │
│                        [ Gerar Novo Post ]                  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Decisões Técnicas

Esta seção registra as decisões de arquitetura e tecnologia definidas durante a fase de exploração do sistema.

| Decisão | Escolha | Justificativa |
|---|---|---|
| **Framework de agentes** | Microsoft Agent Framework (Workflow graph) | Processo com passos bem definidos e múltiplos agentes coordenados — Workflow é o padrão indicado pelo próprio framework |
| **Orquestração** | Workflow com 3 nós: TranscriptNode → SummaryNode → LinkedInWriterNode | Ordem de execução explícita, fácil de depurar, suporte nativo a human-in-the-loop via checkpointing |
| **Extração de transcrição** | `YoutubeTranscriptApi` (NuGet) | Gratuito, sem necessidade de OAuth ou conta Google, fácil integração em .NET |
| **LLM** | Azure OpenAI / OpenAI (GPT-4o ou GPT-4o-mini) | Suporte nativo no Microsoft Agent Framework; flexibilidade para trocar de provedor |
| **Comunicação frontend-backend** | SignalR | Necessário para streaming de progresso em tempo real e human-in-the-loop (pausa e retomada do workflow) |
| **Frontend** | Angular (SPA, 1 página) | Preferência do usuário; simplicidade máxima para MVP |
| **Backend** | .NET 9 Minimal API | Leveza e compatibilidade com o Microsoft Agent Framework |
| **Persistência** | In-memory (sem banco de dados) | MVP sem necessidade de histórico; estado de sessão gerenciado em memória |
| **Regras de geração do post** | Skill `criar-post-linkedin` como system prompt do LinkedInWriterNode | Centraliza as regras de templates, formatação, SEO e tom em um único lugar |
| **Vídeos longos** | Sem tratamento especial (MVP) | Fora do escopo do MVP; contexto longo do modelo cobre a maioria dos casos |
| **Autenticação** | Sem autenticação | Uso pessoal, ferramenta local |

