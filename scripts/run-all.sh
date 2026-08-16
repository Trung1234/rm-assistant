#!/usr/bin/env bash
#
# Chạy toàn bộ hệ thống: MCP Server + Agent host.
#
# Hai tiến trình chạy nền, log ghi vào .run/. Dừng bằng scripts/stop-all.sh.
# Script cố ý KHÔNG ghi gì vào repo bankassist-mcp — nếu thiếu khoá thì nó in ra
# hướng dẫn để bạn tự tạo, vì đó là secret của dự án khác.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MCP_DIR="${MCP_DIR:-$ROOT/../bankassist-mcp}"
RUN_DIR="$ROOT/.run"

MCP_URL="${MCP_URL:-http://localhost:5026}"
AGENT_URL="${AGENT_URL:-http://localhost:5080}"
WAIT_SECONDS="${WAIT_SECONDS:-90}"

red()  { printf '\033[31m%s\033[0m\n' "$*"; }
grn()  { printf '\033[32m%s\033[0m\n' "$*"; }
ylw()  { printf '\033[33m%s\033[0m\n' "$*"; }
step() { printf '\n\033[1m▶ %s\033[0m\n' "$*"; }

mkdir -p "$RUN_DIR"

# ── Kiểm tra môi trường ─────────────────────────────────────────────────────
step "Kiểm tra môi trường"

command -v dotnet >/dev/null 2>&1 || { red "Không tìm thấy dotnet. Cài .NET 10 SDK."; exit 1; }

SDK="$(dotnet --version)"
case "$SDK" in
    10.*) grn "  .NET SDK $SDK" ;;
    *)    ylw "  .NET SDK $SDK — dự án nhắm net10.0, bản khác có thể không build được." ;;
esac

if [ -d "$MCP_DIR" ]; then
    MCP_DIR="$(cd "$MCP_DIR" && pwd)"
fi

if [ ! -d "$MCP_DIR" ]; then
    red "Không tìm thấy MCP Server tại: $MCP_DIR"
    echo "  Đặt biến MCP_DIR nếu repo nằm chỗ khác:"
    echo "    MCP_DIR=/duong/dan/bankassist-mcp $0"
    exit 1
fi
grn "  MCP Server: $MCP_DIR"

# Server MCP từ chối khởi động khi thiếu khoá. Báo trước để không phải đi đọc log.
MCP_SECRETS="$MCP_DIR/src/BankAssist.Mcp/appsettings.Development.json"
if [ ! -f "$MCP_SECRETS" ] || ! grep -q "BANKASSIST_AES_KEY" "$MCP_SECRETS" 2>/dev/null; then
    red "Thiếu khoá cho MCP Server."
    echo "  Tạo file: $MCP_SECRETS"
    echo "  Với nội dung:"
    echo
    echo "    {"
    echo "      \"BANKASSIST_AES_KEY\": \"$(openssl rand -base64 32 2>/dev/null || echo '<32 byte Base64>')\","
    echo "      \"BANKASSIST_API_KEY\": \"$(openssl rand -hex 24 2>/dev/null || echo '<>=16 ký tự>')\""
    echo "    }"
    echo
    echo "  (Khoá ở trên sinh ngẫu nhiên sẵn cho bạn dùng ở máy cá nhân.)"
    exit 1
fi
grn "  Khoá MCP: đã có"

if [ ! -f "$ROOT/src/BankAgent.Api/appsettings.Development.json" ]; then
    ylw "  Chưa có src/BankAgent.Api/appsettings.Development.json"
    ylw "  → Agent vẫn chạy, nhưng thiếu Model:Endpoint thì mọi câu hỏi sẽ lỗi ở bước gọi model."
fi

# ── Build ───────────────────────────────────────────────────────────────────
step "Build"
dotnet build "$ROOT/BankCrmAgent.slnx" -v q --nologo | tail -3

# ── Khởi động ───────────────────────────────────────────────────────────────
wait_for_health() {
    local name="$1" url="$2" log="$3" pid="$4" deadline=$((SECONDS + WAIT_SECONDS))

    while [ "$SECONDS" -lt "$deadline" ]; do
        if ! kill -0 "$pid" 2>/dev/null; then
            red "  $name đã thoát. 20 dòng log cuối:"
            tail -20 "$log" | sed 's/^/    /'
            return 1
        fi
        if curl -fsS -m 2 "$url/health" >/dev/null 2>&1; then
            grn "  $name sẵn sàng: $url"
            return 0
        fi
        sleep 1
    done

    red "  $name không phản hồi sau ${WAIT_SECONDS}s. 20 dòng log cuối:"
    tail -20 "$log" | sed 's/^/    /'
    return 1
}

start_service() {
    local name="$1" url="$2" project="$3" workdir="$4" pidfile="$5" log="$6"

    # Đã có ai đó chạy sẵn ở cổng này thì dùng lại, đừng dựng thêm cái thứ hai.
    if curl -fsS -m 2 "$url/health" >/dev/null 2>&1; then
        ylw "  $name đã chạy sẵn tại $url — dùng lại."
        return 0
    fi

    # Chuyển hướng cả ba fd TRƯỚC exec, rồi exec để tiến trình nền thay thế luôn subshell.
    # Nếu không làm vậy, subshell bọc ngoài giữ stdout của script — chạy
    # `run-all.sh | tail` sẽ treo vĩnh viễn vì `tail` không bao giờ thấy EOF — và `$!`
    # sẽ là pid của subshell chứ không phải của `dotnet run`, khiến stop-all.sh giết nhầm.
    (
        cd "$workdir" || exit 1
        exec >"$log" 2>&1 </dev/null
        exec env ASPNETCORE_ENVIRONMENT=Development dotnet run --project "$project" --no-build
    ) &

    local pid=$!
    echo "$pid" >"$pidfile"
    wait_for_health "$name" "$url" "$log" "$pid"
}

step "Khởi động MCP Server"
start_service "MCP Server" "$MCP_URL" "src/BankAssist.Mcp" "$MCP_DIR" "$RUN_DIR/mcp.pid" "$RUN_DIR/mcp.log"

step "Khởi động Agent host"
start_service "Agent host" "$AGENT_URL" "src/BankAgent.Api" "$ROOT" "$RUN_DIR/agent.pid" "$RUN_DIR/agent.log"

# ── Tóm tắt ─────────────────────────────────────────────────────────────────
READY="$(curl -fsS -m 3 "$AGENT_URL/health/ready" 2>/dev/null || echo '{}')"
TOOLS="$(grep -o 'Đã nạp [0-9]*/[0-9]* tool MCP' "$RUN_DIR/agent.log" | tail -1 || true)"

step "Sẵn sàng"
echo "  Giao diện RM : $AGENT_URL"
echo "  REST API     : $AGENT_URL/api/conversations/{id}/messages"
echo "  MCP Server   : $MCP_URL/mcp"
echo "  Tri thức     : $READY"
# if thay vì `[ ... ] && echo`: dưới `set -e`, dạng AND-list rỗng dễ thành lỗi im lặng
# khi có ai sửa thứ tự các dòng sau này.
if [ -n "$TOOLS" ]; then
    echo "  Tool MCP     : $TOOLS"
fi
echo
echo "  Log   : ${RUN_DIR#"$ROOT/"}/agent.log · ${RUN_DIR#"$ROOT/"}/mcp.log"
echo "  Dừng  : scripts/stop-all.sh"
echo
