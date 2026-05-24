## ADDED Requirements

### Requirement: Extrair videoId de URL do YouTube
O sistema SHALL extrair o `videoId` de uma URL do YouTube usando Regex, cobrindo os formatos `youtube.com/watch?v=`, `youtu.be/` e `youtube.com/shorts/`.

#### Scenario: URL no formato watch?v=
- **WHEN** a URL fornecida é `https://www.youtube.com/watch?v=XXXXXXXXXXX`
- **THEN** o sistema extrai `videoId = "XXXXXXXXXXX"` sem realizar chamada de rede

#### Scenario: URL no formato youtu.be
- **WHEN** a URL fornecida é `https://youtu.be/XXXXXXXXXXX`
- **THEN** o sistema extrai `videoId = "XXXXXXXXXXX"` sem realizar chamada de rede

#### Scenario: URL no formato shorts
- **WHEN** a URL fornecida é `https://www.youtube.com/shorts/XXXXXXXXXXX`
- **THEN** o sistema extrai `videoId = "XXXXXXXXXXX"` sem realizar chamada de rede

#### Scenario: URL inválida (não é YouTube)
- **WHEN** a URL fornecida não corresponde a nenhum dos 3 formatos suportados
- **THEN** o sistema retorna erro imediatamente sem chamada de rede e emite evento SignalR `{ step: "transcript", status: "error", message: "URL do YouTube inválida. Use um link no formato youtube.com/watch?v=... ou youtu.be/..." }`

---

### Requirement: Extrair transcrição via YoutubeExplode
O sistema SHALL usar a biblioteca `YoutubeExplode 6.6.0` para obter a transcrição em texto puro de um vídeo do YouTube identificado pelo `videoId`.

#### Scenario: Transcrição disponível
- **WHEN** o `videoId` corresponde a um vídeo público com transcrição/legenda disponível
- **THEN** o sistema retorna o texto completo da transcrição em formato string, sem formatação adicional

#### Scenario: Vídeo privado ou removido
- **WHEN** o YoutubeExplode lança `VideoUnavailableException`
- **THEN** o sistema MUST emitir evento SignalR `{ step: "transcript", status: "error", message: "Não foi possível acessar este vídeo. Verifique se ele é público e tente novamente." }` e NÃO expor detalhes da exceção interna

#### Scenario: Vídeo sem transcrição
- **WHEN** o manifest retornado pelo YoutubeExplode não contém tracks (`!manifest.Tracks.Any()`)
- **THEN** o sistema MUST emitir evento SignalR `{ step: "transcript", status: "error", message: "Este vídeo não possui transcrição disponível. Tente com outro vídeo." }`

#### Scenario: Falha genérica da API
- **WHEN** o YoutubeExplode lança qualquer outra exceção
- **THEN** o sistema MUST emitir evento SignalR `{ step: "transcript", status: "error", message: "Ocorreu um erro ao extrair a transcrição. Tente novamente." }` sem expor o stack trace

---

### Requirement: Emitir eventos SignalR de progresso da transcrição
O sistema SHALL emitir eventos SignalR para o cliente identificado pelo `sessionId` durante o processo de extração de transcrição.

#### Scenario: Início da extração
- **WHEN** o `TranscriptExecutor` inicia a extração (antes de chamar a API externa)
- **THEN** o sistema MUST emitir `workflowEvent` com payload `{ step: "transcript", status: "in_progress" }` para o `sessionId` correspondente

#### Scenario: Extração concluída com sucesso
- **WHEN** a transcrição é extraída com sucesso
- **THEN** o sistema MUST emitir `workflowEvent` com payload `{ step: "transcript", status: "completed" }` para o `sessionId` correspondente

#### Scenario: Extração falha
- **WHEN** qualquer erro mapeado ocorre durante a extração
- **THEN** o sistema MUST emitir `workflowEvent` com payload `{ step: "transcript", status: "error", message: "<mensagem descritiva>" }` para o `sessionId` correspondente
