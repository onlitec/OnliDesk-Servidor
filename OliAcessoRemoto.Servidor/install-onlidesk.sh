#!/bin/bash

# Script de Instalação Automatizada - OnliDesk Servidor
# Para Ubuntu 20.04+ 
# Uso: sudo bash install-onlidesk.sh

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Função para log
log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

error() {
    echo -e "${RED}[ERRO] $1${NC}"
    exit 1
}

warning() {
    echo -e "${YELLOW}[AVISO] $1${NC}"
}

info() {
    echo -e "${BLUE}[INFO] $1${NC}"
}

# Verificar se está rodando como root
if [[ $EUID -eq 0 ]]; then
   error "Este script não deve ser executado como root. Use: bash install-onlidesk.sh"
fi

# Verificar se sudo está disponível
if ! command -v sudo &> /dev/null; then
    error "sudo não está instalado. Instale sudo primeiro."
fi

log "Iniciando instalação do OnliDesk Servidor..."

# Passo 1: Atualizar sistema
log "Atualizando sistema..."
sudo apt update && sudo apt upgrade -y

# Passo 2: Instalar dependências básicas
log "Instalando dependências básicas..."
sudo apt install -y wget curl apt-transport-https software-properties-common

# Passo 3: Instalar .NET 9.0
log "Instalando .NET 9.0..."
if ! command -v dotnet &> /dev/null; then
    # Baixar e instalar chave Microsoft
    wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    rm packages-microsoft-prod.deb
    
    # Atualizar e instalar .NET
    sudo apt update
    sudo apt install -y dotnet-sdk-9.0
    
    # Verificar instalação
    if dotnet --version; then
        log ".NET 9.0 instalado com sucesso!"
    else
        error "Falha na instalação do .NET 9.0"
    fi
else
    info ".NET já está instalado: $(dotnet --version)"
fi

# Passo 4: Instalar Git, Nginx e Supervisor
log "Instalando Git, Nginx e outras dependências..."
sudo apt install -y git nginx supervisor

# Passo 5: Clonar repositório
log "Clonando repositório OnliDesk-Servidor..."
if [ -d "/opt/OnliDesk-Servidor" ]; then
    warning "Diretório /opt/OnliDesk-Servidor já existe. Fazendo backup..."
    sudo mv /opt/OnliDesk-Servidor /opt/OnliDesk-Servidor.backup.$(date +%Y%m%d_%H%M%S)
fi

sudo git clone https://github.com/onlitec/OnliDesk-Servidor.git /opt/OnliDesk-Servidor
sudo chown -R $USER:$USER /opt/OnliDesk-Servidor

# Passo 6: Compilar aplicação
log "Compilando aplicação..."
cd /opt/OnliDesk-Servidor

# Restaurar dependências
dotnet restore

# Compilar em Release
dotnet build --configuration Release

# Publicar aplicação (incluindo arquivos estáticos)
dotnet publish --configuration Release --output ./publish

# Verificar se os arquivos estáticos foram copiados
if [ ! -f "./publish/wwwroot/index.html" ]; then
    warning "Arquivos estáticos não foram copiados automaticamente. Copiando manualmente..."
    cp -r wwwroot ./publish/
fi

# Verificar novamente
if [ -f "./publish/wwwroot/index.html" ]; then
    log "✅ Arquivos estáticos copiados com sucesso!"
else
    error "❌ Falha ao copiar arquivos estáticos"
fi

# Passo 7: Criar configuração de produção
log "Criando configuração de produção..."
sudo tee /opt/OnliDesk-Servidor/appsettings.Production.json > /dev/null <<EOF
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
EOF

# Passo 8: Criar serviço systemd
log "Criando serviço systemd..."
sudo tee /etc/systemd/system/onlidesk-servidor.service > /dev/null <<EOF
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
EOF

# Definir permissões corretas
sudo chown -R www-data:www-data /opt/OnliDesk-Servidor

