#!/usr/bin/env bash
# Isolated startup/failure smoke only. No live database, credentials or deployment.
set -euo pipefail
: "${QA_API_IMAGE:?Supply the locally built CI API image}"
qa_docker() {
  env -u DOCKER_HOST -u DOCKER_CONTEXT -u DOCKER_TLS -u DOCKER_TLS_VERIFY -u DOCKER_CERT_PATH \
    docker --host=unix:///var/run/docker.sock "$@"
}
qa_run="$(python3 -c 'import uuid; print(uuid.uuid4().hex)')"
qa_container="academydesk-api-qa-$qa_run"
qa_container_id=''
cleanup() {
  if [[ -z "$qa_container_id" ]]; then return; fi
  local qa_owner
  qa_owner="$(qa_docker inspect --format '{{index .Config.Labels "academydesk.qa.run"}}' "$qa_container_id")" || return 1
  if [[ "$qa_owner" != "$qa_run" ]]; then
    echo 'Refused API container cleanup: ownership mismatch.' >&2
    return 1
  fi
  qa_docker rm -f "$qa_container_id" >/dev/null
}
trap cleanup EXIT
qa_container_id="$(qa_docker run --pull=never -d --name "$qa_container" --label "academydesk.qa.run=$qa_run" \
  -p 127.0.0.1::8080 \
  -e ASPNETCORE_ENVIRONMENT=Testing \
  -e Database__ApplyMigrationsOnStartup=false \
  -e Bootstrap__PlatformOwnerEmail= \
  -e Bootstrap__PlatformOwnerPassword= \
  -e MediaStorage__Enabled=false \
  -e 'ConnectionStrings__DefaultConnection=Server=127.0.0.1,1;Database=AcademyDesk_QA_Unavailable;Integrated Security=True;Connect Timeout=1;Encrypt=True;TrustServerCertificate=False' \
  "$QA_API_IMAGE")"
qa_binding="$(qa_docker port "$qa_container_id" 8080/tcp)"
if [[ ! "$qa_binding" =~ ^127\.0\.0\.1:([0-9]+)$ ]]; then
  echo 'API container did not bind only to loopback.' >&2
  exit 1
fi
qa_port="${BASH_REMATCH[1]}"
qa_health=''
for ((qa_attempt=0; qa_attempt<30; qa_attempt++)); do
  qa_health="$(curl --silent --output /dev/null --write-out '%{http_code}' --max-time 3 "http://127.0.0.1:$qa_port/health" || true)"
  if [[ "$qa_health" == '503' ]]; then break; fi
  sleep 1
done
if [[ "$qa_health" != '503' ]]; then
  echo "Expected unavailable-database health 503, observed $qa_health." >&2
  exit 1
fi
qa_auth="$(curl --silent --output /dev/null --write-out '%{http_code}' --max-time 3 "http://127.0.0.1:$qa_port/api/academies")"
if [[ "$qa_auth" != '401' ]]; then
  echo "Expected anonymous academy access 401, observed $qa_auth." >&2
  exit 1
fi
echo 'API container smoke PASS: startup; unavailable isolated DB -> 503; anonymous academy read -> 401.'
