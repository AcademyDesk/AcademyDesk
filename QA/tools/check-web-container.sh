#!/usr/bin/env bash
# Current static artifact smoke against an already-built image. Never pull/build,
# contact a database/model, reuse a container, or bind a public listener.
set -euo pipefail
: "${QA_WEB_IMAGE:?Supply the locally built static web image; no pull is allowed}"
qa_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
qa_docker() {
  env -u DOCKER_HOST -u DOCKER_CONTEXT -u DOCKER_TLS -u DOCKER_TLS_VERIFY -u DOCKER_CERT_PATH \
    docker --host=unix:///var/run/docker.sock "$@"
}
# Fail before allocating resources when the image/export is unavailable.
qa_image="$(qa_docker image inspect --format '{{.Id}}' "$QA_WEB_IMAGE")"
test -f "$qa_root/apps/web/out/penta.html"
qa_run="$(python3 -c 'import uuid; print(uuid.uuid4().hex)')"
qa_name="academydesk-web-qa-$qa_run"
qa_network_id=''
qa_container_id=''
cleanup() {
  local qa_status=$? qa_owner qa_actual_image
  trap - EXIT
  if [[ -n "$qa_container_id" ]]; then
    qa_owner="$(qa_docker inspect --format '{{index .Config.Labels "academydesk.qa.run"}}' "$qa_container_id")" || qa_status=1
    qa_actual_image="$(qa_docker inspect --format '{{.Image}}' "$qa_container_id")" || qa_status=1
    if [[ "${qa_owner:-}" == "$qa_run" && "${qa_actual_image:-}" == "$qa_image" ]]; then
      qa_docker rm -f "$qa_container_id" >/dev/null || qa_status=1
    else
      echo 'Refused static container cleanup: ownership/image mismatch.' >&2
      qa_status=1
    fi
  fi
  if [[ -n "$qa_network_id" ]]; then
    qa_owner="$(qa_docker network inspect --format '{{index .Labels "academydesk.qa.run"}}' "$qa_network_id")" || qa_status=1
    if [[ "${qa_owner:-}" == "$qa_run" ]]; then
      qa_docker network rm "$qa_network_id" >/dev/null || qa_status=1
    else
      echo 'Refused static network cleanup: ownership mismatch.' >&2
      qa_status=1
    fi
  fi
  exit "$qa_status"
}
trap cleanup EXIT
qa_network_id="$(qa_docker network create --internal --label "academydesk.qa.run=$qa_run" "$qa_name")"
qa_container_id="$(qa_docker create --pull=never --name "$qa_name" --label "academydesk.qa.run=$qa_run" \
  --network "$qa_network_id" --user 101:101 --read-only --cap-drop ALL \
  --security-opt no-new-privileges:true --memory 128m --cpus 1 --pids-limit 64 \
  --tmpfs /var/cache/nginx:rw,noexec,nosuid,size=16m,uid=101,gid=101 \
  --tmpfs /var/run:rw,noexec,nosuid,size=1m,uid=101,gid=101 \
  --tmpfs /tmp:rw,noexec,nosuid,size=4m,uid=101,gid=101 \
  -p 127.0.0.1::3000 "$qa_image")"
qa_docker start "$qa_container_id" >/dev/null
qa_binding="$(qa_docker port "$qa_container_id" 3000/tcp)"
if [[ ! "$qa_binding" =~ ^127\.0\.0\.1:([0-9]+)$ ]]; then
  echo 'Static web container did not bind only to loopback.' >&2
  exit 1
fi
qa_port="${BASH_REMATCH[1]}"
qa_ready=''
for ((qa_attempt=0; qa_attempt<20; qa_attempt++)); do
  qa_ready="$(curl --silent --output /dev/null --write-out '%{http_code}' --max-time 2 "http://127.0.0.1:$qa_port/login" || true)"
  if [[ "$qa_ready" == '200' ]]; then break; fi
  sleep 1
done
if [[ "$qa_ready" != '200' ]]; then
  echo "Expected static login 200, observed $qa_ready." >&2
  exit 1
fi
(cd "$qa_root" && node QA/tools/static-web-runtime.cjs "http://127.0.0.1:$qa_port" "$qa_run")
echo 'Static web image smoke PASS: exact exported bytes, route/segment behavior; no API/model acceptance.'
