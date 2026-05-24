## Why

O produto atualmente recebe uma URL do YouTube mas não extrai nenhum conteúdo dela. A Fase 2 introduz o `TranscriptExecutor`, responsável por obter a transcrição em texto puro do vídeo, possibilitando que as fases seguintes (resumo e geração de post) tenham conteúdo para processar.

## What Changes

- Adicionar `TranscriptExecutor` ao backend: extrai transcrição de vídeo do YouTube via `YoutubeExplode` (NuGet 6.6.0)
- Implementar parser de `videoId` por Regex cobrindo 3 formatos de URL do YouTube (`watch?v=`, `youtu.be/`, `/shorts/`)
- Integrar `TranscriptExecutor` ao `WorkflowStartEndpoint`, emitindo eventos SignalR `in_progress`, `completed` e `error`
- Criar `WorkflowProgressComponent` no frontend com visualização de 3 etapas do fluxo
- Criar `ErrorDisplayComponent` com mensagem descritiva e botão de retry (preservando dados do formulário)

## Capabilities

### New Capabilities

- `transcript-extraction`: Extração de transcrição de vídeo do YouTube a partir da URL, com tratamento de erros (vídeo privado, sem transcrição, URL inválida) e notificação de progresso via SignalR
- `workflow-progress-ui`: Componente Angular que exibe o progresso do fluxo em etapas (pendente / in_progress / completed / error) com suporte a erro e retry

### Modified Capabilities

<!-- Nenhuma capability existente tem seus requisitos alterados nesta fase -->

## Impact

- **Backend**: Novo arquivo `TranscriptExecutor.cs`; `WorkflowStartEndpoint.cs` modificado para chamar o executor; adição do pacote NuGet `YoutubeExplode 6.6.0`
- **Frontend**: Novos componentes `WorkflowProgressComponent` e `ErrorDisplayComponent`; nenhuma alteração nos serviços existentes (`SignalRService`, `WorkflowService`)
- **SignalR**: Passa a emitir eventos com `step: "transcript"` e status `in_progress` / `completed` / `error`
- **Sem persistência**: transcrição trafega apenas em memória na sessão; nenhum banco de dados é afetado
