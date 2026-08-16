#!/usr/bin/env bash
#
# Dừng MCP Server và Agent host do run-all.sh khởi động.

set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUN_DIR="$ROOT/.run"

# `dotnet run` chạy ứng dụng như tiến trình CON. Giết mỗi tiến trình cha sẽ để lại
# ứng dụng còn giữ cổng, và lần chạy sau báo "địa chỉ đã được dùng" mà không rõ vì sao.
# Vì vậy phải đi hết cây tiến trình, con trước cha sau.
kill_tree() {
    local pid="$1" sig="$2" child

    for child in $(pgrep -P "$pid" 2>/dev/null); do
        kill_tree "$child" "$sig"
    done

    kill "-$sig" "$pid" 2>/dev/null || true
}

alive() {
    kill -0 "$1" 2>/dev/null
}

stop_one() {
    local name="$1" pidfile="$2"

    if [ ! -f "$pidfile" ]; then
        echo "  $name: không có pid file, bỏ qua."
        return
    fi

    local pid
    pid="$(cat "$pidfile")"

    if ! alive "$pid"; then
        echo "  $name: không còn chạy."
        rm -f "$pidfile"
        return
    fi

    kill_tree "$pid" TERM

    # Cho 10 giây để đóng kết nối và ghi nốt audit đang chờ.
    local waited=0
    while alive "$pid" && [ "$waited" -lt 10 ]; do
        sleep 1
        waited=$((waited + 1))
    done

    if alive "$pid"; then
        kill_tree "$pid" KILL
        echo "  $name: đã buộc dừng (pid $pid)."
    else
        echo "  $name: đã dừng (pid $pid)."
    fi

    rm -f "$pidfile"
}

echo "Dừng dịch vụ:"
stop_one "Agent host" "$RUN_DIR/agent.pid"
stop_one "MCP Server" "$RUN_DIR/mcp.pid"

# Lưới an toàn: bắt tiến trình mồ côi từ lần chạy trước bị Ctrl-C giữa chừng.
for orphan in $(pgrep -f "BankAgent.Api|BankAssist.Mcp" 2>/dev/null); do
    kill -9 "$orphan" 2>/dev/null && echo "  Dọn tiến trình mồ côi: pid $orphan"
done

exit 0
