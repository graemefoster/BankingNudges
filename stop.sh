#!/usr/bin/env bash
set -euo pipefail

# Stop all Bank of Graeme services that run.sh starts.
# Finds processes by their known ports so it works even if run.sh
# was killed without its cleanup trap firing.

PORTS=(5225 5173 5174 7071)
LABELS=("API" "UI" "CRM" "Functions")

stop_tree() {
    local pid=$1
    local children
    children=$(pgrep -P "$pid" 2>/dev/null || true)
    for child in $children; do
        stop_tree "$child"
    done
    kill "$pid" 2>/dev/null || true
    # If still alive after SIGTERM, force-kill
    sleep 0.2
    kill -0 "$pid" 2>/dev/null && kill -9 "$pid" 2>/dev/null || true
}

killed=0
for i in "${!PORTS[@]}"; do
    port=${PORTS[$i]}
    label=${LABELS[$i]}
    pids=$(lsof -ti ":$port" 2>/dev/null || true)
    if [ -n "$pids" ]; then
        echo "🛑 Stopping $label (port $port)..."
        for pid in $pids; do
            stop_tree "$pid"
        done
        ((killed++)) || true
    fi
done

# Sweep for orphaned Azure Functions dotnet workers that outlived their parent
orphans=$(pgrep -f "BankOfGraeme.Functions.dll" 2>/dev/null || true)
if [ -n "$orphans" ]; then
    echo "🛑 Stopping orphaned Functions workers..."
    for pid in $orphans; do
        kill -9 "$pid" 2>/dev/null || true
    done
    ((killed++)) || true
fi

if [ "$killed" -eq 0 ]; then
    echo "✅ Nothing running — all services already stopped."
else
    echo "✅ Stopped $killed service(s)."
fi
