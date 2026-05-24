## ADDED Requirements

### Requirement: LinkedInWriterExecutor gera rascunho de post via Azure OpenAI
O sistema SHALL chamar o Azure OpenAI com o system prompt carregado de `Prompts/linkedin-writer-system.md` e uma user message contendo o tipo de post e o resumo. A resposta SHALL ser um JSON `{ "draft": "...", "templateUsed": "..." }`.

#### Scenario: Geração bem-sucedida
- **WHEN** `ExecuteAsync` é chamado com um `summary` não vazio e um `postType` válido
- **THEN** o executor chama o Azure OpenAI e retorna um `PostDraftResult` com `Draft` e `TemplateUsed` não vazios

#### Scenario: Resposta com markdown fence
- **WHEN** o LLM retorna o JSON envolvido em bloco de código markdown (` ```json ... ``` `)
- **THEN** o executor remove o fence antes do parse e retorna `PostDraftResult` corretamente

#### Scenario: Falha na API do LLM
- **WHEN** o Azure OpenAI retorna um erro (`RequestFailedException`)
- **THEN** o executor emite `workflowEvent` com `{ step: "writing", status: "error", message: "Ocorreu um erro ao gerar o post. Tente novamente." }` e re-lança a exceção

#### Scenario: Timeout na chamada ao LLM
- **WHEN** a chamada ao Azure OpenAI expira (`TaskCanceledException`)
- **THEN** o executor emite `workflowEvent` com `{ step: "writing", status: "error", message: "Ocorreu um erro ao gerar o post. Tente novamente." }` e re-lança a exceção

#### Scenario: JSON inválido na resposta
- **WHEN** o LLM retorna uma string que não pode ser desserializada como `PostDraftResult`
- **THEN** o executor emite `workflowEvent` com `{ step: "writing", status: "error", message: "Ocorreu um erro ao gerar o post. Tente novamente." }` e re-lança a exceção

### Requirement: LinkedInWriterExecutor emite eventos SignalR de progresso
O sistema SHALL emitir `workflowEvent` SignalR para a sessão do cliente ao início e ao fim do step de escrita.

#### Scenario: Evento emitido antes da chamada ao LLM
- **WHEN** `ExecuteAsync` é invocado
- **THEN** um `workflowEvent` com `{ step: "writing", status: "in_progress" }` é enviado à sessão do cliente antes da chamada ao Azure OpenAI

#### Scenario: Evento emitido com resultado após resposta bem-sucedida
- **WHEN** o Azure OpenAI retorna uma resposta bem-sucedida e o JSON é parseado com sucesso
- **THEN** um `workflowEvent` com `{ step: "writing", status: "completed", result: { draft: "...", templateUsed: "..." } }` é enviado à sessão do cliente

### Requirement: LinkedInWriterExecutor é stateless e registrado como Singleton
O `LinkedInWriterExecutor` SHALL não conter estado por-requisição. SHALL ser registrado como `AddSingleton` em `Program.cs`.

#### Scenario: Requisições concorrentes não interferem
- **WHEN** dois workflows são processados concorrentemente
- **THEN** os eventos SignalR de cada um são enviados apenas à sua própria `sessionId` e os rascunhos não se cruzam

### Requirement: LinkedInWriterExecutor é encadeado após SummaryExecutor
O sistema SHALL chamar `LinkedInWriterExecutor.ExecuteAsync` imediatamente após `SummaryExecutor.ExecuteAsync` retornar com sucesso, passando o resumo, o `PostType` da requisição e o `sessionId`.

#### Scenario: Encadeamento bem-sucedido
- **WHEN** `SummaryExecutor` completa sem erro
- **THEN** `LinkedInWriterExecutor` é invocado com o resumo retornado e o `PostType` da requisição original

#### Scenario: Falha no SummaryExecutor não aciona o LinkedInWriterExecutor
- **WHEN** `SummaryExecutor` lança uma exceção
- **THEN** `LinkedInWriterExecutor` NÃO é chamado e a exceção se propaga

### Requirement: PostDraftResult é um record C# imutável
O sistema SHALL representar o resultado da geração como `public record PostDraftResult(string Draft, string TemplateUsed)`.

#### Scenario: Serialização para SignalR
- **WHEN** `PostDraftResult` é incluído no payload do evento `workflowEvent`
- **THEN** o JSON resultante contém as propriedades `draft` e `templateUsed` (camelCase via convenção SignalR)
