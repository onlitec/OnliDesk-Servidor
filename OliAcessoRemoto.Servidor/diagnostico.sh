#!/bin/bash

# Script de Diagnóstico - OnliDesk Servidor
# Uso: bash diagnostico.sh

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔍 Diagnóstico do OnliDesk Servidor${NC}"
echo "========================================"

# Verificar se o serviço está rodando
echo -e "\n${BLUE}1. Status do Serviço${NC}"
if systemctl is-active --quiet onlidesk-servidor; then
    echo -e "✅ ${GREEN}Serviço OnliDesk está rodando${NC}"
else
    echo -e "❌ ${RED}Serviço OnliDesk NÃO está rodando${NC}"
    echo "   Execute: sudo systemctl start onlidesk-servidor"
fi

# Verificar Nginx
echo -e "\n${BLUE}2. Status do Nginx${NC}"
if systemctl is-active --quiet nginx; then
    echo -e "✅ ${GREEN}Nginx está rodando${NC}"
else
    echo -e "❌ ${RED}Nginx NÃO está rodando${NC}"
    echo "   Execute: sudo systemctl start nginx"
fi

# Verificar portas
echo -e "\n${BLUE}3. Verificação de Portas${NC}"
if netstat -tlnp | grep -q ":5165"; then
    echo -e "✅ ${GREEN}Porta 5165 (aplicação) está em uso${NC}"
    netstat -tlnp | grep ":5165"
else
    echo -e "❌ ${RED}Porta 5165 (aplicação) NÃO está em uso${NC}"
fi

if netstat -tlnp | grep -q ":80"; then
    echo -e "✅ ${GREEN}Porta 80 (Nginx) está em uso${NC}"
    netstat -tlnp | grep ":80"
else
    echo -e "❌ ${RED}Porta 80 (Nginx) NÃO está em uso${NC}"
fi

# Verificar arquivos estáticos
echo -e "\n${BLUE}4. Verificação de Arquivos Estáticos${NC}"
if [ -f "/opt/OnliDesk-Servidor/publish/wwwroot/index.html" ]; then
    echo -e "✅ ${GREEN}index.html encontrado${NC}"
else
    echo -e "❌ ${RED}index.html NÃO encontrado${NC}"
    echo "   Caminho esperado: /opt/OnliDesk-Servidor/publish/wwwroot/index.html"
fi

if [ -f "/opt/OnliDesk-Servidor/publish/wwwroot/css/dashboard.css" ]; then
    echo -e "✅ ${GREEN}dashboard.css encontrado${NC}"
else
    echo -e "❌ ${RED}dashboard.css NÃO encontrado${NC}"
fi

if [ -f "/opt/OnliDesk-Servidor/publish/wwwroot/js/dashboard.js" ]; then
    echo -e "✅ ${GREEN}dashboard.js encontrado${NC}"
else
    echo -e "❌ ${RED}dashboard.js NÃO encontrado${NC}"
fi

# Verificar permissões
echo -e "\n${BLUE}5. Verificação de Permissões${NC}"
OWNER=$(stat -c '%U' /opt/OnliDesk-Servidor/publish 2>/dev/null || echo "N/A")
if [ "$OWNER" = "www-data" ]; then
    echo -e "✅ ${GREEN}Permissões corretas (www-data)${NC}"
else
    echo -e "⚠️  ${YELLOW}Permissões podem estar incorretas (owner: $OWNER)${NC}"
    echo "   Execute: sudo chown -R www-data:www-data /opt/OnliDesk-Servidor"
fi

# Teste de conectividade
echo -e "\n${BLUE}6. Teste de Conectividade${NC}"

