#!/bin/bash

# VPS Setup Script for EVSRS Production Deployment
# Run this script on your fresh VPS as root user

set -e

echo "🚀 Starting VPS Setup for EVSRS Production..."

# Update system
echo "📦 Updating system packages..."
apt update && apt upgrade -y

# Install essential packages
echo "🔧 Installing essential packages..."
apt install -y \
    curl \
    wget \
    git \
    ufw \
    fail2ban \
    htop \
    nano \
    unzip \
    software-properties-common \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release

# Install Docker
echo "🐳 Installing Docker..."
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh
systemctl start docker
systemctl enable docker

# Install Docker Compose
echo "🐙 Installing Docker Compose..."
curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
chmod +x /usr/local/bin/docker-compose
ln -sf /usr/local/bin/docker-compose /usr/bin/docker-compose

# Create docker group and add user
usermod -aG docker $USER

# Setup firewall
echo "🔒 Configuring firewall..."
ufw default deny incoming
ufw default allow outgoing
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 9000/tcp  # Portainer
ufw --force enable

# Install Portainer
echo "🎯 Installing Portainer..."
docker volume create portainer_data
docker run -d \
    -p 8000:8000 \
    -p 9443:9443 \
    --name portainer \
    --restart=always \
    -v /var/run/docker.sock:/var/run/docker.sock \
    -v portainer_data:/data \
    portainer/portainer-ce:latest

# Create application directory
echo "📁 Creating application directories..."
mkdir -p /opt/evsrs
mkdir -p /opt/evsrs/logs
mkdir -p /opt/evsrs/backups
mkdir -p /opt/evsrs/uploads
mkdir -p /opt/evsrs/data/nginx
mkdir -p /opt/evsrs/data/elasticsearch
mkdir -p /opt/evsrs/scripts

# Set permissions
chown -R www-data:www-data /opt/evsrs/uploads
chmod 755 /opt/evsrs/uploads

# Configure fail2ban
echo "🛡️ Configuring fail2ban..."
cat > /etc/fail2ban/jail.local << EOF
[DEFAULT]
bantime = 3600
findtime = 600
maxretry = 5

[sshd]
enabled = true
port = ssh
logpath = /var/log/auth.log
maxretry = 3
EOF

systemctl restart fail2ban

# Install Nginx (for reverse proxy)
echo "🌐 Installing Nginx..."
apt install -y nginx
systemctl start nginx
systemctl enable nginx

# Basic Nginx configuration
cat > /etc/nginx/sites-available/default << 'EOF'
server {
    listen 80 default_server;
    listen [::]:80 default_server;
    
    server_name _;
    
    location / {
        return 301 https://$server_name$request_uri;
    }
    
    location /.well-known/acme-challenge/ {
        root /var/www/html;
    }
}
EOF

# Install Certbot for SSL
echo "🔐 Installing Certbot..."
apt install -y snapd
snap install core; snap refresh core
snap install --classic certbot
ln -sf /snap/bin/certbot /usr/bin/certbot

# Create monitoring script
cat > /opt/evsrs/scripts/monitor.sh << 'EOF'
#!/bin/bash
# Basic monitoring script

LOG_FILE="/opt/evsrs/logs/monitor.log"
DATE=$(date '+%Y-%m-%d %H:%M:%S')

echo "[$DATE] Starting health check..." >> $LOG_FILE

# Check if containers are running
if ! docker ps | grep -q evsrs-api-prod; then
    echo "[$DATE] ERROR: EVSRS API container is not running!" >> $LOG_FILE
    # Restart the application
    cd /opt/evsrs && docker-compose -f docker-compose.production.yml up -d evsrs-api
fi

# Check disk space
DISK_USAGE=$(df /opt/evsrs | awk 'NR==2 {print $5}' | sed 's/%//')
if [ $DISK_USAGE -gt 80 ]; then
    echo "[$DATE] WARNING: Disk usage is ${DISK_USAGE}%" >> $LOG_FILE
fi

# Check memory usage
MEM_USAGE=$(free | awk '/^Mem/ {printf "%.0f", $3/$2 * 100}')
if [ $MEM_USAGE -gt 85 ]; then
    echo "[$DATE] WARNING: Memory usage is ${MEM_USAGE}%" >> $LOG_FILE
fi

echo "[$DATE] Health check completed." >> $LOG_FILE
EOF

chmod +x /opt/evsrs/scripts/monitor.sh

# Create backup script
cat > /opt/evsrs/scripts/backup.sh << 'EOF'
#!/bin/bash
# Database backup script

BACKUP_DIR="/opt/evsrs/backups"
DATE=$(date +%Y%m%d_%H%M%S)
DB_NAME="evsrs_production"

# Create backup
docker exec evsrs-postgres-prod pg_dump -U evsrs_user $DB_NAME > $BACKUP_DIR/backup_$DATE.sql

# Compress backup
gzip $BACKUP_DIR/backup_$DATE.sql

# Keep only last 30 days of backups
find $BACKUP_DIR -name "backup_*.sql.gz" -mtime +30 -delete

echo "Backup completed: backup_$DATE.sql.gz"
EOF

chmod +x /opt/evsrs/scripts/backup.sh

# Setup cron jobs
echo "⏰ Setting up cron jobs..."
(crontab -l 2>/dev/null; echo "0 2 * * * /opt/evsrs/scripts/backup.sh") | crontab -
(crontab -l 2>/dev/null; echo "*/5 * * * * /opt/evsrs/scripts/monitor.sh") | crontab -

# Configure log rotation
cat > /etc/logrotate.d/evsrs << 'EOF'
/opt/evsrs/logs/*.log {
    daily
    missingok
    rotate 30
    compress
    notifempty
    create 644 www-data www-data
    postrotate
        docker kill -s USR1 $(docker ps -q --filter name=evsrs-api-prod) 2>/dev/null || true
    endscript
}
EOF

# Set up auto-updates for security
echo "🔄 Setting up automatic security updates..."
apt install -y unattended-upgrades
cat > /etc/apt/apt.conf.d/50unattended-upgrades << 'EOF'
Unattended-Upgrade::Allowed-Origins {
    "${distro_id}:${distro_codename}-security";
};
Unattended-Upgrade::AutoFixInterruptedDpkg "true";
Unattended-Upgrade::MinimalSteps "true";
Unattended-Upgrade::Remove-Unused-Dependencies "true";
Unattended-Upgrade::Automatic-Reboot "false";
EOF

echo "✅ VPS setup completed!"
echo ""
echo "🎯 Next steps:"
echo "1. Access Portainer at: https://YOUR_VPS_IP:9443"
echo "2. Set up admin user in Portainer"
echo "3. Clone your repository to /opt/evsrs"
echo "4. Configure domain and SSL"
echo "5. Deploy your application"
echo ""
echo "📋 Important directories:"
echo "- Application: /opt/evsrs"
echo "- Logs: /opt/evsrs/logs"
echo "- Backups: /opt/evsrs/backups"
echo "- Scripts: /opt/evsrs/scripts"
echo ""
echo "🔐 Security features enabled:"
echo "- UFW Firewall"
echo "- Fail2ban"
echo "- Automatic security updates"
echo ""
echo "Please reboot the system to complete the setup."