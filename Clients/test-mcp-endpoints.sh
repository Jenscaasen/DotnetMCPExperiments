#!/bin/bash

echo "Testing MCP endpoints that were failing..."
echo "=========================================="

BASE_URL="http://localhost:5253/mcp"

echo ""
echo "1. Testing ping method:"
curl -X POST "$BASE_URL" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "ping", "id": 1}' \
  | jq '.'

echo ""
echo "2. Testing resources/list method:"
curl -X POST "$BASE_URL" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "resources/list", "params": {}, "id": 2}' \
  | jq '.'

echo ""
echo "3. Testing resources/templates/list method:"
curl -X POST "$BASE_URL" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "resources/templates/list", "params": {}, "id": 3}' \
  | jq '.'

echo ""
echo "4. Testing prompts/list method:"
curl -X POST "$BASE_URL" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "method": "prompts/list", "params": {}, "id": 4}' \
  | jq '.'

echo ""
echo "Testing completed!"