# Passo 9: Configurar Nginx
log "Configurando Nginx..."
sudo tee /etc/nginx/sites-available/onlidesk-servidor > /dev/null <<EOF
server {
    listen 80;
    server_name _;

    location / {
        proxy_pass http://localhost:5165;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }

    # Configuração para SignalR WebSockets
    location /hubs/ {
        proxy_pass http://localhost:5165;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
}
EOF

# Habilitar site
sudo ln -sf /etc/nginx/sites-available/onlidesk-servidor /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default

# Testar configuração Nginx
if sudo nginx -t; then
    log "Configuração do Nginx válida!"
else
    error "Erro na configuração do Nginx"
fi

# Passo 10: Configurar firewall
log "Configurando firewall..."
if command -v ufw &> /dev/null; then
    sudo ufw allow 22/tcp
    sudo ufw allow 80/tcp
    sudo ufw allow 443/tcp
    echo "y" | sudo ufw enable
    info "Firewall configurado (portas 22, 80, 443 abertas)"
else
    warning "UFW não está instalado. Configure o firewall manualmente."
fi

# Passo 11: Iniciar serviços
log "Iniciando serviços..."

# Recarregar systemd
sudo systemctl daemon-reload

# Habilitar e iniciar OnliDesk
sudo systemctl enable onlidesk-servidor
sudo systemctl start onlidesk-servidor

# Reiniciar Nginx
sudo systemctl restart nginx
sudo systemctl enable nginx

# Passo 12: Verificar instalação
log "Verificando instalação..."

sleep 5

if sudo systemctl is-active --quiet onlidesk-servidor; then
    log "✅ Serviço OnliDesk-Servidor está rodando!"
else
    error "❌ Falha ao iniciar o serviço OnliDesk-Servidor"
fi

if sudo systemctl is-active --quiet nginx; then
    log "✅ Nginx está rodando!"
else
    error "❌ Falha ao iniciar o Nginx"
fi

# Teste de conectividade
if curl -s http://localhost > /dev/null; then
    log "✅ Aplicação está respondendo!"
else
    warning "⚠️  Aplicação pode não estar respondendo corretamente"
fi

# Informações finais
log "🎉 Instalação concluída com sucesso!"
echo ""
info "📋 Informações importantes:"
echo "  • Aplicação rodando na porta: 5165"
echo "  • Nginx proxy na porta: 80"
echo "  • Logs do serviço: sudo journalctl -u onlidesk-servidor -f"
echo "  • Status do serviço: sudo systemctl status onlidesk-servidor"
echo "  • Reiniciar serviço: sudo systemctl restart onlidesk-servidor"
echo ""
info "🌐 Acesse a aplicação em: http://$(hostname -I | awk '{print $1}')"
echo ""
info "📚 Para mais informações, consulte: /opt/OnliDesk-Servidor/INSTALACAO_UBUNTU.md"

# Criar script de atualização
log "Criando script de atualização..."
sudo tee /opt/OnliDesk-Servidor/update.sh > /dev/null <<'EOF'
#!/bin/bash
set -e

echo "🔄 Atualizando OnliDesk Servidor..."

# Parar serviço
sudo systemctl stop onlidesk-servidor

# Fazer backup
sudo cp -r /opt/OnliDesk-Servidor/publish /opt/OnliDesk-Servidor/publish.backup.$(date +%Y%m%d_%H%M%S)

# Atualizar código
cd /opt/OnliDesk-Servidor
sudo git pull origin master

# Recompilar
dotnet publish --configuration Release --output ./publish

# Verificar se os arquivos estáticos foram copiados
if [ ! -f "./publish/wwwroot/index.html" ]; then
    echo "⚠️  Copiando arquivos estáticos manualmente..."
    cp -r wwwroot ./publish/
fi

# Definir permissões
sudo chown -R www-data:www-data /opt/OnliDesk-Servidor

# Reiniciar serviço
sudo systemctl start onlidesk-servidor

echo "✅ Atualização concluída!"
echo "🌐 Teste o acesso em: http://$(hostname -I | awk '{print $1}')"
EOF

sudo chmod +x /opt/OnliDesk-Servidor/update.sh

log "✅ Script de instalação finalizado!"
log "Para atualizar no futuro, execute: sudo bash /opt/OnliDesk-Servidor/update.sh"