# Architecture Decisions

🌍 Idioma
- 🇧🇷 Português (atual)
- 🇺🇸 [English](ARCHITECTURE_DECISIONS.md)

---

Este documento descreve as principais decisões arquiteturais e técnicas tomadas durante a implementação da Rental Pipeline API.

---

# Por que .NET 10?

O .NET 10 foi escolhido porque oferece:

- Excelente performance
- Injeção de Dependência nativa
- Modelo de hospedagem moderno
- Manutenibilidade de longo prazo
- Ecossistema forte

---

# Por que Clean Architecture?

O desafio contém diversas regras de negócio que devem permanecer independentes de preocupações de infraestrutura.

Clean Architecture oferece:

- Separação de responsabilidades
- Melhor testabilidade
- Baixo acoplamento
- Alta manutenibilidade
- Evolução futura mais fácil

As regras de negócio permanecem isoladas de frameworks e dependências externas.

---

# Por que DDD Lite?

O projeto modela o domínio de negócio usando conceitos selecionados de Domain-Driven Design, sem introduzir complexidade desnecessária.

Conceitos implementados:

- Entities
- Aggregate Root
- Domain Services
- Repository Interfaces
- State Machine

Intencionalmente omitidos:

- CQRS
- MediatR
- Event Sourcing
- Specifications
- Factories
- Domain Events

Essa abordagem mantém a solução simples e, ao mesmo tempo, oferece um modelo de domínio rico.

---

# CRUD Completo para Property e Customer

A especificação original documenta apenas as operações de Create e Get para Property e Customer.

Operações completas de Update e Delete foram adicionadas para ambas as entidades a pedido explícito, para fornecer uma experiência de CRUD completa e consistente em toda a API.

As operações de exclusão incluem uma proteção de exclusão segura: um Property ou Customer com pelo menos uma rental proposal associada não pode ser excluído. Isso evita chaves estrangeiras órfãs e preserva a integridade do histórico do pipeline de propostas.

---

# Prioridade de Escopo: Fluxo das Propostas e Estados das Entidades em vez de Detalhes Financeiros

O requisito central do desafio é um pipeline correto de propostas de locação — transições de status, reserva/liberação do imóvel, segurança de concorrência, histórico e simulação de eventos.

Aspectos financeiros de uma proposta (valor do aluguel, depósitos, taxas, condições de pagamento, etc.) foram intencionalmente deixados fora do escopo. A prioridade foi o fluxo em si e o funcionamento correto das mudanças de estado das propostas e dos imóveis, e não um modelo comercial completo de um contrato de locação.

---

# Por que PostgreSQL?

O PostgreSQL foi selecionado porque oferece:

- Transações ACID
- Excelente suporte a concorrência
- Isolamento de transação Serializable
- Row locking
- Confiabilidade
- Ótima integração com Entity Framework Core

Esses recursos são particularmente importantes para proteger o processo de criação de propostas contra requisições concorrentes.

---

# Por que Entity Framework Core?

O Entity Framework Core foi selecionado porque oferece:

- Produtividade
- Tipagem forte
- Suporte a LINQ
- Code First Migrations
- Suporte a Optimistic Concurrency
- Excelente integração com o .NET

---

# Optimistic Concurrency: RowVersion Mapeado para o xmin do PostgreSQL

Atualizações concorrentes ao mesmo Property ou RentalProposal precisam ser detectadas e rejeitadas com segurança, em vez de sobrescreverem uma à outra silenciosamente.

Em vez de manter uma coluna de concorrência separada e gerenciada manualmente, o token de optimistic concurrency é mapeado diretamente para a coluna de sistema nativa `xmin` do PostgreSQL, usando a configuração `IsRowVersion()` do provider Npgsql do EF Core.

Isso exige que o token de concorrência seja tipado como `uint` (não `byte[]`, que é a convenção mais comum para SQL Server) nas entidades de Domain, já que é assim que o provider Npgsql representa o `xmin`.

Benefícios:

- Nenhuma coluna, trigger ou lógica de incremento manual extra é necessária
- Suporte nativo do banco de dados, atualizado automaticamente pelo PostgreSQL a cada alteração de linha
- Conflitos surgem como uma `DbUpdateConcurrencyException`, mapeada para `409 Conflict`

---

# Por que FluentValidation?

A validação de requisições deve permanecer separada da lógica de negócio.

O FluentValidation permite que as regras de validação permaneçam:

- Reutilizáveis
- Legíveis
- Facilmente testáveis

