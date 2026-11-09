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

 reguirements: Docker (Docker Desktop for Win/mac-OS), Git, .NET 10 SDK (for Simulation)

  ```sh
  git clone https://github.com/vojtovic/Iot-Sensor-platform.git
  cd Iot-Sensor-platform
  docker compose up -d
  ```

First start may take few minutes.

| Service | URL | Note |
| --- | --- | --- |
| Test client (UI) | <http://localhost:5235> | devices, claims, sensor types |
| API documentation | <http://localhost:5047/scalar/v1> | REST API (Scalar) |
| Grafana | <http://localhost:3000> | view without login, admin: `admin` / `admin` |
| MQTT broker | `localhost:1883` | for devices |
| PostgreSQL / TimescaleDB | `localhost:5432` | database `timescaledb_iot` |

You can simulate traffic by running simulator:

linux/macOS

```sh
export Claim__GroupKey=$(grep -o 'CLAIM_GROUP_KEY:-[0-9a-fA-F]*' docker-compose.yml | cut -d- -f2) 
```

PowerShell(win)

```powershell
$env:Claim__GroupKey = (Select-String -Path docker-compose.yml -Pattern 'CLAIM_GROUP_KEY:-([0-9a-fA-F]+)').Matches[0].Groups[1].Value 
```

```sh
dotnet run --project src/Simulator -- 100 1 
```

*Note: -- 1st parameter(Number of devices every device has 3 sensors , 2nd parameter number of seconds between measurements ,3rd parameter(optional) 10.42.0.54(ip address of broker))*

*Note: Stop with Ctrl + C for summary.*

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
