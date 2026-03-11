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

API_PID="" UI_PID="" CRM_PID="" FUNC_PID="" MCP_PID=""

# Kill a process and all its descendants
kill_tree() {
    local pid=$1
    local children
    children=$(pgrep -P "$pid" 2>/dev/null || true)
    for child in $children; do
        kill_tree "$child"
    done
    kill "$pid" 2>/dev/null || true
    sleep 0.1
    kill -0 "$pid" 2>/dev/null && kill -9 "$pid" 2>/dev/null || true
}

cleanup() {
    echo ""
    echo "Shutting down..."
    for pid in $API_PID $UI_PID $CRM_PID $FUNC_PID $MCP_PID; do
        [ -n "$pid" ] && kill_tree "$pid"
    done
    # Sweep for any orphaned workers that escaped the tree walk
    local orphans
    orphans=$(pgrep -f "BankOfGraeme\." 2>/dev/null || true)
    for pid in $orphans; do
        kill -9 "$pid" 2>/dev/null || true
    done
    wait 2>/dev/null || true
    echo "Stopped."
}
trap cleanup EXIT INT TERM

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

# 3. Build all .NET projects upfront to avoid parallel build races on shared Domain DLL
echo "🔨 Building .NET projects..."
dotnet build "$SCRIPT_DIR/src/BankOfGraeme.Api/BankOfGraeme.Api.csproj" -v q 2>/dev/null || true
dotnet build "$SCRIPT_DIR/src/BankOfGraeme.Functions/BankOfGraeme.Functions.csproj" -v q 2>/dev/null || true

# 4. Start API
echo "🚀 Starting API on http://localhost:5225..."
cd "$SCRIPT_DIR/src/BankOfGraeme.Api"
dotnet run --no-build --urls "http://localhost:5225" &
API_PID=$!

# 5. Start frontend
echo "🎨 Starting UI on http://localhost:5173..."
cd "$SCRIPT_DIR/src/bank-ui"
npm run dev &
UI_PID=$!

# 6. Start CRM
echo "📋 Starting CRM on http://localhost:5174..."
cd "$SCRIPT_DIR/src/bank-crm"
npm run dev &
CRM_PID=$!

# 7. Start Azure Functions
echo "⚡ Starting Functions..."
cd "$SCRIPT_DIR/src/BankOfGraeme.Functions/bin/Debug/net10.0"
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