As regras de negócio permanecem dentro das camadas de Domain/Application.

---

# Por que FluentResults?

Os Application Services retornam `Result<T>` em vez de lançar exceções para cenários de negócio esperados.

Benefícios:

- Fluxo de execução explícito
- Testes mais fáceis
- Melhor legibilidade
- Respostas de API previsíveis

Exceções ficam reservadas para falhas inesperadas.

---

# Exceções de Domain: Um Único Tipo Genérico

A camada de Domain define um único tipo `DomainException`, em vez de uma hierarquia de subclasses de exceção específicas (por exemplo, uma exceção por estado inválido).

Motivos:

- Essas exceções existem apenas como uma rede de segurança de defesa em profundidade, para estados que nunca deveriam ocorrer se a camada de Application tivesse realizado a validação esperada previamente
- Nenhuma delas deve ser capturada ou tratada de forma diferente das outras — todas representam a mesma categoria de "isso nunca deveria acontecer"
- Uma hierarquia de subclasses adicionaria cerimônia sem nenhum benefício comportamental real

Falhas de negócio esperadas (uma transição de status de proposta inválida, um imóvel que não está disponível, etc.) nunca são representadas como exceções — elas sempre fluem através de `Result<T>`.

---

# Por que Mapeamento Manual em vez de AutoMapper?

O mapeamento entre entidades de Domain e DTOs é feito através de pequenos métodos de extensão explícitos (por exemplo, `ToDto()`), em vez de uma biblioteca de mapeamento como o AutoMapper.

Motivos:

- Os mapeamentos envolvidos são simples, sem flattening complexo, coleções aninhadas ou lógica condicional que justificasse uma biblioteca de mapeamento
- Código de mapeamento explícito é mais fácil de ler, depurar e refatorar com segurança, e oferece segurança total verificada em tempo de compilação
- O AutoMapper migrou para um modelo de licenciamento pago para uso comercial, o que representa uma dependência e um custo desnecessários para as necessidades de mapeamento simples deste projeto

Isso mantém a camada de Application livre de uma biblioteca baseada em reflection, permanecendo igualmente fácil de manter.

---

# Por que uma State Machine Dedicada para Propostas?

As transições de status da proposta são centralizadas dentro de uma `ProposalStateMachine` dedicada.

Em vez de espalhar as regras de transição por múltiplos services ou usar declarações condicionais complexas, um mapa de transições é utilizado.

Benefícios:

- Única fonte da verdade
- Manutenção mais fácil
- Testes mais fáceis
- Fácil de estender
- Elimina regras de negócio duplicadas

---

# Coordenação entre Aggregates

`RentalProposal` e `Property` são Aggregate Roots separados. Um único aggregate nunca deve alterar diretamente o estado de outro aggregate.

Por causa disso, efeitos colaterais que abrangem ambos os aggregates — como reservar ou liberar um Property quando o status de uma proposta muda — são coordenados pela camada de Application Service, e não pela própria entidade `RentalProposal`.

Cada aggregate permanece responsável apenas por impor seus próprios invariantes:

- `RentalProposal` é dono de suas próprias transições de status e do histórico de status
- `Property` é dono de suas próprias guard clauses de status

O Application Service carrega ambos os aggregates, aplica a transição da proposta, aplica a transição resultante no imóvel, e persiste ambas as alterações através de um único Unit of Work.

---

# O Histórico da Proposta Inclui sua Própria Criação

O ciclo de vida completo de uma rental proposal precisa estar visível através do seu histórico — incluindo o momento em que ela foi criada, não apenas suas transições de status posteriores.

Por esse motivo, o "status anterior" registrado no histórico é anulável (`nullable`): a entrada criada junto com a proposta não possui um status anterior real, e `null` comunica isso de forma mais honesta do que reutilizar o status inicial como um "anterior" falso.

Como resultado, toda rental proposal sempre possui pelo menos uma entrada de histórico desde o momento em que existe, e seu endpoint de histórico sempre reflete a história completa da proposta, da criação até seu estado atual.

---

# Estratégia de Publicação de Eventos

O desafio exige a simulação de um evento assíncrono quando uma proposta se torna `Active`.

Em vez de integrar diretamente com o RabbitMQ, a aplicação introduz uma abstração:

```
IEventPublisher
```

A implementação atual:

```
FakeEventPublisher
```

Responsabilidades:

- Logging estruturado
- Simulação de eventos
- Abstração de infraestrutura

