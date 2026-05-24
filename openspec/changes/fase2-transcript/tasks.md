## 1. Backend — Dependências

- [x] 1.1 Adicionar pacote NuGet `YoutubeExplode 6.6.0` ao projeto `YoutubeToLinkedIn.Api`

## 2. Backend — TranscriptExecutor

- [x] 2.1 Criar `src/backend/YoutubeToLinkedIn.Api/Executors/TranscriptExecutor.cs`
- [x] 2.2 Implementar método privado `ExtractVideoId(string url): string?` usando Regex para os 3 formatos (`watch?v=`, `youtu.be/`, `/shorts/`)
- [x] 2.3 Implementar método público `ExecuteAsync(string url, string sessionId): Task<string>` que emite evento SignalR `in_progress` antes de chamar a API
- [x] 2.4 Tratar `VideoUnavailableException` → emitir SignalR `error` com mensagem `"Não foi possível acessar este vídeo. Verifique se ele é público e tente novamente."`
- [x] 2.5 Tratar ausência de tracks (`!manifest.Tracks.Any()`) → emitir SignalR `error` com mensagem `"Este vídeo não possui transcrição disponível. Tente com outro vídeo."`
- [x] 2.6 Tratar URL inválida (sem `videoId`) → emitir SignalR `error` com mensagem de URL inválida antes de chamar a API
- [x] 2.7 Tratar `Exception` genérica → emitir SignalR `error` com mensagem `"Ocorreu um erro ao extrair a transcrição. Tente novamente."`
- [x] 2.8 Emitir SignalR `completed` após transcrição extraída com sucesso

## 3. Backend — Integração no Endpoint

- [x] 3.1 Registrar `TranscriptExecutor` no DI container em `Program.cs`
- [x] 3.2 Injetar e chamar `TranscriptExecutor` no `WorkflowStartEndpoint.cs`, passando `url` e `sessionId`

## 4. Frontend — WorkflowProgressComponent

- [x] 4.1 Criar componente `WorkflowProgressComponent` em `src/frontend/youtube-to-linkedin-app/src/app/components/workflow-progress/`
- [x] 4.2 Definir modelo de dados para as 3 etapas: `transcript`, `summary`, `post` com estados `pending | in_progress | completed | error`
- [x] 4.3 Mapear eventos SignalR recebidos via `SignalRService` para atualizar o estado de cada etapa
- [x] 4.4 Renderizar ícone e estilo visual de acordo com o estado (`○` pendente, `⏳` in_progress, `✅` completed, `❌` error)
- [x] 4.5 Exibir `ErrorDisplayComponent` quando qualquer etapa estiver em estado `error`

## 5. Frontend — ErrorDisplayComponent

- [x] 5.1 Criar componente `ErrorDisplayComponent` em `src/frontend/youtube-to-linkedin-app/src/app/components/error-display/`
- [x] 5.2 Receber como input a mensagem de erro e emitir evento `retry` ao clicar no botão
- [x] 5.3 Exibir texto do botão como `"Tentar com outro vídeo"` quando a mensagem for sobre ausência de transcrição, `"Tentar novamente"` nos demais casos
- [x] 5.4 Garantir que o evento `retry` preserve URL e demais dados do formulário no componente pai

## 6. Frontend — Integração

- [x] 6.1 Substituir exibição atual do formulário pela sequência: formulário → `WorkflowProgressComponent` após submissão
- [x] 6.2 Ao receber evento `retry`, retornar ao formulário com os campos restaurados
