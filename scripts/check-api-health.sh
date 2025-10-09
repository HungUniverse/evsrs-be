#!/bin/bash

# Quick script to check API health and debugging
echo "🔍 Checking API container health and endpoints..."

echo "1. Container status:"
docker ps -f name=evsrs-api

echo -e "\n2. Container logs (last 50 lines):"
docker logs evsrs-api --tail=50

echo -e "\n3. Testing endpoints:"
echo "Health endpoint:"
curl -v http://localhost:8080/health 2>&1 | head -20

echo -e "\nSwagger endpoint:"
curl -v http://localhost:8080/swagger 2>&1 | head -20

echo -e "\nSwagger index:"
curl -v http://localhost:8080/swagger/index.html 2>&1 | head -20

echo -e "\nAPI root:"
curl -v http://localhost:8080/ 2>&1 | head -20

echo -e "\n4. Available API routes:"
curl -s http://localhost:8080/swagger/v1/swagger.json | grep -o '"[^"]*":{"' | grep -v 'definitions\|components' | sort

echo -e "\n5. Container inspect:"
docker inspect evsrs-api | grep -A 5 -B 5 "Health"