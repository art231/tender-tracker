-- Инициализация базы данных TenderTracker
-- Создание таблиц и индексов

-- Таблица поисковых запросов
CREATE TABLE IF NOT EXISTS SearchQueries (
    Id SERIAL PRIMARY KEY,
    Keyword TEXT NOT NULL,
    Category TEXT,
    IsActive BOOLEAN DEFAULT true,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица найденных тендеров
CREATE TABLE IF NOT EXISTS FoundTenders (
    Id SERIAL PRIMARY KEY,
    ExternalId TEXT UNIQUE NOT NULL,
    PurchaseNumber TEXT NOT NULL,
    Title TEXT NOT NULL,
    CustomerName TEXT,
    PublishDate TIMESTAMPTZ,
    DirectLinkToSource TEXT,
    FoundByQueryId INT REFERENCES SearchQueries(Id) ON DELETE SET NULL,
    SavedAt TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Индексы для производительности
CREATE INDEX IF NOT EXISTS idx_foundtenders_foundbyqueryid ON FoundTenders(FoundByQueryId);
CREATE INDEX IF NOT EXISTS idx_foundtenders_publishdate ON FoundTenders(PublishDate DESC);
CREATE INDEX IF NOT EXISTS idx_foundtenders_externalid ON FoundTenders(ExternalId);
CREATE INDEX IF NOT EXISTS idx_searchqueries_isactive ON SearchQueries(IsActive);

-- Вставка тестовых данных (опционально)
INSERT INTO SearchQueries (Keyword, Category, IsActive) VALUES
    ('разработка .NET', 'IT', true),
    ('строительство', 'Строительство', true),
    ('медицинское оборудование', 'Медицина', true)
ON CONFLICT DO NOTHING;

-- Комментарии к таблицам
COMMENT ON TABLE SearchQueries IS 'Таблица поисковых запросов для мониторинга тендеров';
COMMENT ON TABLE FoundTenders IS 'Таблица найденных тендеров с ГосПлан API';
