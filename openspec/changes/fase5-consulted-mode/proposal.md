## Why

O modo Automático gerado na Fase 4 produz posts genéricos sem contexto pessoal do criador. O modo Consultado introduz uma pausa human-in-the-loop após o resumo, permitindo que o usuário forneça contexto específico (obstáculos, aprendizados, público-alvo) antes da geração do post, tornando o conteúdo mais autêntico e personalizado.

## What Changes

- Novo mecanismo de **pausa do workflow** via `RequestPort` (Agent Framework) dentro do `LinkedInWriterExecutor`
- `LinkedInWriterExecutor` passa a suportar dois modos: `"automatico"` (comportamento atual) e `"consultado"` (novo)
- Emissão de evento SignalR `awaiting_input` com lista de perguntas contextuais
- Novo endpoint `POST /api/workflow/{sessionId}/respond` para receber respostas do usuário e retomar o workflow
- Novo serviço `WorkflowSessionManager` para gerenciar sessões pausadas com timeout de 10 minutos
- Novo componente frontend `ConsultedQuestionsComponent` que exibe as perguntas e submete as respostas
- Novo status `awaiting_input` no frontend (`StepStatus`)

## Capabilities

### New Capabilities

- `workflow-session-manager`: Gerenciamento de sessões pausadas em memória com registro, resposta, limpeza e expiração automática (10 min) via background task
- `consulted-mode-executor`: Lógica human-in-the-loop dentro do `LinkedInWriterExecutor` — geração de perguntas fixas por template + dinâmicas via LLM, pausa via `RequestPort`, retomada com respostas
- `workflow-respond-endpoint`: Endpoint `POST /api/workflow/{sessionId}/respond` que entrega respostas ao executor pausado
- `consulted-questions-ui`: Componente `ConsultedQuestionsComponent` que renderiza perguntas dinamicamente, coleta respostas livres e submete ao backend

### Modified Capabilities

- `workflow-progress-ui`: Novo status `awaiting_input` (passo pausado) precisa ser representado visualmente

## Impact

- **Backend**: `LinkedInWriterExecutor.cs`, novo `WorkflowSessionManager.cs`, novo `WorkflowRespondEndpoint.cs`, `Program.cs` (registro do manager e novo endpoint), Agent Framework como dependência
- **Frontend**: `signalr.service.ts` (novo `StepStatus: 'awaiting_input'`), `WorkflowProgressComponent` (ícone para paused), `app.ts` / `app.html` (exibir `ConsultedQuestionsComponent`), novo `WorkflowService.respond()`
- **Sem breaking changes** para o modo Automático — fluxo existente inalterado
