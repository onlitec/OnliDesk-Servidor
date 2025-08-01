// Dashboard JavaScript
class Dashboard {
    constructor() {
        this.connection = null;
        this.charts = {};
        this.currentSection = 'dashboard';
        this.refreshInterval = null;
        this.init();
    }

    async init() {
        this.setupSignalR();
        this.setupNavigation();
        this.setupEventListeners();
        this.initializeCharts();
        await this.loadInitialData();
        this.startAutoRefresh();
    }

    // SignalR Setup
    setupSignalR() {
        // Check if signalR is available
        if (typeof signalR === 'undefined') {
            console.error('SignalR not loaded');
            this.updateServerStatus(false);
            return;
        }
        
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/monitoringhub")
            .withAutomaticReconnect()
            .build();

        this.connection.start().then(() => {
            console.log('SignalR Connected');
            this.connection.invoke("JoinDashboard");
            this.updateServerStatus(true);
        }).catch(err => {
            console.error('SignalR Connection Error:', err);
            this.updateServerStatus(false);
        });

        // SignalR event handlers
        this.connection.on("DashboardStatsUpdated", (stats) => {
            this.updateDashboardStats(stats);
        });

        this.connection.on("SystemInfoUpdated", (systemInfo) => {
            this.updateSystemInfo(systemInfo);
        });

        this.connection.on("ActiveConnectionsUpdated", (connections) => {
            this.updateActiveConnections(connections);
        });

        this.connection.onreconnected(() => {
            this.updateServerStatus(true);
            this.connection.invoke("JoinDashboard");
        });

        this.connection.onclose(() => {
            this.updateServerStatus(false);
        });
    }

