-- Настройка репликации
ALTER SYSTEM SET wal_level = replica;
ALTER SYSTEM SET max_wal_senders = 10;
ALTER SYSTEM SET max_replication_slots = 10;
ALTER SYSTEM SET hot_standby = on;

-- Перезагружаем конфигурацию
SELECT pg_reload_conf();

-- Ждем применения настроек
SELECT pg_sleep(2);