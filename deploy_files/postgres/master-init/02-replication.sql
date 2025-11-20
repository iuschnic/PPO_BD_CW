-- Создаем пользователя для репликации
CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replicate';

-- Создаем вторую базу данных
CREATE DATABASE messagesenderdb;

-- Создаем слоты репликации
SELECT pg_create_physical_replication_slot('replica_slot_1', true);
SELECT pg_create_physical_replication_slot('replica_slot_2', true);

-- Даем права пользователю replicator
GRANT CONNECT ON DATABASE tasktracker TO replicator;
GRANT CONNECT ON DATABASE messagesenderdb TO replicator;

-- Проверяем создание слотов
SELECT slot_name, active, wal_status FROM pg_replication_slots;