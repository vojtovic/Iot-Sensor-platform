# IoT System

IoT system for Acquisition, Transmission, and Storage of Sensor Data in Indoor Environments

<div align="center">

| Layer | Technology | Version | Purpose |
| :---: | :---: | :---: | :---: |
| Transport | Eclipse Mosquitto | 2.1.2 | MQTT broker; telemetry on `iot/v1/{hardwareId}/telemetry` |
| Backend | .NET / ASP.NET Core | 10.0 | hosts the ingest pipeline as a `BackgroundService` |
| MQTT client | MQTTnet | 5.2.0 1603 | subscribe to telemetry topic |
| ORM | EF CORE + Npgsql | 10.0.3 | data model and migrations |
| Database | PostgreSQL | 18.6 | relational data: buildings, rooms, devices, sensors |
| Time series | TimescaleDB | 2.30.1 | hypertable over `measurements` |
| Visualisation | Grafana | 13.2.2 | dashboards; datasource provisioned from file |
| Testing | xUnit | 2.9.3 | unit tests in `tests/Domain.Tests` |
| Runtime | Docker Compose | - | broker, database and Grafana |

<i>Tab.1 Technology</i>
</div>

## How to run it

Requirements: Docker (Docker Desktop for Win/mac-OS), Git, .NET 10 SDK (for Simulation)

  ```sh
  git clone https://github.com/vojtovic/Iot-Sensor-platform.git
  cd Iot-Sensor-platform
  docker compose up -d
  ```

The first start may take a few minutes (images are downloaded, the API and the web are built, TLS certificates are generated).

| Service | URL | Note |
| --- | --- | --- |
| Test client (UI) | <http://localhost:5235> | login `admin@iot.local` / `Admin123!` |
| API documentation | <http://localhost:5047/scalar/v1> | REST API (Scalar), needs a token, see below |
| Grafana | <http://localhost:3000> | view without login, admin: `admin` / `admin` |
| MQTT broker (TLS) | `<server>:8883` | for devices, CA certificate `docker/certs/data/ca.crt` |
| MQTT broker | `localhost:1883` | without TLS, only from the same computer |
| PostgreSQL / TimescaleDB | `localhost:5432` | database `timescaledb_iot` |

Stop:

```sh
docker compose down # stops the containers, data are kept
docker compose down -v # also deletes the database, Grafana and broker data
```

### Passwords and settings (.env)

Without a `.env` file the default passwords from `docker-compose.yml` are used. That is fine for trying it out. To change them, create `.env` **before the first start**:

```sh
cp .env.example .env #Windows: copy .env.example .env
```

| Variable | Meaning |
| --- | --- |
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | login to the UI and the API |
| `POSTGRES_PASSWORD`, `GRAFANA_DB_PASSWORD` | database |
| `MOSQUITTO_DYNSEC_PASSWORD`, `MQTT_BACKEND_PASSWORD`, `MQTT_PROVISIONER_PASSWORD` | MQTT broker |
| `CLAIM_GROUP_KEY` | key from which devices derive their device key (`openssl rand -hex 32`) |
| `TLS_SAN` | names and IP addresses of the server in the broker certificate |

*Note: the database and broker passwords are stored on the first start. To change them later, run `docker compose down -v` (deletes all data).*

### Using the API

All `/api/v1` endpoints need a token (otherwise `401`).

1. In Scalar call `POST /login` with `{"email": "admin@iot.local", "password": "Admin123!"}`.
2. Copy `accessToken` from the response.
3. Send it with every request in the header `Authorization: Bearer <accessToken>`

The token is valid for 1 hour.

### Devices on the network (TLS)

Devices outside the server connect to port **8883** with TLS and must trust the CA certificate `docker/certs/data/ca.crt`, created on the first start.

The address the device uses must be in the certificate. By default it contains only `localhost` and `127.0.0.1`. Add the IP address of the server to `TLS_SAN` in `.env`, e.g.:

```
TLS_SAN=DNS:mosquitto,DNS:localhost,IP:127.0.0.1,IP:10.42.0.54
```

The certificate is created only once. After changing `TLS_SAN` delete the old one (this also creates a new CA, so copy the new `ca.crt` to the devices):

```sh
sudo rm -rf docker/certs/data     # Windows: Remove-Item -Recurse docker/certs/data
docker compose up -d
docker compose restart mosquitto
```

### Simulator

The simulator behaves like real devices: each one derives its key from the group key, claims itself and sends measurements. Set the group key first (the default one from `docker-compose.yml`; if you set your own `CLAIM_GROUP_KEY` in `.env`, use that one):

