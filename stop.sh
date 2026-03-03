#!/usr/bin/env bash
set -euo pipefail

# Stop all Bank of Graeme services that run.sh starts.
# Finds processes by their known ports so it works even if run.sh
# was killed without its cleanup trap firing.

PORTS=(5225 5173 5174 7071)
LABELS=("API" "UI" "CRM" "Functions")

killed=0
for i in "${!PORTS[@]}"; do
    port=${PORTS[$i]}
    label=${LABELS[$i]}
    pids=$(lsof -ti ":$port" 2>/dev/null || true)
    if [ -n "$pids" ]; then
        echo "🛑 Stopping $label (port $port)..."
        echo "$pids" | xargs kill 2>/dev/null || true
        ((killed++)) || true
    fi
done

if [ "$killed" -eq 0 ]; then
    echo "✅ Nothing running — all services already stopped."
else
    echo "✅ Stopped $killed service(s)."
fi
