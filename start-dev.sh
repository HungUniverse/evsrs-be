#!/bin/bash

# Start Development Environment Script

echo "🚀 Starting EVSRS Development Environment..."

# Create logs directory if it doesn't exist
mkdir -p logs
mkdir -p init-scripts

# Start services
echo "📦 Starting Docker services..."
docker-compose up -d

# Wait for services to be healthy
echo "⏳ Waiting for services to be ready..."
sleep 30

# Check service status
echo "📋 Service Status:"
docker-compose ps

# Show logs
echo "📝 API Logs (press Ctrl+C to stop following logs):"
docker-compose logs -f evsrs-api