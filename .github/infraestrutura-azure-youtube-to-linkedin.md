# Infraestrutura Azure — Tech Content Agent

---

# Parte 1 — Tecnologias Necessárias

## Contexto

O Tech Content Agent é uma ferramenta pessoal que recebe a URL de um vídeo do YouTube, extrai a transcrição e usa inteligência artificial para gerar um rascunho de post pronto para o LinkedIn. Toda a inteligência de geração de texto depende de um modelo de linguagem hospedado no Azure.

---

## Serviços Azure Necessários

### Azure OpenAI

**Para que serve:** É o serviço da Microsoft que dá acesso a modelos de linguagem avançados (como o GPT-4o) para gerar, resumir e transformar texto de forma inteligente.

**Por que esta feature precisa dele:** O sistema usa o Azure OpenAI em duas etapas do processo: primeiro para condensar a transcrição do vídeo nos pontos técnicos mais relevantes, e depois para escrever o rascunho do post no formato escolhido (Storytelling, Lista Prática ou Opinião Provocativa). Sem ele, o sistema não tem capacidade de interpretar conteúdo nem de gerar texto.

**Nível de uso:** Básico — cada geração de post dispara duas chamadas ao modelo; o volume é baixo por se tratar de uso pessoal.

---

## Serviços que Podem Ser Necessários no Futuro

- **Azure Application Insights:** Quando o sistema for compartilhadoo com outras pessoas, será necessário monitorar erros, tempo de resposta e falhas nas chamadas ao modelo de IA.
- **Azure Key Vault:** Se a API Key do Azure OpenAI precisar ser centralizada fora da máquina local — por exemplo, em um ambiente de servidor ou CI/CD —, o Key Vault é o local seguro para armazená-la.

---

## Resumo de Tecnologias

| Serviço         | Papel na Feature                                             | Quando Provisionar |
| --------------- | ------------------------------------------------------------ | ------------------ |
| Azure OpenAI    | Resumo da transcrição e geração do post do LinkedIn          | Agora              |
| App. Insights   | Monitoramento de erros e performance em ambiente compartilhado | Futuro             |
| Azure Key Vault | Armazenamento seguro da API Key fora da máquina local        | Futuro             |

---

---

# Parte 2 — Guia de Criação no Portal Azure

> Este guia foi criado para desenvolvedores que nunca configuraram serviços Azure.
> Siga as etapas na ordem apresentada — o modelo de IA só pode ser configurado depois que o recurso Azure OpenAI existir.

## Pré-requisitos

Antes de começar, verifique se você tem:

