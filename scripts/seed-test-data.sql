
INSERT INTO buildings (name, address, created_at)
VALUES ('Testovací budova', 'Univerzitní 1', now());

INSERT INTO rooms (building_id, name, floor)
VALUES ((SELECT id FROM buildings WHERE name = 'Testovací budova'), 'Laboratoř 101', 1);

INSERT INTO sensor_types (code, unit, min_valid, max_valid) VALUES
  ('temperature', '°C',  -40,   85),
  ('humidity',    '%',     0,  100),
  ('co2',         'ppm',   0, 5000);

INSERT INTO devices (hardware_id, room_id, status, firmware_version)
VALUES ('ESP32-001',
        (SELECT id FROM rooms WHERE name = 'Laboratoř 101'),
        1,
        '1.0.0');

INSERT INTO sensors (device_id, channel, sensor_type_id, calibration_offset) VALUES
  ((SELECT id FROM devices WHERE hardware_id = 'ESP32-001'), 'temp1',
   (SELECT id FROM sensor_types WHERE code = 'temperature'), 0),
  ((SELECT id FROM devices WHERE hardware_id = 'ESP32-001'), 'hum1',
   (SELECT id FROM sensor_types WHERE code = 'humidity'), 0),
  ((SELECT id FROM devices WHERE hardware_id = 'ESP32-001'), 'co2',
   (SELECT id FROM sensor_types WHERE code = 'co2'), 0);
