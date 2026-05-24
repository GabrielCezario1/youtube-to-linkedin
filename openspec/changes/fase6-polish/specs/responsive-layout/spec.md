## ADDED Requirements

### Requirement: Layout funcional em mobile (375 px)
A aplicação SHALL apresentar layout de coluna única, sem overflow horizontal, em viewports com `max-width: 375px`.

#### Scenario: Formulário em mobile sem overflow
- **WHEN** a viewport é 375 px de largura
- **THEN** todos os inputs e botões ocupam 100% da largura disponível
- **THEN** nenhum elemento ultrapassa os limites do viewport (sem scroll horizontal)

#### Scenario: Progresso do workflow em mobile
- **WHEN** a view de progresso é exibida em 375 px
- **THEN** a lista de etapas é legível e não há truncamento de texto

### Requirement: Layout funcional em tablet (768 px)
A aplicação SHALL apresentar layout centralizado com largura máxima adequada em viewports de 768 px.

#### Scenario: Formulário em tablet centralizado
- **WHEN** a viewport é 768 px de largura
- **THEN** o formulário fica centralizado com `max-width` definido (≤ 600 px)
- **THEN** não há elementos excessivamente largos ou comprimidos

### Requirement: Layout funcional em desktop (1 280 px)
A aplicação SHALL apresentar layout confortável e legível em viewports ≥ 1 280 px.

#### Scenario: Conteúdo não se estira além do máximo em desktop
- **WHEN** a viewport é 1 280 px ou maior
- **THEN** o conteúdo principal respeita um `max-width` (≤ 800 px) e fica centrado
- **THEN** não há quebras de layout ou sobreposição de elementos