- [ ] Conta Azure ativa — se não tiver, crie gratuitamente em https://azure.microsoft.com/free
- [ ] Acesso ao [Portal Azure](https://portal.azure.com) (faça login com sua conta Microsoft)
- [ ] .NET 10 SDK instalado na sua máquina (para configurar o User Secrets após criar o recurso)

---

## Etapa 1 — Criar o Grupo de Recursos

> **Por que fazer isso primeiro?** O Grupo de Recursos é uma pasta no Azure que agrupa todos os serviços do projeto. Ele deve existir antes de qualquer outro serviço ser criado. Também facilita excluir tudo de uma vez quando não precisar mais.

1. No [Portal Azure](https://portal.azure.com), clique na **barra de pesquisa** no topo da tela e digite **"Grupos de recursos"**
2. Clique em **"Grupos de recursos"** nos resultados
3. Clique no botão **"+ Criar"** no canto superior esquerdo
4. Preencha os campos:
   - **Assinatura:** selecione sua assinatura (normalmente aparece o nome da sua conta Microsoft)
   - **Grupo de recursos:** digite `rg-youtube-to-linkedin`
   - **Região:** selecione **"Brazil South"**
5. Clique em **"Examinar + criar"**
6. Revise as informações e clique em **"Criar"**
7. Aguarde a mensagem **"Implantação bem-sucedida"** — quando aparecer, clique em **"Ir para o recurso"**

---

## Etapa 2 — Criar o Recurso Azure OpenAI

> **O que é e por que criar:** O recurso Azure OpenAI é o "contêiner" que abriga os modelos de IA. Dentro dele, você vai alocar o modelo `gpt-4o-mini`, que é o responsável por gerar os resumos e os posts do LinkedIn. Sem este recurso, a aplicação não consegue chamar nenhum modelo de IA.

### 2.1 — Criar o recurso

1. Na barra de pesquisa do portal, digite **"Azure OpenAI"**
2. Clique em **"Azure OpenAI"** nos resultados (categoria: Serviços de IA + Aprendizado de Máquina)
3. Clique em **"+ Criar"**
4. Preencha os campos na aba **"Básico"**:
   - **Assinatura:** selecione sua assinatura
   - **Grupo de recursos:** selecione `rg-youtube-to-linkedin` (criado na Etapa 1)
   - **Região:** selecione **"Brazil South"**
   - **Nome:** digite `openai-youtube-to-linkedin`
     > ⚠️ O nome do recurso Azure OpenAI é globalmente único — se `openai-youtube-to-linkedin` já estiver em uso no mundo, adicione um sufixo (ex: `openai-youtube-to-linkedin-01` ou `openai-youtube-to-linkedin-gab`)
   - **Tipo de preço:** selecione **"Standard S0"** — é o único tier disponível e cobre o uso pessoal sem custo fixo mensal (você paga apenas pelos tokens usados)
5. Clique em **"Próximo"** nas abas **"Rede"** e **"Marcas"** sem alterar nada — os padrões são adequados para uso pessoal
6. Na aba **"Revisar + criar"**, revise as informações e clique em **"Criar"**
7. A criação pode levar de 1 a 3 minutos — aguarde a mensagem **"Implantação bem-sucedida"**
8. Clique em **"Ir para o recurso"**

---

### 2.2 — Implantar o modelo gpt-4o-mini

> Criar o recurso Azure OpenAI não é suficiente — você precisa alocar um modelo dentro dele. Este passo cria o "deployment" que a aplicação vai chamar.

1. Dentro do recurso `openai-youtube-to-linkedin`, clique em **"Microsoft Foundry"** no menu lateral esquerdo (ou no botão de acesso rápido no centro da tela) — isso abre o portal em [ai.azure.com](https://ai.azure.com)
   > ⚠️ O portal foi rebrandeado: o nome correto agora é **Microsoft Foundry** (anteriormente "Azure AI Foundry" / "Azure OpenAI Studio"). Se ao abrir o portal aparecer um toggle **"New Foundry"**, certifique-se de que ele esteja **desligado** para usar a experiência clássica compatível com recursos Azure OpenAI existentes.
2. Na seção **"Keep building with Foundry"**, clique em **"View all resources"** e selecione o recurso `openai-youtube-to-linkedin`
3. No menu lateral, em **"Shared resources"**, clique em **"Implantações"** (ou "Deployments")
4. Clique em **"+ Implantar modelo"** → **"Implantar modelo base"**
5. Na lista de modelos, selecione **"gpt-4o-mini"**
6. Clique em **"Confirmar"**
7. Preencha os campos:
   - **Nome da implantação:** digite exatamente `gpt-4o-mini`
     > ⚠️ Este nome deve ser idêntico ao valor `AzureOpenAI:ModelId` no `appsettings.json`. O projeto já vem configurado com `gpt-4o-mini`, então manter este nome evita qualquer alteração de código.
   - **Versão do modelo:** deixe a versão mais recente selecionada
   - **Tipo de implantação:** selecione **"Standard"**
   - **Limite de taxa de tokens por minuto:** o valor padrão (normalmente 10K–30K TPM) é mais que suficiente para uso pessoal — não precisa alterar
8. Clique em **"Implantar"**
9. Aguarde o status mudar para **"Êxito"** — pode levar alguns segundos

---

### 2.3 — Copiar o Endpoint e a API Key

> Estas duas informações são necessárias para conectar a aplicação ao modelo. Você vai usá-las na próxima etapa.

1. Volte para o [Portal Azure](https://portal.azure.com) e abra o recurso `openai-youtube-to-linkedin`
   > Navegue via: Portal Azure → Grupos de recursos → `rg-youtube-to-linkedin` → `openai-youtube-to-linkedin`
2. No menu lateral, clique em **"Chaves e ponto de extremidade"** (ou "Keys and Endpoint")
3. Você verá:
   - **Ponto de extremidade (Endpoint):** algo como `https://openai-youtube-to-linkedin.openai.azure.com/`
   - **Chave 1** e **Chave 2:** sequências alfanuméricas longas (use qualquer uma das duas)

> ⚠️ **Anote estas informações** — você vai precisar delas na próxima etapa:
>
> - **Endpoint:** campo "Ponto de extremidade" → `https://<seu-nome>.openai.azure.com/`
> - **API Key:** campo "Chave 1" → valor oculto com botão de copiar ao lado

---

## Etapa 3 — Configurar a Aplicação Local

> Com o recurso criado e as credenciais em mãos, agora é hora de conectar a aplicação ao Azure.

### 3.1 — Preencher o appsettings.json

1. Abra o arquivo `src/backend/YoutubeToLinkedIn.Api/appsettings.json`
2. Preencha o campo `Endpoint` com o valor copiado na Etapa 2.3:

```json
"AzureOpenAI": {
  "Endpoint": "https://openai-youtube-to-linkedin.openai.azure.com/",
  "ApiKey": "",
  "ModelId": "gpt-4o-mini"
}
```

> ⚠️ **Não coloque a API Key no `appsettings.json`** — este arquivo é versionado no Git e a chave ficaria exposta. Use o próximo passo.

### 3.2 — Salvar a API Key com User Secrets

1. Abra um terminal na pasta do projeto backend:

```bash
cd src/backend/YoutubeToLinkedIn.Api
```

2. Inicialize o User Secrets (só precisa fazer uma vez):

```bash
dotnet user-secrets init
```

3. Salve a API Key (substitua `<sua-chave>` pelo valor copiado na Etapa 2.3):

```bash
dotnet user-secrets set "AzureOpenAI:ApiKey" "<sua-chave>"
```

4. Confirme que foi salvo:

```bash
dotnet user-secrets list
```

> Você deve ver: `AzureOpenAI:ApiKey = <sua-chave>`

A chave fica armazenada fora do repositório, em `%APPDATA%\Microsoft\UserSecrets\` no Windows — nunca será commitada acidentalmente.

---

## Verificação Final

Antes de considerar o ambiente pronto, confirme cada item abaixo:

- [ ] Grupo de recursos `rg-youtube-to-linkedin` criado na região Brazil South
- [ ] Recurso Azure OpenAI `openai-youtube-to-linkedin` criado (status: Êxito)
- [ ] Modelo `gpt-4o-mini` implantado dentro do recurso (status: Êxito)
- [ ] `AzureOpenAI:Endpoint` preenchido no `appsettings.json`
- [ ] `AzureOpenAI:ApiKey` salvo via `dotnet user-secrets`

Se algum item não estiver marcado, volte para a etapa correspondente.

Para testar que tudo funciona, execute a aplicação e gere um post:

```bash
# Terminal 1 — Backend
cd src/backend/YoutubeToLinkedIn.Api
dotnet run

# Terminal 2 — Frontend
cd src/frontend/youtube-to-linkedin-app
npm install
ng serve
```

Acesse `http://localhost:4200`, insira uma URL de vídeo do YouTube e clique em **"Gerar Post"**. Se o post for gerado, a integração com o Azure OpenAI está funcionando.

---

## Problemas Comuns

| Problema | Causa provável | O que fazer |
| -------- | -------------- | ----------- |
| `"AzureOpenAI:ApiKey is not configured"` ao iniciar o backend | O User Secret não foi salvo ou foi salvo com o nome errado | Execute `dotnet user-secrets list` e confira se a chave aparece exatamente como `AzureOpenAI:ApiKey` |
| `401 Unauthorized` ao chamar o modelo | A API Key está errada ou expirada | Volte à Etapa 2.3 no portal, copie a chave novamente e refaça o `dotnet user-secrets set` |
| `404 Not Found` ao chamar o modelo | O nome do deployment não corresponde ao `ModelId` no `appsettings.json` | Confirme que o deployment se chama exatamente `gpt-4o-mini` no Azure AI Foundry |
| `"O nome já está em uso"` ao criar o recurso Azure OpenAI | Nomes de recursos Azure OpenAI são globais | Adicione um sufixo ao nome: `openai-youtube-to-linkedin-01` ou `openai-youtube-to-linkedin-gab` — e atualize o `appsettings.json` com o novo endpoint |
| Recurso Azure OpenAI não aparece para criar | Sua assinatura pode precisar de aprovação para acessar o serviço | Acesse https://aka.ms/oai/access e solicite acesso — o processo pode levar alguns dias em assinaturas novas |
| `CORS error` no frontend | O backend não está rodando ou a porta está diferente | Confirme que o backend está em `https://localhost:5001` e que `AllowedOrigins` no `appsettings.json` contém `http://localhost:4200` |
