"""
HamoodMenu presence backend (Flask, Vercel-ready).

User builds heartbeat their lobby + user id + tier.
Owner builds query by lobby to find user builds, or pull all presence.

Deploy: push this folder to Vercel as a Python project.
  - api/index.py is the entrypoint (this file)
  - vercel.json routes everything to it
  - requirements.txt installs flask

Env:
  OWNER_KEY  shared secret the owner build sends (set in Vercel dashboard)

Endpoints (every one returns the same JSON envelope):
  { "ok": true, "data": { ... } }          on success
  { "ok": false, "error": "reason" }       on failure

  POST /presence/heartbeat   { user_id, nickname, lobby, tier, version }
    -> data: { "user_id": ..., "lobby": ..., "tier": ... }
  GET  /presence/lobby/<code>          (owner key required)
    -> data: { "lobby": ..., "count": N, "users": [ ... ] }
  GET  /presence/all                   (owner key required)
    -> data: { "count": N, "lobbies": { code: [ ... ] } }
  GET  /health
    -> data: { "tracked": N }
"""
import os
import time
import json
from flask import Flask, jsonify, request

app = Flask(__name__)

OWNER_KEY = os.environ.get("OWNER_KEY", "SIGMAMAN")
STALE_AFTER = 120  # seconds without heartbeat before presence drops

# user_id -> { nickname, lobby, tier, version, last_seen }
presence = {}


def ok(data):
    resp = jsonify({"ok": True, "data": data})
    print("[presence] OUT " + json.dumps({"ok": True, "data": data}, default=str))
    return resp


def fail(reason, code=400):
    print("[presence] OUT " + json.dumps({"ok": False, "error": reason}))
    return jsonify({"ok": False, "error": reason}), code


def _clean():
    now = time.time()
    for uid in [u for u, p in presence.items() if now - p["last_seen"] > STALE_AFTER]:
        del presence[uid]


def _owner_ok():
    return request.headers.get("X-Owner-Key") == OWNER_KEY


def _public(p):
    return {
        "nickname": p["nickname"],
        "lobby": p["lobby"],
        "tier": p["tier"],
        "version": p["version"],
        "last_seen": p["last_seen"],
    }


@app.route("/health", methods=["GET"])
def health():
    _clean()
    return ok({"tracked": len(presence)})


@app.route("/presence/heartbeat", methods=["POST", "GET"])
def heartbeat():
    if request.method == "GET":
        # Same heartbeat over GET: /presence/heartbeat?user_id=..&nickname=..&lobby=..&tier=..&version=..
        data = {k: request.args.get(k, "") for k in ("user_id", "nickname", "lobby", "tier", "version")}
    else:
        data = request.get_json(force=True, silent=True) or {}
    user_id = str(data.get("user_id", "")).strip()
    if not user_id:
        return fail("missing user_id")
    entry = {
        "nickname": str(data.get("nickname", "?"))[:32],
        "lobby": str(data.get("lobby", "offline"))[:16],
        "tier": str(data.get("tier", "user"))[:8],
        "version": str(data.get("version", "?"))[:16],
        "last_seen": time.time(),
    }
    print("[presence] IN heartbeat " + json.dumps({"user_id": user_id, **{k: v for k, v in entry.items() if k != "last_seen"}}))
    presence[user_id] = entry
    _clean()
    return ok({"user_id": user_id, "lobby": entry["lobby"], "tier": entry["tier"]})


@app.route("/presence/lobby/<code>", methods=["GET"])
def lobby(code):
    print("[presence] IN lobby_lookup " + json.dumps({"code": code, "key_ok": _owner_ok()}))
    if not _owner_ok():
        return fail("unauthorized", 403)
    _clean()
    code = code.upper()
    found = [
        {"user_id": uid, **_public(p)}
        for uid, p in presence.items()
        if p["lobby"].upper() == code
    ]
    return ok({"lobby": code, "count": len(found), "users": found})


@app.route("/presence/all", methods=["GET"])
def all_presence():
    print("[presence] IN all_lookup " + json.dumps({"key_ok": _owner_ok()}))
    if not _owner_ok():
        return fail("unauthorized", 403)
    _clean()
    by_lobby = {}
    for uid, p in presence.items():
        by_lobby.setdefault(p["lobby"], []).append({"user_id": uid, **_public(p)})
    return ok({"count": len(presence), "lobbies": by_lobby})


# Vercel python runtime exposes `app`
if __name__ == "__main__":
    app.run()