Uma futura integração com RabbitMQ exigirá a substituição apenas da implementação de infraestrutura.

Nenhuma alteração deve ser necessária nas camadas de Domain ou Application.

---

# Estratégia de Concorrência

Um dos requisitos do desafio é prevenir condições de corrida durante a criação de propostas.

A estratégia escolhida combina mecanismos nativos do PostgreSQL com o Entity Framework Core.

Abordagem implementada:

- Transações de banco de dados
- Nível de isolamento Serializable
- Optimistic Concurrency

Isso garante que duas requisições simultâneas não consigam criar propostas para o mesmo imóvel.

A consistência foi intencionalmente priorizada em detrimento da performance.

---

# Detalhes da Implementação de Concorrência

A estratégia acima é implementada como duas camadas independentes e complementares — qualquer uma delas sozinha já fecharia a corrida na prática, mas o `Architecture.md` pede explicitamente as duas, e cada uma protege contra uma anomalia ligeiramente diferente:

**Camada 1 — Transação Serializable em torno da seção crítica.** `IUnitOfWork.ExecuteInSerializableTransactionAsync<TResult>` encapsula um delegate em uma transação `Database.BeginTransactionAsync(IsolationLevel.Serializable)` (feito commit somente se o delegate for concluído sem lançar exceção; qualquer exceção provoca rollback através do dispose). `RentalProposalService.CreateAsync` encapsula exatamente a sequência ler-verificar-reservar-criar do diagrama de "Transaction Flow" do `Architecture.md` (ler o Property, verificar a Rule 2, reservá-lo e criar a Proposal) dentro dessa transação — não a requisição inteira, já que a verificação de existência do Customer não participa da corrida. Sob o Serializable Snapshot Isolation do PostgreSQL, se as leituras de duas transações realmente se sobrepõem antes de qualquer uma delas fazer commit, o PostgreSQL detecta a anomalia no momento do commit e aborta um dos lados com um `40001 serialization_failure`.

**Camada 2 — Optimistic Concurrency (`xmin`/`RowVersion`).** Mesmo sem qualquer sobreposição nas fases de leitura das transações, tanto `Property` quanto `RentalProposal` carregam um `RowVersion` mapeado para o `xmin` do PostgreSQL. Todo `UPDATE` gerado pelo EF Core inclui `WHERE Id = @id AND xmin = @originalXmin`; se outra transação já fez commit de uma alteração naquela linha, o segundo `UPDATE` afeta zero linhas e o EF Core lança `DbUpdateConcurrencyException`.

**Ambas as exceções são mapeadas para `409 Conflict` em um único lugar**: `ExceptionHandlingMiddleware` captura especificamente `DbUpdateConcurrencyException` e `DbUpdateException` que encapsula uma `PostgresException` com `SqlState == PostgresErrorCodes.SerializationFailure`, registra cada uma como `Warning` (não `Error` — são resultados esperados e passíveis de nova tentativa de uma corrida legítima, não bugs), e retorna `409` para ambas. Tudo o mais permanece como um `500` inesperado.

Na prática, para `POST /proposals` isso significa: se a leitura da segunda requisição acontece *depois* que a primeira já fez commit, a verificação explícita `property.Status != Available` da Rule 2 já retorna um `409 ConflictError` — nenhuma exceção envolvida. Somente na janela estreita em que as leituras de ambas as requisições realmente se sobrepõem é que o abort do Serializable-isolation ou o mismatch do `xmin` entram em ação. Externamente, os três caminhos são indistinguíveis: exatamente uma requisição sempre tem sucesso, e a perdedora sempre recebe `409`. A classe de teste de integração `ConcurrencyTests` dispara duas requisições verdadeiramente paralelas (via `Task.WhenAll` e dois `HttpClient`s separados atingindo o mesmo container real de PostgreSQL) para verificar isso de ponta a ponta, tanto para `POST /proposals` (duas propostas disputando um mesmo Property) quanto para `PATCH /proposals/{id}/status` (duas atualizações disputando uma mesma Proposal).

---

# Testes de Integração com Testcontainers

O `Architecture.md` pede Testes de Integração focados em "REST Endpoints, Database, Transactions, Concurrency, History, Event Simulation" — nenhum dos quais pode ser verificado de forma significativa contra repositórios mockados, como fazem os Testes Unitários. `RentalPipeline.IntegrationTests` inicializa a API real em memória via `WebApplicationFactory<Program>`, apoiada por uma instância real e efêmera de PostgreSQL iniciada com **Testcontainers**, em vez de um "banco de dados de teste dedicado" mantido manualmente — isso evita qualquer banco de dados de teste compartilhado e stateful que poderia sofrer drift ou ficar sujo entre execuções, não exige nenhuma configuração manual além de ter o Docker disponível, e roda de forma idêntica em qualquer máquina e no CI.

