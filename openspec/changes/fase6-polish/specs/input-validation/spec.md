## ADDED Requirements

### Requirement: URL é obrigatória e deve ser do YouTube
O endpoint `/api/workflow/start` SHALL rejeitar requests com `url` vazia ou que não corresponda ao padrão de URL do YouTube (`youtube.com/watch?v=`, `youtu.be/`, `youtube.com/shorts/`).

#### Scenario: URL vazia retorna 400
- **WHEN** o cliente envia `POST /api/workflow/start` com `url: ""`
- **THEN** o servidor retorna `400 Bad Request` com `{ "error": "URL é obrigatória", "field": "url" }`
- **THEN** nenhum `sessionId` é criado

#### Scenario: URL de domínio não-YouTube retorna 400
- **WHEN** o cliente envia `POST /api/workflow/start` com `url: "https://vimeo.com/123"`
- **THEN** o servidor retorna `400 Bad Request` com `{ "error": "URL inválida. Use um link do YouTube.", "field": "url" }`
- **THEN** nenhum `sessionId` é criado

#### Scenario: URL válida do YouTube é aceita
- **WHEN** o cliente envia `url: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"`
- **THEN** o servidor prossegue com a validação dos demais campos

### Requirement: postType deve ser um valor válido
O endpoint SHALL rejeitar `postType` que não seja `"storytelling"`, `"lista"` ou `"opiniao"`.

#### Scenario: postType inválido retorna 400
- **WHEN** o cliente envia `postType: "tweet"`
- **THEN** o servidor retorna `400 Bad Request` com `{ "error": "Tipo de post inválido. Use: storytelling, lista, opiniao", "field": "postType" }`

#### Scenario: postType válido é aceito
- **WHEN** o cliente envia `postType: "storytelling"` (ou `"lista"` ou `"opiniao"`)
- **THEN** o servidor prossegue com a validação dos demais campos

### Requirement: mode deve ser um valor válido
O endpoint SHALL rejeitar `mode` que não seja `"automatico"` ou `"consultado"`.

#### Scenario: mode inválido retorna 400
- **WHEN** o cliente envia `mode: "hybrid"`
- **THEN** o servidor retorna `400 Bad Request` com `{ "error": "Modo inválido. Use: automatico, consultado", "field": "mode" }`

#### Scenario: mode válido é aceito
- **WHEN** o cliente envia `mode: "automatico"` ou `mode: "consultado"`
- **THEN** o servidor retorna `200 OK` com `{ "sessionId": "<guid>" }` e inicia o workflow
