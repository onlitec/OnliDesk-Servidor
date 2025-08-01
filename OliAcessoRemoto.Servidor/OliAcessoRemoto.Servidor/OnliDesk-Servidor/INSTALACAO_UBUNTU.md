# Guia de Instalação - OnliDesk Servidor no Ubuntu

Este guia fornece instruções completas para instalar e configurar o OnliDesk Servidor em um servidor Ubuntu usando o repositório GitHub.

## 📋 Pré-requisitos

- Ubuntu 20.04 LTS ou superior
- Acesso root ou sudo
- Conexão com a internet
- Pelo menos 2GB de RAM
- 10GB de espaço em disco disponível

## 🔧 Passo 1: Atualizar o Sistema

```bash
sudo apt update && sudo apt upgrade -y
```

## 🔧 Passo 2: Instalar o .NET 9.0

### Adicionar o repositório da Microsoft:
```bash
# Baixar e instalar a chave de assinatura da Microsoft
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Atualizar a lista de pacotes
sudo apt update
```

### Instalar o .NET 9.0 SDK:
```bash
sudo apt install -y dotnet-sdk-9.0
```

### Verificar a instalação:
```bash
dotnet --version
```

## 🔧 Passo 3: Instalar Dependências Adicionais

```bash
# Git para clonar o repositório
sudo apt install -y git

# Nginx para proxy reverso (opcional)
sudo apt install -y nginx

# Supervisor para gerenciar o serviço (opcional)
sudo apt install -y supervisor
```

## 📥 Passo 4: Clonar o Repositório

```bash
# Navegar para o diretório de aplicações
cd /opt

# Clonar o repositório
sudo git clone https://github.com/onlitec/OnliDesk-Servidor.git

# Definir permissões
sudo chown -R $USER:$USER /opt/OnliDesk-Servidor
```

## 🔨 Passo 5: Compilar a Aplicação

```bash
# Navegar para o diretório do projeto
cd /opt/OnliDesk-Servidor

# Restaurar dependências
dotnet restore

# Compilar em modo Release
dotnet build --configuration Release

# Publicar a aplicação
dotnet publish --configuration Release --output ./publish
```

## ⚙️ Passo 6: Configurar o Ambiente de Produção

### Criar arquivo de configuração de produção:
```bash
sudo nano /opt/OnliDesk-Servidor/appsettings.Production.json
```

Conteúdo do arquivo:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://0.0.0.0:5165",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=onlidesk.db"
  }
}
```

## 🔧 Passo 7: Criar Serviço Systemd

```bash
sudo nano /etc/systemd/system/onlidesk-servidor.service
```

Conteúdo do arquivo:
```ini
[Unit]
Description=OnliDesk Servidor - Sistema de Acesso Remoto
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /opt/OnliDesk-Servidor/publish/OliAcessoRemoto.Servidor.dll
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=onlidesk-servidor
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
WorkingDirectory=/opt/OnliDesk-Servidor/publish

[Install]
WantedBy=multi-user.target
```

### Habilitar e iniciar o serviço:
```bash
# Recarregar configurações do systemd
sudo systemctl daemon-reload

# Habilitar o serviço para iniciar automaticamente
sudo systemctl enable onlidesk-servidor

# Iniciar o serviço
sudo systemctl start onlidesk-servidor