# Teste health check
if curl -s http://localhost:5165/health > /dev/null 2>&1; then
    echo -e "✅ ${GREEN}Health check respondendo (porta 5165)${NC}"
    HEALTH_RESPONSE=$(curl -s http://localhost:5165/health)
    echo "   Resposta: $HEALTH_RESPONSE"
else
    echo -e "❌ ${RED}Health check NÃO está respondendo (porta 5165)${NC}"
fi

# Teste página principal (aplicação direta)
echo -e "\n${BLUE}7. Teste da Página Principal${NC}"
RESPONSE_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5165/ 2>/dev/null || echo "000")
if [ "$RESPONSE_CODE" = "200" ]; then
    echo -e "✅ ${GREEN}Página principal respondendo (código: $RESPONSE_CODE)${NC}"
    
    # Verificar se é HTML
    CONTENT_TYPE=$(curl -s -I http://localhost:5165/ | grep -i "content-type" | head -1)
    if echo "$CONTENT_TYPE" | grep -q "text/html"; then
        echo -e "✅ ${GREEN}Retornando HTML corretamente${NC}"
    else
        echo -e "⚠️  ${YELLOW}Tipo de conteúdo: $CONTENT_TYPE${NC}"
    fi
else
    echo -e "❌ ${RED}Página principal NÃO está respondendo (código: $RESPONSE_CODE)${NC}"
fi

# Teste via Nginx
NGINX_RESPONSE_CODE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost/ 2>/dev/null || echo "000")
if [ "$NGINX_RESPONSE_CODE" = "200" ]; then
    echo -e "✅ ${GREEN}Nginx proxy funcionando (código: $NGINX_RESPONSE_CODE)${NC}"
else
    echo -e "❌ ${RED}Nginx proxy com problema (código: $NGINX_RESPONSE_CODE)${NC}"
fi

# Verificar logs recentes
echo -e "\n${BLUE}8. Logs Recentes (últimas 5 linhas)${NC}"
echo -e "${YELLOW}Logs do OnliDesk:${NC}"
sudo journalctl -u onlidesk-servidor --no-pager -n 5 2>/dev/null || echo "Não foi possível acessar os logs"

echo -e "\n${YELLOW}Logs do Nginx (erro):${NC}"
sudo tail -n 3 /var/log/nginx/error.log 2>/dev/null || echo "Arquivo de log não encontrado"

# Verificar configuração do .NET
echo -e "\n${BLUE}9. Verificação do .NET${NC}"
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo -e "✅ ${GREEN}.NET instalado: $DOTNET_VERSION${NC}"
else
    echo -e "❌ ${RED}.NET NÃO está instalado${NC}"
fi

# Resumo e recomendações
echo -e "\n${BLUE}📋 Resumo e Recomendações${NC}"
echo "========================================"

if systemctl is-active --quiet onlidesk-servidor && [ -f "/opt/OnliDesk-Servidor/publish/wwwroot/index.html" ]; then
    echo -e "✅ ${GREEN}Sistema parece estar funcionando corretamente!${NC}"
    echo -e "🌐 Acesse: http://$(hostname -I | awk '{print $1}')/"
else
    echo -e "⚠️  ${YELLOW}Problemas detectados. Soluções sugeridas:${NC}"
    
    if ! systemctl is-active --quiet onlidesk-servidor; then
        echo "   • Iniciar o serviço: sudo systemctl start onlidesk-servidor"
    fi
    
    if [ ! -f "/opt/OnliDesk-Servidor/publish/wwwroot/index.html" ]; then
        echo "   • Recompilar com arquivos estáticos:"
        echo "     cd /opt/OnliDesk-Servidor"
        echo "     dotnet publish --configuration Release --output ./publish"
        echo "     cp -r wwwroot ./publish/"
        echo "     sudo systemctl restart onlidesk-servidor"
    fi
    
    if [ "$OWNER" != "www-data" ]; then
        echo "   • Corrigir permissões: sudo chown -R www-data:www-data /opt/OnliDesk-Servidor"
    fi
fi

echo -e "\n${BLUE}Para mais ajuda, consulte:${NC}"
echo "• Logs detalhados: sudo journalctl -u onlidesk-servidor -f"
echo "• Documentação: /opt/OnliDesk-Servidor/INSTALACAO_UBUNTU.md"
echo "• Issues: https://github.com/onlitec/OnliDesk-Servidor/issues"