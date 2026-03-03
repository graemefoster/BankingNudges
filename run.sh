#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
FRESH=false

usage() {
    echo "Usage: ./run.sh [--fresh]"
    echo ""
    echo "  --fresh   Drop and recreate the database with fresh seed data"
    echo ""
    echo "Starts PostgreSQL (Docker), the .NET API, and the React frontend."
    exit 0
}

for arg in "$@"; do
    case $arg in
        --fresh) FRESH=true ;;
        --help|-h) usage ;;
        *) echo "Unknown option: $arg"; usage ;;
    esac
done

cleanup() {
    echo ""
    echo "Shutting down..."
    kill $API_PID $UI_PID $CRM_PID $FUNC_PID 2>/dev/null || true
    wait $API_PID $UI_PID $CRM_PID $FUNC_PID 2>/dev/null || true
}
trap cleanup EXIT

# 1. Start PostgreSQL
echo "🐘 Starting PostgreSQL..."
docker compose -f "$SCRIPT_DIR/docker-compose.yml" up -d --wait

# 2. Fresh database if requested
if [ "$FRESH" = true ]; then
    echo "🗑️  Dropping database for fresh seed..."
    docker exec bankofgraeme-db psql -U bankadmin -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'bankofgraeme' AND pid <> pg_backend_pid();" postgres > /dev/null 2>&1
    docker exec bankofgraeme-db psql -U bankadmin -c "DROP DATABASE IF EXISTS bankofgraeme;" postgres
    docker exec bankofgraeme-db psql -U bankadmin -c "CREATE DATABASE bankofgraeme;" postgres
    echo "✅ Fresh database created — migrations and seed will run on API startup."
fi

# 3. Start API
echo "🚀 Starting API on http://localhost:5225..."
cd "$SCRIPT_DIR/src/BankOfGraeme.Api"
dotnet run --urls "http://localhost:5225" &
API_PID=$!

# 4. Start frontend
echo "🎨 Starting UI on http://localhost:5173..."
cd "$SCRIPT_DIR/src/bank-ui"
npm run dev &
UI_PID=$!

# 5. Start CRM
echo "📋 Starting CRM on http://localhost:5174..."
cd "$SCRIPT_DIR/src/bank-crm"
npm run dev &
CRM_PID=$!

# 6. Build and start Azure Functions
echo "⚡ Building Functions..."
cd "$SCRIPT_DIR/src/BankOfGraeme.Functions"
# Build ignoring metadata-generation error (macOS code-signing issue with .NET 6 tooling).
# The actual DLL and .azurefunctions metadata are still produced.
dotnet build -v q 2>/dev/null || true
echo "⚡ Starting Functions..."
cd bin/Debug/net10.0
func start --no-build --port 7071 &
FUNC_PID=$!

echo ""
echo "════════════════════════════════════════"
echo "  🏦 Bank of Graeme is running!"
echo "  UI:        http://localhost:5173"
echo "  CRM:       http://localhost:5174"
echo "  API:       http://localhost:5225"
echo "  Functions: http://localhost:7071"
echo "  API docs:  http://localhost:5225/openapi/v1.json"
echo "════════════════════════════════════════"
echo "  Press Ctrl+C to stop"
echo ""

wait