# Verificar status
sudo systemctl status onlidesk-servidor
```

## 🌐 Passo 8: Configurar Nginx (Proxy Reverso)

```bash
sudo nano /etc/nginx/sites-available/onlidesk-servidor
```

Conteúdo do arquivo:
```nginx
server {
    listen 80;
    server_name seu-dominio.com;  # Substitua pelo seu domínio

    location / {
        proxy_pass http://localhost:5165;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # Configuração para SignalR WebSockets
    location /hubs/ {
        proxy_pass http://localhost:5165;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### Habilitar o site:
```bash
# Criar link simbólico
sudo ln -s /etc/nginx/sites-available/onlidesk-servidor /etc/nginx/sites-enabled/

# Testar configuração
sudo nginx -t

# Reiniciar Nginx
sudo systemctl restart nginx
```

## 🔒 Passo 9: Configurar Firewall

```bash
# Permitir HTTP
sudo ufw allow 80

# Permitir HTTPS (se configurado SSL)
sudo ufw allow 443

# Permitir SSH (se necessário)
sudo ufw allow 22

# Habilitar firewall
sudo ufw enable
```

## 📊 Passo 10: Verificar Instalação

### Verificar se o serviço está rodando:
```bash
sudo systemctl status onlidesk-servidor
```

### Verificar logs:
```bash
# Logs do serviço
sudo journalctl -u onlidesk-servidor -f

# Logs do Nginx
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log
```

### Testar acesso:
```bash
# Teste do health check
curl http://localhost:5165/health

# Teste da página principal via aplicação
curl -I http://localhost:5165/

# Teste via Nginx
curl -I http://localhost/
```

### Verificar se a página de gerenciamento está sendo servida:
```bash
# Deve retornar HTML da página de gerenciamento
curl http://localhost/ | head -20

# Verificar se arquivos estáticos estão acessíveis
curl -I http://localhost/css/dashboard.css
curl -I http://localhost/js/dashboard.js
```

**Importante**: Se você estiver vendo apenas a página de health check em vez da interface de gerenciamento, verifique:

1. **Ordem das rotas no Program.cs**: O endpoint `/health` deve vir antes do fallback
2. **Arquivos estáticos**: Certifique-se de que a pasta `wwwroot` foi copiada corretamente
3. **Permissões**: Verifique se o usuário `www-data` tem acesso aos arquivos

### Solução de problemas comuns:

#### Problema: Página de health em vez da interface de gerenciamento
```bash
# Verificar se os arquivos estáticos existem
ls -la /opt/OnliDesk-Servidor/publish/wwwroot/

# Se não existirem, recompilar com arquivos estáticos
cd /opt/OnliDesk-Servidor
dotnet publish --configuration Release --output ./publish

# Reiniciar o serviço
sudo systemctl restart onlidesk-servidor
```

## 🔄 Comandos de Manutenção

### Atualizar a aplicação:
```bash
# Parar o serviço
sudo systemctl stop onlidesk-servidor

# Navegar para o diretório
cd /opt/OnliDesk-Servidor

# Fazer pull das atualizações
sudo git pull origin master

# Recompilar
dotnet publish --configuration Release --output ./publish

# Reiniciar o serviço
sudo systemctl start onlidesk-servidor
```

### Backup da aplicação:
```bash
# Criar backup
sudo tar -czf /backup/onlidesk-servidor-$(date +%Y%m%d).tar.gz /opt/OnliDesk-Servidor
```

## 🔧 Configuração SSL (Opcional)

Para configurar SSL com Let's Encrypt:

```bash
# Instalar Certbot
sudo apt install -y certbot python3-certbot-nginx

# Obter certificado SSL
sudo certbot --nginx -d seu-dominio.com

# Renovação automática
sudo crontab -e
# Adicionar linha: 0 12 * * * /usr/bin/certbot renew --quiet
```

## 📝 Logs e Monitoramento

### Localização dos logs:
- **Aplicação**: `sudo journalctl -u onlidesk-servidor`
- **Nginx**: `/var/log/nginx/`
- **Sistema**: `/var/log/syslog`

### Monitoramento de recursos:
```bash
# CPU e memória
htop

# Espaço em disco
df -h

# Status dos serviços
sudo systemctl status onlidesk-servidor nginx
```

## 🆘 Solução de Problemas

### Problema: Serviço não inicia
```bash
# Verificar logs detalhados
sudo journalctl -u onlidesk-servidor -n 50

# Verificar permissões
sudo chown -R www-data:www-data /opt/OnliDesk-Servidor
```

### Problema: Erro de porta em uso
```bash
# Verificar processos usando a porta
sudo netstat -tlnp | grep :5165
sudo lsof -i :5165
```

### Problema: Erro de dependências .NET
```bash
# Reinstalar .NET
sudo apt remove --purge dotnet-sdk-9.0
sudo apt install -y dotnet-sdk-9.0
```

## 📞 Suporte

Para suporte técnico ou dúvidas:
- **Repositório**: https://github.com/onlitec/OnliDesk-Servidor
- **Issues**: https://github.com/onlitec/OnliDesk-Servidor/issues

---

**Nota**: Este guia assume uma instalação padrão do Ubuntu. Ajuste as configurações conforme necessário para seu ambiente específico.