## ADDED Requirements

### Requirement: PostDraftComponent exibe o rascunho gerado e o template utilizado
O sistema SHALL renderizar o `PostDraftComponent` após receber o evento SignalR `{ step: "writing", status: "completed" }`. O componente SHALL exibir o campo `templateUsed` e o campo `draft` com espaçamento correto entre parágrafos.

#### Scenario: Exibição do rascunho após conclusão do workflow
- **WHEN** o evento `workflowEvent` com `{ step: "writing", status: "completed" }` é recebido
- **THEN** o `PostDraftComponent` é exibido com o `templateUsed` e o `draft` formatado (linhas em branco preservadas como parágrafos)

#### Scenario: Rascunho não exibido antes da conclusão
- **WHEN** o step `writing` ainda está `in_progress` ou não foi recebido
- **THEN** o `PostDraftComponent` não é visível na UI

### Requirement: Botão "Copiar" copia o rascunho para a área de transferência com feedback visual
O componente SHALL conter um botão "Copiar" que copia o conteúdo de `draft` para a área de transferência. Após a cópia, o botão SHALL exibir "Copiado!" por 2 segundos e então retornar ao texto original.

#### Scenario: Cópia bem-sucedida
- **WHEN** o usuário clica no botão "Copiar"
- **THEN** o texto do `draft` é copiado para a área de transferência e o botão exibe "Copiado!" por 2 segundos

#### Scenario: Retorno ao estado original após feedback
- **WHEN** 2 segundos se passam após a exibição de "Copiado!"
- **THEN** o botão retorna a exibir "Copiar"

### Requirement: Botão "Gerar Novo Post" reseta o estado completo do workflow
O componente SHALL conter um botão "Gerar Novo Post" que, ao ser clicado, reseta todos os sinais de estado (transcript, summary, draft, steps de progresso) e retorna a UI ao estado inicial (formulário de URL visível, componentes de resultado ocultados).

#### Scenario: Reset completo ao clicar em "Gerar Novo Post"
- **WHEN** o usuário clica em "Gerar Novo Post"
- **THEN** todos os estados do workflow são resetados e o formulário de URL é exibido novamente

#### Scenario: Componentes de resultado ocultados após reset
- **WHEN** o reset é acionado
- **THEN** `PostDraftComponent`, `WorkflowProgressComponent` e quaisquer outros componentes de resultado são ocultados