    // Navigation Setup
    setupNavigation() {
        document.querySelectorAll('[data-section]').forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const section = e.target.closest('[data-section]').dataset.section;
                this.showSection(section);
            });
        });
    }

    showSection(sectionName) {
        // Hide all sections
        document.querySelectorAll('.content-section').forEach(section => {
            section.style.display = 'none';
        });

        // Show selected section
        const targetSection = document.getElementById(`${sectionName}-section`);
        if (targetSection) {
            targetSection.style.display = 'block';
            this.currentSection = sectionName;
        }

        // Update navigation
        document.querySelectorAll('.nav-link').forEach(link => {
            link.classList.remove('active');
        });
        document.querySelector(`[data-section="${sectionName}"]`).classList.add('active');

        // Load section-specific data
        this.loadSectionData(sectionName);
    }

    // Event Listeners
    setupEventListeners() {
        // Report form
        document.getElementById('report-form').addEventListener('submit', (e) => {
            e.preventDefault();
            this.generateReport();
        });

        // Set default dates
        const today = new Date().toISOString().split('T')[0];
        const lastWeek = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
        document.getElementById('date-to').value = today;
        document.getElementById('date-from').value = lastWeek;
    }

    // Charts Initialization
    initializeCharts() {
        // Check if Chart.js is available
        if (typeof Chart === 'undefined') {
            console.error('Chart.js not loaded');
            return;
        }
        
        // Connections Chart
        const connectionsCtx = document.getElementById('connectionsChart').getContext('2d');
        this.charts.connections = new Chart(connectionsCtx, {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Conexões',
                    data: [],
                    borderColor: '#4e73df',
                    backgroundColor: 'rgba(78, 115, 223, 0.1)',
                    borderWidth: 2,
                    fill: true,
                    tension: 0.3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1
                        }
                    }
                }
            }
        });

        // Countries Chart
        const countriesCtx = document.getElementById('countriesChart').getContext('2d');
        this.charts.countries = new Chart(countriesCtx, {
            type: 'doughnut',
            data: {
                labels: [],
                datasets: [{
                    data: [],
                    backgroundColor: [
                        '#4e73df',
                        '#1cc88a',
                        '#36b9cc',
                        '#f6c23e',
                        '#e74a3b'
                    ]
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });

        // System Resources Chart
        const systemCtx = document.getElementById('systemResourcesChart').getContext('2d');
        this.charts.systemResources = new Chart(systemCtx, {
            type: 'doughnut',
            data: {
                labels: ['CPU', 'Memória', 'Disco'],
                datasets: [{
                    data: [0, 0, 0],
                    backgroundColor: ['#36b9cc', '#f6c23e', '#e74a3b']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom'
                    }
                }
            }
        });
    }

    // Data Loading
    async loadInitialData() {
        try {
            await Promise.all([
                this.loadDashboardStats(),
                this.loadSystemInfo(),
                this.loadActiveConnections()
            ]);
        } catch (error) {
            console.error('Error loading initial data:', error);
            this.showError('Erro ao carregar dados iniciais');
        }
    }

    async loadDashboardStats() {
        try {
            const response = await fetch('/api/dashboard/stats');
            const stats = await response.json();
            this.updateDashboardStats(stats);
        } catch (error) {
            console.error('Error loading dashboard stats:', error);
        }
    }

    async loadSystemInfo() {
        try {
            const response = await fetch('/api/dashboard/system-info');
            const systemInfo = await response.json();
            this.updateSystemInfo(systemInfo);
        } catch (error) {
            console.error('Error loading system info:', error);
        }
    }

    async loadActiveConnections() {
        try {
            const response = await fetch('/api/dashboard/active-connections');
            const connections = await response.json();
            this.updateActiveConnections(connections);
        } catch (error) {
            console.error('Error loading active connections:', error);
        }
    }

    async loadSectionData(section) {
        switch (section) {
            case 'connections':
                await this.loadConnectionsHistory();
                break;
            case 'reports':
                await this.loadStatistics();
                break;
            case 'system':
                await this.loadSystemMetrics();
                break;
        }
    }

    // Update Methods
    updateDashboardStats(stats) {
        document.getElementById('active-connections').textContent = stats.activeConnections;
        document.getElementById('connections-today').textContent = stats.connectionsToday;

        // Update connections chart
        if (stats.connectionsByHour) {
            const labels = stats.connectionsByHour.map(item => `${item.hour}:00`);
            const data = stats.connectionsByHour.map(item => item.count);
            
            this.charts.connections.data.labels = labels;
            this.charts.connections.data.datasets[0].data = data;
            this.charts.connections.update();
        }

        // Update countries chart
        if (stats.topCountries) {
            const labels = stats.topCountries.map(item => item.country);
            const data = stats.topCountries.map(item => item.count);
            
            this.charts.countries.data.labels = labels;
            this.charts.countries.data.datasets[0].data = data;
            this.charts.countries.update();
        }
    }

    updateSystemInfo(systemInfo) {
        const cpuUsage = Math.round(systemInfo.cpuUsage);
        const memoryUsage = Math.round(systemInfo.memoryUsage);
        const diskUsage = Math.round(systemInfo.diskUsage || 0);

        document.getElementById('cpu-usage').textContent = `${cpuUsage}%`;
        document.getElementById('cpu-progress').style.width = `${cpuUsage}%`;
        
        document.getElementById('memory-usage').textContent = `${memoryUsage}%`;
        document.getElementById('memory-progress').style.width = `${memoryUsage}%`;

        // Update system resources chart
        this.charts.systemResources.data.datasets[0].data = [cpuUsage, memoryUsage, diskUsage];
        this.charts.systemResources.update();

        // Update system info section
        if (this.currentSection === 'system') {
            this.updateSystemInfoSection(systemInfo);
        }
    }

    updateActiveConnections(connections) {
        const tbody = document.querySelector('#active-connections-table tbody');
        tbody.innerHTML = '';

        connections.forEach(connection => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${connection.clientName}</td>
                <td>${connection.ipAddress}</td>
                <td>${connection.country || 'N/A'}</td>
                <td>${this.formatDateTime(connection.connectedAt)}</td>
                <td>${this.formatDuration(connection.duration)}</td>
                <td>
                    <span class="badge status-${connection.isActive ? 'online' : 'offline'}">
                        <i class="fas fa-circle me-1"></i>
                        ${connection.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                </td>
                <td>
                    ${connection.isActive ? 
                        `<button class="btn btn-sm btn-danger" onclick="dashboard.disconnectClient('${connection.connectionId}')">
                            <i class="fas fa-times"></i> Desconectar
                        </button>` : 
                        '<span class="text-muted">-</span>'
                    }
                </td>
            `;
            tbody.appendChild(row);
        });
    }

    updateSystemInfoSection(systemInfo) {
        const content = document.getElementById('system-info-content');
        content.innerHTML = `
            <div class="row">
                <div class="col-md-6">
                    <h6>Informações Gerais</h6>
                    <table class="table table-sm">
                        <tr><td><strong>Sistema Operacional:</strong></td><td>${systemInfo.operatingSystem}</td></tr>
                        <tr><td><strong>Arquitetura:</strong></td><td>${systemInfo.architecture}</td></tr>
                        <tr><td><strong>Processador:</strong></td><td>${systemInfo.processorName}</td></tr>
                        <tr><td><strong>Memória Total:</strong></td><td>${this.formatBytes(systemInfo.totalMemory)}</td></tr>
                        <tr><td><strong>Memória Disponível:</strong></td><td>${this.formatBytes(systemInfo.availableMemory)}</td></tr>
                    </table>
                </div>
                <div class="col-md-6">
                    <h6>Interfaces de Rede</h6>
                    <div class="list-group">
                        ${systemInfo.networkInterfaces.map(ni => `
                            <div class="list-group-item">
                                <strong>${ni.name}</strong><br>
                                <small class="text-muted">
                                    ${ni.ipAddress} - ${ni.status}
                                    <br>Enviados: ${this.formatBytes(ni.bytesSent)} | Recebidos: ${this.formatBytes(ni.bytesReceived)}
                                </small>
                            </div>
                        `).join('')}
                    </div>
                </div>
            </div>
        `;
    }

    updateServerStatus(isOnline) {
        const statusElement = document.getElementById('server-status');
        if (isOnline) {
            statusElement.className = 'badge bg-success';
            statusElement.innerHTML = '<i class="fas fa-circle me-1"></i>Online';
        } else {
            statusElement.className = 'badge bg-danger';
            statusElement.innerHTML = '<i class="fas fa-circle me-1"></i>Offline';
        }
    }

    // Connection Management
    async disconnectClient(connectionId) {
        if (!confirm('Tem certeza que deseja desconectar este cliente?')) {
            return;
        }

        try {
            const response = await fetch(`/api/dashboard/disconnect/${connectionId}`, {
                method: 'POST'
            });

            if (response.ok) {
                this.showSuccess('Cliente desconectado com sucesso');
                await this.loadActiveConnections();
            } else {
                this.showError('Erro ao desconectar cliente');
            }
        } catch (error) {
            console.error('Error disconnecting client:', error);
            this.showError('Erro ao desconectar cliente');
        }
    }

    // Reports
    async generateReport() {
        const type = document.getElementById('report-type').value;
        const dateFrom = document.getElementById('date-from').value;
        const dateTo = document.getElementById('date-to').value;
        const format = document.getElementById('report-format').value;

        if (!type || !dateFrom || !dateTo) {
            this.showError('Preencha todos os campos obrigatórios');
            return;
        }

        try {
            const response = await fetch(`/api/reports/${type}?from=${dateFrom}&to=${dateTo}&format=${format}`);
            
            if (response.ok) {
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = `${type}_${dateFrom}_${dateTo}.${format}`;
                document.body.appendChild(a);
                a.click();
                window.URL.revokeObjectURL(url);
                document.body.removeChild(a);
                
                this.showSuccess('Relatório gerado com sucesso');
            } else {
                this.showError('Erro ao gerar relatório');
            }
        } catch (error) {
            console.error('Error generating report:', error);
            this.showError('Erro ao gerar relatório');
        }
    }

    async loadStatistics() {
        try {
            const response = await fetch('/api/reports/statistics');
            const stats = await response.json();
            
            const content = document.getElementById('statistics-content');
            content.innerHTML = `
                <div class="row">
                    <div class="col-md-6">
                        <h6>Conexões</h6>
                        <ul class="list-unstyled">
                            <li><strong>Total de Conexões:</strong> ${stats.totalConnections}</li>
                            <li><strong>Conexões Bem-sucedidas:</strong> ${stats.successfulConnections}</li>
                            <li><strong>Taxa de Sucesso:</strong> ${((stats.successfulConnections / stats.totalConnections) * 100).toFixed(1)}%</li>
                            <li><strong>Duração Média da Sessão:</strong> ${this.formatDuration(stats.averageSessionDuration)}</li>
                        </ul>
                    </div>
                    <div class="col-md-6">
                        <h6>Dados Transferidos</h6>
                        <ul class="list-unstyled">
                            <li><strong>Total Transferido:</strong> ${this.formatBytes(stats.totalDataTransferred)}</li>
                            <li><strong>Média por Sessão:</strong> ${this.formatBytes(stats.averageDataPerSession)}</li>
                        </ul>
                        <h6>Sistema</h6>
                        <ul class="list-unstyled">
                            <li><strong>CPU Média:</strong> ${stats.averageCpuUsage.toFixed(1)}%</li>
                            <li><strong>Memória Média:</strong> ${stats.averageMemoryUsage.toFixed(1)}%</li>
                        </ul>
                    </div>
                </div>
            `;
        } catch (error) {
            console.error('Error loading statistics:', error);
        }
    }

    // Utility Methods
    formatDateTime(dateString) {
        return new Date(dateString).toLocaleString('pt-BR');
    }

    formatDuration(seconds) {
        const hours = Math.floor(seconds / 3600);
        const minutes = Math.floor((seconds % 3600) / 60);
        const secs = seconds % 60;
        
        if (hours > 0) {
            return `${hours}h ${minutes}m ${secs}s`;
        } else if (minutes > 0) {
            return `${minutes}m ${secs}s`;
        } else {
            return `${secs}s`;
        }
    }

    formatBytes(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    showSuccess(message) {
        this.showAlert(message, 'success');
    }

    showError(message) {
        this.showAlert(message, 'danger');
    }

    showAlert(message, type) {
        const alertDiv = document.createElement('div');
        alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        alertDiv.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        alertDiv.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        document.body.appendChild(alertDiv);
        
        setTimeout(() => {
            if (alertDiv.parentNode) {
                alertDiv.parentNode.removeChild(alertDiv);
            }
        }, 5000);
    }

    startAutoRefresh() {
        this.refreshInterval = setInterval(() => {
            if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
                this.connection.invoke("RequestDashboardUpdate");
                this.connection.invoke("RequestSystemInfoUpdate");
                this.connection.invoke("RequestActiveConnectionsUpdate");
            }
        }, 30000); // Refresh every 30 seconds
    }

    stopAutoRefresh() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }
}

// Global functions
function refreshConnections() {
    dashboard.loadActiveConnections();
}

// Initialize dashboard when DOM is loaded
let dashboard;
document.addEventListener('DOMContentLoaded', () => {
    dashboard = new Dashboard();
});

// Cleanup on page unload
window.addEventListener('beforeunload', () => {
    if (dashboard) {
        dashboard.stopAutoRefresh();
        if (dashboard.connection) {
            dashboard.connection.stop();
        }
    }
});