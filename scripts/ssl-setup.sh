#!/bin/bash

# SSL Setup Script for EVSRS
# Run this script after VPS setup and domain configuration

set -e

# Configuration
DOMAIN_API="api.your-domain.com"
DOMAIN_ADMIN="admin.your-domain.com" 
DOMAIN_LOGS="logs.your-domain.com"
EMAIL="admin@your-domain.com"

echo "🔐 Setting up SSL certificates for EVSRS..."

# Ensure nginx is stopped
systemctl stop nginx

# Generate certificates for all domains
echo "📜 Generating SSL certificates..."
certbot certonly --standalone \
    --email $EMAIL \
    --agree-tos \
    --no-eff-email \
    -d $DOMAIN_API \
    -d $DOMAIN_ADMIN \
    -d $DOMAIN_LOGS

# Copy nginx configuration
echo "⚙️ Configuring Nginx..."
cp /opt/evsrs/configs/nginx/evsrs.conf /etc/nginx/sites-available/evsrs

# Update domains in configuration
sed -i "s/api.your-domain.com/$DOMAIN_API/g" /etc/nginx/sites-available/evsrs
sed -i "s/admin.your-domain.com/$DOMAIN_ADMIN/g" /etc/nginx/sites-available/evsrs
sed -i "s/logs.your-domain.com/$DOMAIN_LOGS/g" /etc/nginx/sites-available/evsrs
sed -i "s/your-domain.com/$DOMAIN_API/g" /etc/nginx/sites-available/evsrs

# Enable site
ln -sf /etc/nginx/sites-available/evsrs /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default

# Create password file for Kibana
echo "🔑 Setting up Kibana authentication..."
read -p "Enter username for Kibana access: " KIBANA_USER
htpasswd -c /etc/nginx/.htpasswd $KIBANA_USER

# Test nginx configuration
nginx -t

# Start nginx
systemctl start nginx
systemctl enable nginx

# Setup auto-renewal
echo "🔄 Setting up SSL auto-renewal..."
(crontab -l 2>/dev/null; echo "0 3 * * * certbot renew --post-hook 'systemctl reload nginx'") | crontab -

# Create DH parameters for better security
echo "🔒 Generating DH parameters (this may take a while)..."
openssl dhparam -out /etc/ssl/certs/dhparam.pem 2048

# Add DH parameters to nginx config
cat >> /etc/nginx/sites-available/evsrs << 'EOF'

# Add this to each server block's SSL configuration
# ssl_dhparam /etc/ssl/certs/dhparam.pem;
EOF

# Restart nginx with final configuration
systemctl restart nginx

echo "✅ SSL setup completed!"
echo ""
echo "🌐 Your domains are now configured:"
echo "- API: https://$DOMAIN_API"
echo "- Admin Panel: https://$DOMAIN_ADMIN"  
echo "- Logs: https://$DOMAIN_LOGS"
echo ""
echo "🔧 Next steps:"
echo "1. Test SSL: curl -I https://$DOMAIN_API/health"
echo "2. Deploy your application"
echo "3. Configure monitoring alerts"