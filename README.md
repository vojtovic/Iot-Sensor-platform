#IoT System
IoT system for Acquisition, Transmission, and Storage of Sensor Data in Indoor Environments


<div align="center">

| Layer | Technology | Version | Purpose |
| :---: | :---: | :---: | :---: |
| Transport | Eclipse Mosquitto | 2.1.2 | MQTT broker; telemetry on `iot/v1/{hardwareId}/telemetry` |
| Backend | .NET / ASP.NET Core | 10.0 | hosts the ingest pipeline as a `BackgroundService` |
| MQTT client | MQTTnet | 5.2.0 1603 | subscribe to telemetry topiss |
| ORM | EF CORE + Npgsql | 10.0.3 | data model and migrations |
| Database | PostgreSQL | 18.6 | relational data: buildings, rooms, devices, sensors |
| Time series | TimescaleDB | 2.30.1 | hypertable over `measurments` |
| Visualisation | Grafana | 13.2.2 | dashboards; detasource provisioned from file |
| Testing | xUnit | 2.9.3 | unnit tests in `tests/Domain.Tests` |
|Runtime | Docekr Compose | - | broker, database and Grafana|

<i>Tab.1 I/O table</i>
</div>


**Ports:** 1883 (MQTT), 5432 (PostgreSQL), 3000 (Grafana - `admin` /  `admin`)

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
