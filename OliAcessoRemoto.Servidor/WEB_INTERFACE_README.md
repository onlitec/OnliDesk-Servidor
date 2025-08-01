# Interface Web de Monitoramento - OliAcesso Remoto

## Visão Geral

Esta interface web fornece um sistema completo de monitoramento e gerenciamento para o servidor OliAcesso Remoto. A interface permite visualizar conexões ativas, histórico de conexões, relatórios, estatísticas e informações do sistema em tempo real.

## Funcionalidades Implementadas

### 1. Dashboard Principal
- **Estatísticas em Tempo Real**: Conexões ativas, conexões do dia, uso de CPU e memória
- **Gráficos Interativos**: 
  - Conexões por hora (gráfico de linha)
  - Top países (gráfico de pizza)
  - Recursos do sistema (gráfico de rosca)
- **Tabela de Conexões Ativas**: Lista todas as conexões ativas com opção de desconectar

### 2. Monitoramento de Conexões
- **Histórico Completo**: Visualização de todas as conexões passadas
- **Informações Detalhadas**: IP, país, duração, dados transferidos
- **Paginação**: Navegação eficiente através do histórico
- **Atualização em Tempo Real**: Via SignalR

### 3. Relatórios e Estatísticas
- **Geração de Relatórios**: 
  - Relatório de conexões (CSV)
  - Métricas do sistema (CSV)
- **Filtros por Data**: Período personalizável
- **Estatísticas Detalhadas**:
  - Total de conexões e taxa de sucesso
  - Duração média das sessões
  - Dados transferidos
  - Uso médio de recursos

### 4. Informações do Sistema
- **Monitoramento de Recursos**: CPU, memória, disco
- **Informações do Hardware**: Processador, arquitetura, SO
- **Interfaces de Rede**: Status e estatísticas de tráfego
- **Atualização Automática**: A cada 30 segundos

## Arquitetura Técnica

### Frontend
- **HTML5 + CSS3**: Interface responsiva e moderna
- **Bootstrap 5**: Framework CSS para design consistente
- **JavaScript ES6**: Lógica da aplicação
- **Chart.js**: Visualização de dados em gráficos
- **SignalR Client**: Comunicação em tempo real
- **Font Awesome**: Ícones

### Backend
- **ASP.NET Core 9.0**: Framework web
- **SignalR**: Comunicação em tempo real
- **Entity Framework Core**: ORM para acesso a dados
- **Serilog**: Sistema de logging
- **In-Memory Database**: Armazenamento temporário

### Estrutura de Arquivos
```
wwwroot/
├── index.html          # Página principal
├── css/
│   └── dashboard.css   # Estilos personalizados
└── js/
    └── dashboard.js    # Lógica da aplicação

Controllers/
├── DashboardController.cs  # API do dashboard
└── ReportsController.cs    # API de relatórios

Hubs/
├── RemoteAccessHub.cs      # Hub para acesso remoto
└── MonitoringHub.cs        # Hub para monitoramento

Services/
├── ConnectionService.cs    # Gerenciamento de conexões
├── MonitoringService.cs    # Serviços de monitoramento
├── SystemInfoService.cs    # Informações do sistema
└── ReportService.cs        # Geração de relatórios

Models/
├── ClientConnection.cs     # Modelo de conexão
├── ServerModels.cs         # Modelos do servidor
└── MonitoringDTOs.cs       # DTOs para API

Data/
└── ApplicationDbContext.cs # Contexto do banco de dados
```

## APIs Disponíveis

### Dashboard
- `GET /api/dashboard/stats` - Estatísticas do dashboard
- `GET /api/dashboard/system-info` - Informações do sistema
- `GET /api/dashboard/active-connections` - Conexões ativas
- `GET /api/dashboard/connections-history` - Histórico de conexões
- `POST /api/dashboard/disconnect/{id}` - Desconectar cliente
- `GET /api/dashboard/system-metrics` - Métricas do sistema

### Relatórios
- `GET /api/reports/connections` - Relatório de conexões (CSV)
- `GET /api/reports/system-metrics` - Relatório de métricas (CSV)
- `GET /api/reports/statistics` - Estatísticas detalhadas
- `GET /api/reports/available` - Lista de relatórios disponíveis

## SignalR Hubs

### MonitoringHub (`/monitoringhub`)
- `JoinDashboard()` - Entrar no grupo de monitoramento
- `LeaveDashboard()` - Sair do grupo de monitoramento
- `RequestDashboardUpdate()` - Solicitar atualização do dashboard
- `RequestSystemInfoUpdate()` - Solicitar atualização do sistema
- `RequestActiveConnectionsUpdate()` - Solicitar atualização das conexões

### RemoteAccessHub (`/remotehub`)
- `RegisterClient()` - Registrar cliente
- `RequestConnection()` - Solicitar conexão
- `SendScreenData()` - Enviar dados da tela
- `SendInputEvent()` - Enviar eventos de entrada

## Como Usar

### Acesso à Interface
1. Inicie o servidor: `dotnet run`
2. Acesse: `http://localhost:5165`
3. A interface carregará automaticamente

### Navegação
- **Dashboard**: Visão geral em tempo real
- **Conexões**: Histórico e gerenciamento
- **Relatórios**: Geração e estatísticas
- **Sistema**: Informações do servidor

### Funcionalidades em Tempo Real
- As estatísticas são atualizadas automaticamente a cada 30 segundos
- Conexões ativas são monitoradas em tempo real
- Alertas são exibidos para eventos importantes

## Configuração

### Dependências
As seguintes dependências são necessárias no arquivo `.csproj`:
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="1.1.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
<PackageReference Include="System.Diagnostics.PerformanceCounter" Version="9.0.0" />
```

### Configuração do Servidor
O servidor é configurado automaticamente no `Program.cs` com:
- SignalR para comunicação em tempo real
- Entity Framework com banco em memória
- Serilog para logging
- CORS para desenvolvimento
- Swagger para documentação da API

## Segurança

### Medidas Implementadas
- Validação de entrada em todas as APIs
- Sanitização de dados
- Logs de segurança
- Controle de acesso via CORS

### Recomendações para Produção
- Implementar autenticação e autorização
- Usar HTTPS obrigatório
- Configurar banco de dados persistente
- Implementar rate limiting
- Configurar logs centralizados

## Monitoramento e Logs

### Logs Disponíveis
- Conexões de clientes
- Tentativas de conexão
- Erros do sistema
- Métricas de performance

### Localização dos Logs
- Console durante desenvolvimento
- Arquivos de log em produção (configurável via Serilog)

## Troubleshooting

### Problemas Comuns
1. **SignalR não conecta**: Verificar se o hub está registrado
2. **Gráficos não carregam**: Verificar se Chart.js está carregado
3. **Dados não atualizam**: Verificar conexão SignalR no console

### Debug
- Abrir DevTools do navegador
- Verificar console para erros JavaScript
- Verificar aba Network para falhas de API
- Verificar logs do servidor

## Desenvolvimento Futuro

### Melhorias Planejadas
- Autenticação de usuários
- Configurações personalizáveis
- Alertas por email/SMS
- Backup automático
- Clustering para alta disponibilidade
- Mobile app companion

### Extensibilidade
A arquitetura modular permite fácil extensão com:
- Novos tipos de relatórios
- Métricas customizadas
- Integrações externas
- Plugins de monitoramento