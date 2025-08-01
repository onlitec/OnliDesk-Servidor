# 🚀 Instalação Rápida - OnliDesk Servidor

## Instalação Automatizada (Recomendada)

Para uma instalação rápida e automatizada no Ubuntu:

```bash
# Clonar o repositório
git clone https://github.com/onlitec/OnliDesk-Servidor.git
cd OnliDesk-Servidor

# Executar script de instalação
bash install-onlidesk.sh
```

O script irá:
- ✅ Instalar .NET 9.0
- ✅ Configurar Nginx como proxy reverso
- ✅ Criar serviço systemd
- ✅ Configurar firewall
- ✅ Iniciar a aplicação automaticamente

## Instalação Manual

Para instalação manual detalhada, consulte: [INSTALACAO_UBUNTU.md](./INSTALACAO_UBUNTU.md)

## Requisitos Mínimos

- **SO**: Ubuntu 20.04 LTS ou superior
- **RAM**: 2GB mínimo
- **Disco**: 10GB disponível
- **Rede**: Conexão com internet

## Portas Utilizadas

- **5165**: Aplicação .NET (interna)
- **80**: Nginx proxy (externa)
- **443**: HTTPS (se SSL configurado)

## Comandos Úteis

```bash
# Status do serviço
sudo systemctl status onlidesk-servidor

# Ver logs em tempo real
sudo journalctl -u onlidesk-servidor -f

# Reiniciar serviço
sudo systemctl restart onlidesk-servidor

# Atualizar aplicação
sudo bash /opt/OnliDesk-Servidor/update.sh
```

## Acesso à Aplicação

Após a instalação, acesse:
- **Local**: http://localhost
- **Rede**: http://SEU_IP_SERVIDOR

## Suporte

- 📖 **Documentação completa**: [INSTALACAO_UBUNTU.md](./INSTALACAO_UBUNTU.md)
- 🐛 **Issues**: [GitHub Issues](https://github.com/onlitec/OnliDesk-Servidor/issues)
- 📧 **Contato**: Abra uma issue no repositório

---

**Desenvolvido por**: Onlitec  
**Repositório**: https://github.com/onlitec/OnliDesk-Servidor