## ADDED Requirements

### Requirement: Eventos de erro incluem campo errorCode semântico
Todos os eventos SignalR com `status: "error"` SHALL incluir um campo `errorCode` com um dos valores definidos: `invalid_url`, `video_inaccessible`, `no_transcript`, `llm_error`, `session_expired`, `cancelled`.

#### Scenario: Evento de vídeo inacessível contém errorCode correto
- **WHEN** `TranscriptExecutor` captura `VideoUnavailableException`
- **THEN** emite `{ step: "transcript", status: "error", errorCode: "video_inaccessible", message: "Não foi possível acessar este vídeo. Verifique se é público e tente novamente." }`

#### Scenario: Evento de sem transcrição contém errorCode correto
- **WHEN** `TranscriptExecutor` detecta que o vídeo não tem legendas
- **THEN** emite `{ step: "transcript", status: "error", errorCode: "no_transcript", message: "Este vídeo não possui transcrição disponível. Tente com outro vídeo." }`

#### Scenario: Evento de erro LLM contém errorCode correto
- **WHEN** `SummaryExecutor` ou `LinkedInWriterExecutor` captura exceção do OpenAI
- **THEN** emite `{ step: "<summary|writing>", status: "error", errorCode: "llm_error", message: "Ocorreu um erro ao processar o conteúdo. Tente novamente." }`

#### Scenario: Evento de sessão expirada contém errorCode correto
- **WHEN** `WorkflowSessionManager` expira uma sessão consultada por timeout
- **THEN** emite `{ step: "writing", status: "error", errorCode: "session_expired", message: "Sessão expirada. Inicie novamente." }`

#### Scenario: Evento de cancelamento contém errorCode correto
- **WHEN** executor captura `OperationCanceledException`
- **THEN** emite `{ step: "<step_atual>", status: "error", errorCode: "cancelled", message: "Workflow cancelado." }`

### Requirement: Mensagens de erro não expõem stack trace
O sistema SHALL enviar apenas mensagens amigáveis ao usuário nos eventos SignalR de erro. Stack traces, nomes de exceção e detalhes internos SHALL ser logados no servidor mas nunca enviados ao cliente.

#### Scenario: Exceção inesperada não vaza detalhes
- **WHEN** qualquer executor captura uma exceção não mapeada
- **THEN** emite apenas `{ step, status: "error", errorCode: "llm_error", message: "Ocorreu um erro inesperado. Tente novamente." }`
- **THEN** nenhum campo `exception`, `stackTrace` ou `innerMessage` está presente no payload
