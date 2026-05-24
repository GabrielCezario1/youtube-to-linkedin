# PRD — Fase 7: Configuração e README

> **Versão:** 1.0
> **Data:** 2026-05-24
> **Depende de:** Todas as fases anteriores
> **Referência:** [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md)

---

## Objetivo

Tornar o projeto executável por qualquer desenvolvedor (incluindo o próprio autor no futuro) com o mínimo de fricção. Documentar os pré-requisitos, configurações necessárias e passos para rodar localmente.

---

## Estrutura de Arquivos desta Fase

```
youtube-to-linkedin/
│
├── README.md                          ← documentação principal
│
├── src/
│   ├── backend/
│   │   └── YoutubeToLinkedIn.Api/
│   │       ├── appsettings.json       ← chaves de config (sem segredos)
│   │       └── appsettings.Development.json  ← valores locais (gitignored)
│   │
│   └── frontend/
│       └── youtube-to-linkedin-app/
│           └── .env.example           ← template de variáveis de ambiente
│
└── .gitignore                         ← segredos, build artifacts, node_modules
```

---

## Variáveis de Configuração

### Backend — `appsettings.json`

```json
{
  "AzureOpenAI": {
    "Endpoint": "",
    "ApiKey":   "",
    "ModelId":  "gpt-4o-mini"
  },
  "Workflow": {
    "ConsultedSessionTimeoutMinutes": 10
  },
  "AllowedOrigins": [
    "http://localhost:4200"
  ]
}
```

### Frontend — `.env.example`

```
BACKEND_URL=https://localhost:5001
```

---

## Estrutura do README.md

```
┌─────────────────────────────────────────────────────────────────┐
│                         README.md                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  # Tech Content Agent                                           │
│  Breve descrição do projeto (2–3 linhas)                        │
│                                                                 │
│  ## Pré-requisitos                                              │
│  • .NET 10 SDK                                                  │
│  • Node.js 20+ e npm                                            │
│  • Angular CLI (npm install -g @angular/cli)                    │
│  • Conta Azure OpenAI com deployment ativo                      │
│    (ou chave OpenAI)                                            │
│                                                                 │
│  ## Configuração                                                │
│  1. Clonar o repositório                                        │
│  2. Copiar appsettings.Development.json.example                 │
│     e preencher as chaves                                       │
│  3. Copiar .env.example → .env no frontend                      │
│                                                                 │
│  ## Rodando Localmente                                          │
│  ### Backend                                                    │
│  cd src/backend/YoutubeToLinkedIn.Api                           │
│  dotnet run                                                     │
│  → disponível em https://localhost:5001                         │
│                                                                 │
│  ### Frontend                                                   │
│  cd src/frontend/youtube-to-linkedin-app                        │
│  npm install                                                    │
│  ng serve                                                       │
│  → disponível em http://localhost:4200                          │
│                                                                 │
│  ## Documentação                                                │
│  • PRD: docs/PRD_TechContentAgent.md                            │
│  • Plano de implementação: docs/IMPLEMENTATION_PLAN.md          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## `.gitignore` — Entradas Obrigatórias

```
# Segredos e configuração local
appsettings.Development.json
*.user
.env

# Build artifacts — Backend
bin/
obj/

# Build artifacts — Frontend
node_modules/
dist/
.angular/

# IDE
.vs/
.idea/
*.suo
*.user

# OS
.DS_Store
Thumbs.db
```

---

## Regras e Decisões

| # | Regra / Decisão | Justificativa |
|---|---|---|
| R1 | `appsettings.Development.json` **nunca commitado** (gitignored) | Segredos não entram no repositório |
| R2 | `appsettings.json` tem **valores vazios** para as chaves sensíveis | Documenta a estrutura sem expor segredos |
| R3 | Fornecer **`.env.example`** (não `.env`) no frontend | Padrão da indústria; `.env` é gitignored |
| R4 | `ConsultedSessionTimeoutMinutes` no `appsettings.json` | Configurável sem recompilar; valor padrão: 10 |
| R5 | README em **inglês** | Padrão para projetos no GitHub; audiência mais ampla |
| R6 | README referencia os documentos em `docs/` | Facilita navegação para novos contribuidores |
| R7 | Pré-requisitos listam **versões mínimas** (.NET 10, Node 20+) | Evita incompatibilidades silenciosas |
| R8 | Passos de configuração incluem **exemplo de comando** para cada etapa | Reduz fricção para executar o projeto |
| R9 | `AllowedOrigins` no `appsettings.json` (não hardcoded em `Program.cs`) | CORS configurável por ambiente |
| R10 | `ModelId` configurável via `appsettings.json` | Permite trocar entre `gpt-4o` e `gpt-4o-mini` sem recompilar |

---

## Tarefas

- [ ] Criar `appsettings.json` com estrutura de chaves (valores vazios para segredos)
- [ ] Criar `appsettings.Development.json.example` como template preenchível
- [ ] Adicionar `appsettings.Development.json` ao `.gitignore`
- [ ] Criar `.env.example` no frontend com `BACKEND_URL`
- [ ] Criar/atualizar `.gitignore` raiz com todas as entradas obrigatórias
- [ ] Criar `README.md` cobrindo: descrição, pré-requisitos, configuração, execução local, links para docs
- [ ] Verificar que `ConsultedSessionTimeoutMinutes` é lido de `appsettings.json`
- [ ] Verificar que `AllowedOrigins` é lido de `appsettings.json`

---

## Critério de Conclusão

```
✅  Clone do zero + seguir README → projeto rodando localmente
    sem nenhuma alteração em código-fonte

✅  Nenhum segredo commitado no repositório
    (appsettings.Development.json e .env no .gitignore)

✅  appsettings.json documenta a estrutura completa de configuração

✅  README cobre: pré-requisitos, configuração e execução local

✅  .gitignore cobre: segredos, build artifacts, node_modules, IDE files
```

---

## Fora do Escopo desta Fase

- Deploy em produção (Azure, Docker, CI/CD)
- Documentação de API (Swagger/OpenAPI)
- Changelog ou versionamento semântico
- Testes documentados no README

