## ADDED Requirements

### Requirement: Formulário é bloqueado completamente durante processamento
O formulário SHALL ser envolvido por `<fieldset [disabled]="isProcessing">`, desabilitando todos os controles (inputs, selects, botão "Gerar Post") enquanto `view === 'progress'`.

#### Scenario: Campos desabilitados durante processamento
- **WHEN** o usuário submete o formulário e `view` muda para `'progress'`
- **THEN** todos os inputs e o botão "Gerar Post" ficam desabilitados (atributo `disabled` presente)

#### Scenario: Campos reabilitados após reset
- **WHEN** o usuário clica em "Gerar Novo Post" ou "Cancelar"
- **THEN** `view` volta para `'form'` e todos os campos ficam habilitados

### Requirement: Botão "Cancelar" visível e funcional em todas as etapas
A view de progresso SHALL exibir um botão "Cancelar" durante qualquer etapa, incluindo a pausa do modo Consultado. O clique SHALL chamar `DELETE /api/workflow/{sessionId}` e retornar ao formulário com todos os dados preservados.

#### Scenario: Cancelamento durante transcrição
- **WHEN** o usuário clica em "Cancelar" enquanto a etapa de transcrição está `in_progress`
- **THEN** `WorkflowService.cancel(sessionId)` é chamado
- **THEN** `view` retorna para `'form'` com `url`, `postType` e `mode` preservados

#### Scenario: Cancelamento durante pausa do modo consultado
- **WHEN** o usuário clica em "Cancelar" enquanto `consultedQuestions` está visível
- **THEN** `WorkflowService.cancel(sessionId)` é chamado
- **THEN** `view` retorna para `'form'` com todos os dados preservados

### Requirement: Retry diferenciado por errorCode
A lógica de retry no frontend SHALL distinguir entre erros que preservam a URL e erros que limpam a URL.

#### Scenario: Retry com errorCode no_transcript limpa URL
- **WHEN** o evento `{ errorCode: "no_transcript" }` é recebido
- **THEN** ao clicar em "Tentar com outro vídeo" o campo `url` é limpo
- **THEN** `postType` e `mode` são preservados

#### Scenario: Retry com errorCode video_inaccessible preserva URL
- **WHEN** o evento `{ errorCode: "video_inaccessible" }` é recebido
- **THEN** ao clicar em "Tentar Novamente" o campo `url` é preservado
- **THEN** `postType` e `mode` são preservados

#### Scenario: Retry com errorCode llm_error preserva todos os dados
- **WHEN** o evento `{ errorCode: "llm_error" }` é recebido
- **THEN** ao clicar em "Tentar Novamente" todos os dados do formulário são preservados

#### Scenario: session_expired retorna ao formulário sem botão de retry
- **WHEN** o evento `{ errorCode: "session_expired" }` é recebido
- **THEN** `view` retorna automaticamente ao formulário com todos os dados preservados

### Requirement: Botão de ação do erro é contextual ao errorCode
A UI SHALL exibir texto de botão e ação de retry diferenciados conforme o `errorCode` recebido.

#### Scenario: no_transcript exibe botão "Tentar com outro vídeo"
- **WHEN** `errorCode === 'no_transcript'`
- **THEN** o botão de retry exibe "Tentar com outro vídeo" e limpa o campo URL ao clicar

#### Scenario: Demais erros exibem "Tentar Novamente"
- **WHEN** `errorCode` é `video_inaccessible` ou `llm_error` ou `cancelled`
- **THEN** o botão de retry exibe "Tentar Novamente" e preserva a URL

### Requirement: Reset completo limpa todos os estados
"Gerar Novo Post" SHALL limpar `url`, `postType`, `mode`, `currentSessionId`, `postDraft`, `consultedQuestions` e `lastError` sem recarregar a página.

#### Scenario: Estado limpo após reset
- **WHEN** o usuário clica em "Gerar Novo Post"
- **THEN** todos os campos do formulário ficam vazios
- **THEN** `view === 'form'` e nenhum dado da sessão anterior é visível

### Requirement: WorkflowService expõe método cancel
`WorkflowService` SHALL expor `cancel(sessionId: string): Observable<void>` que realiza `DELETE /api/workflow/{sessionId}`.

#### Scenario: Cancel envia DELETE para o backend
- **WHEN** `workflowService.cancel(sessionId)` é chamado
- **THEN** uma requisição `DELETE /api/workflow/{sessionId}` é enviada ao backend

### Requirement: SignalRService expõe campo errorCode no tipo WorkflowEvent
O tipo `WorkflowEvent` em `signalr.service.ts` SHALL incluir `errorCode?: string` para que componentes consumidores possam usar o código sem acesso ao `message`.

#### Scenario: errorCode disponível no tipo
- **WHEN** um evento de erro é recebido pelo SignalR
- **THEN** `event.errorCode` está disponível como `string | undefined` no handler
