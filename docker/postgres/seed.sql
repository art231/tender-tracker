-- Seed data for TenderTracker database
-- This script adds sample search queries for testing

INSERT INTO SearchQueries (Keyword, Category, IsActive, CreatedAt)
VALUES 
    ('разработка .NET', 'IT', true, NOW()),
    ('строительство', 'Строительство', true, NOW()),
    ('медицинское оборудование', 'Медицина', true, NOW()),
    ('образовательные услуги', 'Образование', true, NOW()),
    ('транспортные услуги', 'Логистика', true, NOW()),
    ('офисная мебель', 'Мебель', true, NOW()),
    ('программное обеспечение', 'IT', true, NOW()),
    ('клининговые услуги', 'Услуги', true, NOW()),
    ('электрооборудование', 'Энергетика', true, NOW()),
    ('сельскохозяйственная техника', 'Сельское хозяйство', true, NOW())
ON CONFLICT DO NOTHING;

-- Add some sample tenders for testing
INSERT INTO FoundTenders (ExternalId, PurchaseNumber, Title, CustomerName, PublishDate, DirectLinkToSource, FoundByQueryId, SavedAt)
VALUES 
    ('ext-001', '0123456789', 'Разработка веб-приложения на .NET Core', 'Министерство цифрового развития', '2024-01-15 10:00:00', 'https://zakupki.gov.ru/epz/order/notice/ea44/view/common-info.html?regNumber=0123456789', 1, NOW()),
    ('ext-002', '9876543210', 'Строительство административного здания', 'ГУП "Стройкомплекс"', '2024-01-16 11:30:00', 'https://zakupki.gov.ru/epz/order/notice/ea44/view/common-info.html?regNumber=9876543210', 2, NOW()),
    ('ext-003', '5555555555', 'Поставка медицинского оборудования', 'Городская больница №1', '2024-01-17 09:15:00', 'https://zakupki.gov.ru/epz/order/notice/ea44/view/common-info.html?regNumber=5555555555', 3, NOW()),
    ('ext-004', '4444444444', 'Образовательные курсы для сотрудников', 'Департамент образования', '2024-01-18 14:20:00', 'https://zakupki.gov.ru/epz/order/notice/ea44/view/common-info.html?regNumber=4444444444', 4, NOW()),
    ('ext-005', '3333333333', 'Транспортные услуги по перевозке грузов', 'Логистическая компания "Транзит"', '2024-01-19 16:45:00', 'https://zakupki.gov.ru/epz/order/notice/ea44/view/common-info.html?regNumber=3333333333', 5, NOW())
ON CONFLICT (ExternalId) DO NOTHING;

-- Update sequence values to avoid conflicts
SELECT setval('SearchQueries_Id_seq', COALESCE((SELECT MAX(Id) FROM SearchQueries), 1));
SELECT setval('FoundTenders_Id_seq', COALESCE((SELECT MAX(Id) FROM FoundTenders), 1));

COMMIT;