Pontos-chave de design:

- **Um container por execução de testes, não por classe de teste.** `RentalPipelineApiFactory` (`IAsyncLifetime`) inicia um único container `postgres:16` e aplica as migrações do EF Core uma vez em `InitializeAsync`. Todas as classes de teste o compartilham através de um único `[CollectionDefinition]`/`ICollectionFixture`, já que iniciar um container novo por classe seria proibitivamente lento. Como o xUnit nunca paraleliza testes dentro de uma mesma collection, compartilhar o banco de dados é seguro desde que cada teste crie seu próprio Property/Customer/Proposal com nome aleatório (veja `TestDataFactory`), em vez de assumir um banco de dados intocado — a única exceção deliberada sendo `ConcurrencyTests`, que dispara intencionalmente requisições verdadeiramente paralelas contra dados que acabou de criar.
- **A simulação de eventos é verificada por asserção, não inferida a partir de logs.** O `FakeEventPublisher` (a implementação real de `IEventPublisher`) apenas registra logs, o que os testes de integração não conseguem verificar facilmente por asserção. `RentalPipelineApiFactory` o substitui por um `RecordingEventPublisher`, um test double, via `ConfigureTestServices`, para que os testes possam afirmar que um `ContractActivatedEvent` foi de fato publicado para um par proposta/imóvel específico quando uma proposta atinge `Active`.
- **O formato JSON do enum precisa corresponder ao da API real.** A API serializa enums como strings através de um `JsonStringEnumConverter` registrado apenas no `AddJsonOptions` do pipeline do MVC — isso não se aplica às chamadas `PostAsJsonAsync`/`ReadFromJsonAsync` de um `HttpClient` puro. `TestJsonOptions.Default` espelha essa configuração para que o código de teste fale exatamente o mesmo formato JSON que a API real.
- **A configuração passa sempre por HTTP, nunca diretamente pelo `DbContext`.** `TestDataFactory` cria Properties/Customers/Proposals chamando os endpoints reais, de modo que todo teste — incluindo sua própria configuração — exercita o pipeline completo de requisições (validação, mapeamento, persistência), em vez de inserir dados através de um atalho.

---

# Por que Não Redis?

O Redis Distributed Lock foi intencionalmente não implementado.

Motivos:

- O PostgreSQL já fornece as garantias de consistência necessárias.
- Introduzir o Redis aumentaria a complexidade arquitetural.
- A carga de trabalho esperada deste desafio não justifica um componente de infraestrutura adicional.

O Redis está documentado como uma possível melhoria futura para ambientes distribuídos de alta escala.

---

# Mapeamento de Result<T> para ProblemDetails

Os controllers nunca constroem respostas HTTP manualmente nem contêm blocos try/catch. Um único conjunto de métodos de extensão de `ControllerBase` (`ResultExtensions`) traduz todo resultado `Result`/`Result<T>` em uma resposta HTTP:

- Sucesso → `200 OK` / `201 Created` (com header `Location`) / `204 No Content`, dependendo do método de extensão que o controller chama.
- Falha → uma resposta ProblemDetails RFC 7807, construída através do próprio helper `ControllerBase.Problem(...)` do ASP.NET Core (que já preenche o campo `type` com o link correto para a seção de status da RFC 9110).

O tipo concreto de erro da camada de Application define o status HTTP e o título:

- `NotFoundError` → `404`, título `"Not Found"`
- `ConflictError` → `409`, título `"Conflict"`
- `BusinessRuleViolationError` → `400`, título `"Business Rule Violation"` (correspondendo literalmente ao título do exemplo de ProblemDetails do próprio `Architecture.md`)
- Qualquer outro erro/erro desconhecido → `400`, título `"Bad Request"`, como um fallback seguro

Isso mantém o mapeamento de erro para status em exatamente um único lugar, em vez de espalhar chamadas a `NotFound()`/`Conflict()`/`BadRequest()` por todas as actions dos controllers.

---

# Por que um Validation Filter Customizado em vez do FluentValidation.AspNetCore

