#!/bin/bash

# VPS Setup Script for EVSRS - Simplified Version
# This script follows official Docker documentation
# Run this script on your fresh Ubuntu VPS as root user

set -e

echo "🚀 Starting VPS Setup for EVSRS..."

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
    htop \
    nano \
    unzip \
    ca-certificates \
    gnupg \
    lsb-release

# Setup firewall
echo "🔒 Configuring firewall..."
ufw default deny incoming
ufw default allow outgoing
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
ufw allow 9000/tcp   # Portainer
ufw allow 81/tcp     # Nginx Proxy Manager
ufw --force enable

# Install Docker (following official documentation)
echo "🐳 Installing Docker (Official method)..."

# Add Docker's official GPG key
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

# Add the repository to Apt sources
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  tee /etc/apt/sources.list.d/docker.list > /dev/null

# Update package index
apt update

# Install Docker Engine, containerd, and Docker Compose
apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Start and enable Docker
systemctl start docker
systemctl enable docker

# Create docker group and add current user
groupadd -f docker
usermod -aG docker $USER

# Test Docker installation
echo "🧪 Testing Docker installation..."
docker run hello-world

# Install Portainer CE (following official documentation)
echo "🎯 Installing Portainer CE..."

# Create volume for Portainer data
docker volume create portainer_data

# Deploy Portainer
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
mkdir -p /opt/evsrs/data
mkdir -p /opt/evsrs/backups

# Set proper permissions
chown -R $USER:$USER /opt/evsrs

echo "✅ VPS setup completed!"
echo ""
echo "🎯 Next steps:"
echo "1. Access Portainer at: https://YOUR_VPS_IP:9443"
echo "2. Set up admin user in Portainer (first visit only)"
echo "3. Clone your repository to /opt/evsrs"
echo "4. Configure environment variables"
echo "5. Deploy using ./scripts/deploy-vps.sh (includes Portainer)"
echo ""
echo "📋 Important access points:"
echo "- Portainer: https://YOUR_VPS_IP:9443"
echo "- HTTP: http://YOUR_VPS_IP:80"
echo "- HTTPS: https://YOUR_VPS_IP:443"
echo ""
echo "🔐 Security features enabled:"
echo "- UFW Firewall with minimal ports"
echo "- Docker security defaults"
echo "- Portainer with HTTPS"
echo ""
echo "⚠️  IMPORTANT: Change default passwords and secure your services!"
echo ""
echo "📝 Note: Portainer is now included in docker-compose.vps.yml"
echo ""
echo "Please reboot the system to complete the setup:"
echo "sudo reboot"