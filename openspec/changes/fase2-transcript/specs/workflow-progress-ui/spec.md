## ADDED Requirements

### Requirement: Exibir progresso do fluxo em etapas
O sistema SHALL exibir um componente `WorkflowProgressComponent` com 3 etapas fixas representando as fases do pipeline: extração de transcrição, resumo de conteúdo e geração de post.

#### Scenario: Estado inicial ao iniciar o fluxo
- **WHEN** o usuário submete a URL e o fluxo inicia
- **THEN** o componente exibe a etapa `transcript` como `in_progress` e as etapas `summary` e `post` como `pendente`

#### Scenario: Transcrição concluída
- **WHEN** o evento SignalR `{ step: "transcript", status: "completed" }` é recebido
- **THEN** a etapa `transcript` passa a exibir ícone ✅ e estado `completed`

#### Scenario: Etapa em progresso
- **WHEN** o evento SignalR com `status: "in_progress"` é recebido para qualquer etapa
- **THEN** a etapa correspondente exibe ícone ⏳ e texto com reticências indicando processamento

#### Scenario: Etapa com erro
- **WHEN** o evento SignalR com `status: "error"` é recebido para qualquer etapa
- **THEN** a etapa correspondente exibe ícone ❌ e o `ErrorDisplayComponent` é exibido abaixo da lista de etapas

#### Scenario: Etapa pendente
- **WHEN** uma etapa ainda não foi iniciada
- **THEN** a etapa exibe ícone ○ sem animação

---

### Requirement: Exibir mensagem de erro com opção de retry
O sistema SHALL exibir um `ErrorDisplayComponent` quando qualquer etapa do fluxo falhar, contendo a mensagem descritiva do erro e um botão de ação para o usuário.

#### Scenario: Erro com mensagem e botão retry genérico
- **WHEN** o evento `{ step: "transcript", status: "error", message: "..." }` é recebido
- **THEN** o `ErrorDisplayComponent` MUST exibir um card com ícone ⚠️, a mensagem exata recebida no evento e um botão de ação

#### Scenario: Retry preserva dados do formulário
- **WHEN** o usuário clica no botão de ação do `ErrorDisplayComponent`
- **THEN** o formulário retorna ao estado inicial com a URL e demais campos preenchidos exatamente como estavam antes da submissão

#### Scenario: Mensagem de erro sem transcrição
- **WHEN** a mensagem recebida é "Este vídeo não possui transcrição disponível. Tente com outro vídeo."
- **THEN** o botão exibe o texto "Tentar com outro vídeo"

#### Scenario: Mensagem de erro genérica
- **WHEN** a mensagem recebida é qualquer outra mensagem de erro
- **THEN** o botão exibe o texto "Tentar novamente"