O `Architecture.md` pede que o pipeline do FluentValidation valide automaticamente as requisições recebidas e retorne `400` automaticamente, sem que controllers ou DTOs chamem validators explicitamente.

A forma histórica de conseguir isso era o pacote `FluentValidation.AspNetCore`, mas seu autor o descontinuou e parou de mantê-lo em 2021, recomendando explicitamente que os consumidores implementassem o comportamento equivalente por conta própria, em vez de depender dele.

Este projeto segue essa recomendação: `Api/Filters/ValidationFilter` é um `IAsyncActionFilter` simples, registrado uma única vez globalmente (`AddControllers(o => o.Filters.Add<ValidationFilter>())`), que resolve um `IValidator<T>` para cada argumento de action (se houver um registrado na DI), o executa e — em caso de falha — interrompe o pipeline com um `400` construído através do próprio `ProblemDetailsFactory.CreateValidationProblemDetails` do framework, produzindo exatamente o mesmo formato que a validação nativa por Data Annotations do ASP.NET Core produziria. Nenhum pacote extra e sem manutenção é necessário.

---

# Chaves Guid Geradas pelo Cliente Exigem `ValueGeneratedNever()`

Todas as entidades geram sua própria chave primária no lado do cliente, dentro do construtor (`Id = Guid.NewGuid()`), em vez de depender do banco de dados ou do EF Core para gerá-la. Durante os testes manuais de ponta a ponta contra uma instância real de PostgreSQL, isso revelou um bug sutil de change tracking do EF Core:

Toda transição de status de `RentalProposal` adiciona uma nova entrada de `ProposalStatusHistory` à coleção `_statusHistory` do aggregate. Para a *primeira* entrada — criada dentro do construtor de `RentalProposal`, antes de o aggregate ser adicionado ao `DbSet` — isso funcionava corretamente, porque o EF Core propaga o estado `Added` para todo o grafo de objetos quando um aggregate root é explicitamente adicionado através do repository. Mas para toda entrada *subsequente* — adicionada depois que a proposta já havia sido carregada e rastreada (por exemplo, dentro de `UpdateStatusAsync`) — o change tracker do EF Core descobre o novo objeto `ProposalStatusHistory` puramente através de navegação/fixup do grafo, e não através de uma chamada explícita a `Add()`. Nessa situação, a heurística padrão do EF Core para decidir entre `Added` e `Modified` é "a chave primária é igual ao valor padrão do CLR (`Guid.Empty`)?" — e como nossos Guids já vêm sempre preenchidos pelo construtor, cada uma dessas entradas era classificada incorretamente como uma linha *existente* sendo modificada, produzindo uma instrução `UPDATE` contra uma linha que ainda não existia, e falhando com `DbUpdateConcurrencyException: expected to affect 1 row(s), but actually affected 0`.

A correção: toda configuração de entidade declara explicitamente `.Property(x => x.Id).ValueGeneratedNever()`. Isso informa ao EF Core que a aplicação sempre é dona da geração de chaves, removendo a ambiguidade do "valor padrão" — qualquer entidade não rastreada descoberta no grafo, independentemente do valor de sua chave, agora é corretamente tratada como `Added`. Confirmado via `dotnet ef migrations add` que essa é uma alteração puramente de metadados/tracking do EF Core, sem nenhum impacto real de SQL/schema (uma migração vazia foi gerada e então removida).

Essa é uma boa ilustração de por que "testar manualmente cada endpoint" é um passo obrigatório, e não apenas um "seria bom fazer" opcional: esse bug era invisível para os testes unitários (que mockam os repositories) e só reproduzível contra um banco de dados real com um change tracker real.

---

# Comportamento da API Independente de Localidade (Mensagens de Validação, Serialização de Enum)

Duas pequenas, porém importantes, decisões de polimento, ambas surgidas durante testes manuais em uma máquina de desenvolvimento pt-BR:

- **Mensagens do FluentValidation**: por padrão, o FluentValidation localiza suas mensagens embutidas com base na cultura da thread em execução. Em um sistema operacional pt-BR, isso produzia silenciosamente mensagens de erro de validação em português nas respostas da API — inconsistente com o restante da API (em inglês) e dependente da localidade do ambiente de implantação, o que não é aceitável para uma API de produção. Corrigido com `ValidatorOptions.Global.LanguageManager.Enabled = false`, que força as mensagens padrão em inglês em todos os lugares, independentemente das configurações de OS/cultura do host.
- **Serialização de enum**: os enums eram serializados como inteiros brutos por padrão (por exemplo, `"status": 0`), o que é tecnicamente correto, mas prejudica a ergonomia da API e a documentação do Swagger. Um `JsonStringEnumConverter` global (registrado via `AddJsonOptions`) faz com que tanto os payloads JSON quanto o schema do Swagger gerado usem o nome do enum (por exemplo, `"status": "Available"`) em vez disso.

