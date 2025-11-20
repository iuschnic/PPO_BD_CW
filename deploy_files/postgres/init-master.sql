-- Создаем пользователя для репликации
CREATE USER replicator WITH REPLICATION ENCRYPTED PASSWORD 'replicate';

-- Создаем вторую базу данных
CREATE DATABASE messagesenderdb;

-- Настраиваем репликацию
ALTER SYSTEM SET wal_level = replica;
ALTER SYSTEM SET max_wal_senders = 10;
ALTER SYSTEM SET max_replication_slots = 10;
ALTER SYSTEM SET hot_standby = on;
ALTER SYSTEM SET listen_addresses = '*';

-- Перезагружаем конфигурацию чтобы применить настройки
SELECT pg_reload_conf();

-- Ждем немного чтобы настройки применились
SELECT pg_sleep(2);

-- Добавляем правила доступа для репликации в pg_hba.conf через SQL
-- Удаляем старые правила если есть
DO $$
BEGIN
    -- Очищаем текущие настройки (кроме local)
    PERFORM pg_read_file('pg_hba.conf') AS current_hba;
    
    -- Создаем новый pg_hba.conf
    EXECUTE 'ALTER SYSTEM RESET pg_hba.conf';
    
    -- Добавляем базовые правила
    PERFORM pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'template1';
    
EXCEPTION
    WHEN others THEN
        NULL;
END $$;

-- Создаем новый pg_hba.conf с правилами для репликации
\! echo "local all all trust" > /var/lib/postgresql/data/pg_hba.conf
\! echo "host replication replicator 0.0.0.0/0 md5" >> /var/lib/postgresql/data/pg_hba.conf
\! echo "host all all 0.0.0.0/0 md5" >> /var/lib/postgresql/data/pg_hba.conf

-- Перезагружаем конфигурацию еще раз
SELECT pg_reload_conf();

-- Создаем слоты репликации (если их нет)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_replication_slots WHERE slot_name = 'replica_slot_1') THEN
        PERFORM pg_create_physical_replication_slot('replica_slot_1', true);
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_replication_slots WHERE slot_name = 'replica_slot_2') THEN
        PERFORM pg_create_physical_replication_slot('replica_slot_2', true);
    END IF;
END $$;

-- Даем права пользователю replicator
GRANT CONNECT ON DATABASE tasktracker TO replicator;
GRANT CONNECT ON DATABASE messagesenderdb TO replicator;

-- Проверяем что настройки применились
SELECT name, setting FROM pg_settings WHERE name IN ('wal_level', 'max_wal_senders', 'max_replication_slots');