linux/macOS

```sh
export Claim__GroupKey=$(grep -o 'CLAIM_GROUP_KEY:-[0-9a-fA-F]*' docker-compose.yml | cut -d- -f2) 
```

PowerShell(win)

```powershell
$env:Claim__GroupKey = (Select-String -Path docker-compose.yml -Pattern 'CLAIM_GROUP_KEY:-([0-9a-fA-F]+)').Matches[0].Groups[1].Value 
```

On the same computer as the server (port 1883, without TLS):

```sh
dotnet run --project src/Simulator -- 100 1 
```

From another computer (port 8883, with TLS), copy `ca.crt` from the server and set:

```sh
export MQTT_TLS_CA=path/to/ca.crt                 # PowerShell: $env:MQTT_TLS_CA = "path\to\ca.crt"
dotnet run --project src/Simulator -- 100 1 10.42.0.54
```

*Parameters: number of devices (each has 3 sensors), seconds between measurements,
broker address (optional, default `localhost`). Stop with Ctrl+C to print a summary.*

## Message formats

| Topic | Direction | QoS / retain | message |
| --- | --- | --- | --- |
| iot/v1/claim/request | device -> backend | 1 / no | registration request |
| iot/v1/claim/response/{claimToken ?? hardwareId} | backend -> device | 1 / no | credentials or status |
| iot/v1/{hardwareId}/telemetry | device -> backend | 1 / no | measurements |
| iot/v1/{hardwareId}/status | device -> broker | 1 / yes | online / offline (Last Will) |
| iot/v1/{hardwareId}/config | backend -> device | 1 / yes | configuration |
| iot/v1/{hardwareId}/config/ack | device -> backend | 1 / no | configuration confirmation |

Claim request

```json
{
  "hardwareId": "sim-00001",
  "claimToken": "7f3a9c…",
  "firmwareVersion": "v.sim.0.1",
  "ts": "2026-11-09T09:14:22Z",
  "sensors": [
    { "channel": "temp1", "type": "temperature" },
    { "channel": "hum1",  "type": "humidity" },
    { "channel": "co2",   "type": "co2" }
  ],
  "actuators": []
}
```

Response with login information (status : "credentials")

```json
{
  "status": "credentials",
  "serverTime": "2026-11-09T09:14:25Z",
  "username": "sim-00001",
  "password": "…",
  "retryAfter": null,
  "message": null,
  "channels": {
    "sensors":   { "accepted": ["temp1", "hum1", "co2"], "pending": [] },
    "actuators": { "accepted": [], "pending": [] }
  }
}
```

Response waiting for approval (pending)

```json
{ 
  "status": "pending", 
  "serverTime": "2026-11-09T09:14:25Z", 
  "retryAfter": 60,
  "message": "Waiting for approval" 
}
```

Telemetry

```json
{
  "ts": "2026-11-09T09:15:00Z",
  "seq": 42,
  "readings": [
    { "channel": "temp1", "value": 23.481, "status": null },
    { "channel": "hum1",  "value": 51.2,   "status": null },
    { "channel": "co2",   "value": 10000,   "status": "error" }
  ]
}
```

*Note: A reading with status: "error" is stored with quality Error; a value outside the sensor type's valid range is stored as Suspect.*

Configuration

```json
{
  "version": 1,
  "interval": 10,
  "maxSilence": 300,
  "channels": {
    "sensors": [
      { "enabled": true, "deadband": 0, "channel": "temp1" },
      { "enabled": true, "deadband": 0, "channel": "hum1" }
    ]
  }
}
```

*Note: Interval and maxSilence are in seconds.*

Configuration acknowledgement

```json
{ 
  "version": 1, 
  "accepted": true, 
  "error": null 
}
```

Status

The payload is plain text, online or offline, not JSON.

## Use of generative AI

Generative AI was used during the development of this project: Claude
(Anthropic), model Claude Opus 5 (`claude-opus-5`), accessed through Claude Code.

It was used for the following purposes and to the following extent:

- consultation on the system architecture and the choice of technologies —
  discussion and comparison of options; the resulting design is my own,
- searching for academic sources; every cited source was subsequently located
  and verified in the original,
- review of source code written by me, and help with finding defects in it,
- consultation on LaTeX typesetting and on producing the diagrams,
- drafting of Git commit messages.

The source code of this system and the text of the thesis were written by me.
I have reviewed the tool's output and take full responsibility for the contents
of this work.

Commits involving AI assistance are marked with a trailer:

    AI-assisted: <what specifically> (Claude Opus 5)

A complete list is available via `git log --grep="AI-assisted"`.