---

# Migrações de Banco de Dados Aplicadas Automaticamente na Inicialização

`docker compose up` (e qualquer outra forma de implantação da API) precisa subir um banco de dados totalmente funcional e migrado, com zero passos manuais — sem a ferramenta `dotnet-ef`, sem a necessidade de um comando `dotnet ef database update` separado no host ou no container.

`Program.cs` chama `dbContext.Database.MigrateAsync()` uma única vez na inicialização, logo após o host ser construído e antes de o pipeline HTTP ser configurado, de modo que a API nunca começa a aceitar requisições contra um schema desatualizado. Isso funciona de forma idêntica seja a API iniciada via `dotnet run` no host, seja via imagem Docker, e é exatamente o que a `RentalPipelineApiFactory` (o host de testes de integração) já fazia de forma independente — agora os dois estão consistentes.

Trade-off reconhecido: para uma implantação de maior escala, com múltiplas instâncias, aplicar migrações a partir do próprio caminho de inicialização da aplicação geralmente é desencorajado (múltiplas instâncias poderiam disputar a aplicação da mesma migração, e uma migração ruim bloquearia a inicialização de todas as instâncias, em vez de ser validada como uma etapa de release separada e controlada). Para o modelo de implantação de instância única deste projeto, o benefício da simplicidade e da ausência de configuração manual supera esse risco; uma etapa de migração dedicada (um job/container avulso de `dotnet ef database update`, executado antes de as instâncias da API subirem) é a alternativa documentada para uma evolução deste projeto rumo a um cenário de produção com múltiplas instâncias.

---

# Ajustes Finos na Imagem Docker

Dois pequenos problemas surgiram ao validar o `docker compose up` de ponta a ponta a partir de um estado limpo, ambos corrigidos no Dockerfile/`Program.cs`, em vez de serem deixados como ruído nos logs:

- **`libgssapi-krb5-2` ausente**: a imagem de runtime `mcr.microsoft.com/dotnet/aspnet:10.0` não inclui essa biblioteca de sistema. O Npgsql sonda oportunisticamente o suporte a GSSAPI (Kerberos) no momento da conexão, independentemente de ele ser realmente usado, e sem a biblioteca isso imprimia `Cannot load library libgssapi_krb5.so.2` / `Error: ... cannot open shared object file` no stdout a cada início de container — inofensivo (o projeto usa apenas autenticação por senha), mas de aparência alarmante nos logs. Corrigido instalando `libgssapi-krb5-2` via `apt-get` no estágio final da imagem.
- **`UseHttpsRedirection` dentro do container**: a imagem Docker expõe apenas HTTP simples na porta 8080 (veja `docker-compose.yml`/`Dockerfile`), sem nenhuma vinculação HTTPS, então `app.UseHttpsRedirection()` nunca conseguiria encontrar uma porta HTTPS para redirecionar, registrando um warning `Failed to determine the https port for redirect` em *toda única requisição*. `DOTNET_RUNNING_IN_CONTAINER` é definido automaticamente como `true` pelas imagens base oficiais de container do .NET da Microsoft, então o `Program.cs` agora pula o `UseHttpsRedirection()` quando essa variável está definida, mantendo-o para o `dotnet run` local (onde o perfil de certificado de desenvolvimento HTTPS do Kestrel está disponível e o redirecionamento faz sentido).

---

# Melhorias Futuras

A arquitetura atual foi projetada para suportar evolução futura com alterações mínimas.

Possíveis melhorias futuras incluem:

- Detalhes financeiros nas propostas (valor do aluguel, depósitos, taxas, condições de pagamento e validações relacionadas)
- RabbitMQ
- Outbox Pattern
- Autenticação JWT
- Autorização
- Redis Cache
- Redis Distributed Lock
- OpenTelemetry
- Health Checks
- Rate Limiting
- API Versioning
- Background Jobs
- Pipeline de CI/CD
- GitHub Actions
- Kubernetes
- Horizontal Scaling

As decisões arquiteturais adotadas neste projeto têm como objetivo manter essas melhorias futuras isoladas da lógica de negócio principal.
