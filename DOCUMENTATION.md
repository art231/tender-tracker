# Документация по функциональности скачивания и анализа документов тендеров

## Обзор

Реализована функциональность для:
1. **Скачивания документов** тендеров из API ГосПлан (44-ФЗ и 223-ФЗ)
2. **Анализа технических заданий (ТЗ)** на совместимость с вашим технологическим стеком
3. **Фильтрации тендеров** по технологической совместимости
4. **Экспорта документов** в форматы PDF и DOCX

## Модели данных

### 1. TenderDocument
Модель для хранения документов тендеров:
```csharp
public class TenderDocument
{
    public int Id { get; set; }
    public int TenderId { get; set; }           // Ссылка на тендер
    public string DocType { get; set; }         // Тип документа: "notification", "protocol", etc.
    public DateTime? PublishedAt { get; set; }  // Дата публикации
    public DateTime DownloadedAt { get; set; }  // Дата скачивания
    public string SourceJson { get; set; }      // Оригинальный JSON документа
    public string? FilePath { get; set; }       // Путь к экспортированному файлу
}
```

### 2. TechnologyAnalysis
Модель для результатов анализа технологической совместимости:
```csharp
public class TechnologyAnalysis
{
    public int Id { get; set; }
    public int TenderId { get; set; }           // Ссылка на тендер
    public int MatchScore { get; set; }         // Процент совпадения (0-100%)
    public string MatchedTechnologiesJson { get; set; } // JSON с найденными технологиями
    public bool IsCompatible { get; set; }      // Совместим ли тендер
    public DateTime AnalyzedAt { get; set; }    // Дата анализа
    public bool ManuallyVerified { get; set; }  // Проверен ли вручную
    public string? AnalysisNotes { get; set; }  // Комментарии к анализу
}
```

## Технологический стек для анализа

Настроен стек технологий для анализа:
- **.NET** (C#, ASP.NET, .NET Core, Entity Framework)
- **PostgreSQL** (Postgres, PSQL)
- **React** (React.js, Redux, Next.js)
- **Java** (Spring, Spring Boot, Hibernate)
- **Angular** (Angular.js, TypeScript)
- **Android** (Kotlin, Android SDK, Flutter)
- **DevOps** (Docker, Kubernetes, CI/CD, AWS/Azure/GCP)
- **ML** (Machine Learning, TensorFlow, PyTorch)
- **RAG ML** (Retrieval-Augmented Generation, LLM, LangChain)

## API Эндпоинты

### Документы (DocumentsController)

#### 1. Получить документы тендера
```
GET /api/documents/tender/{tenderId}
```
Возвращает список всех документов для указанного тендера.

#### 2. Скачать документ определенного типа
```
POST /api/documents/tender/{tenderId}/download
```
Тело запроса:
```json
{
  "docType": "notification"
}
```
Скачивает документ указанного типа из API ГосПлан и сохраняет в БД.

#### 3. Скачать все документы тендера
```
POST /api/documents/tender/{tenderId}/download-all
```
Скачивает все доступные документы для тендера.

#### 4. Получить извещение (ТЗ)
```
GET /api/documents/tender/{tenderId}/notification
```
Возвращает документ-извещение (техническое задание) для тендера.

#### 5. Экспорт документа
```
POST /api/documents/{id}/export
```
Тело запроса:
```json
{
  "format": "pdf"  // или "docx"
}
```
Экспортирует документ в указанный формат (реализация экспорта требует дополнительных библиотек).

#### 6. Удалить документ
```
DELETE /api/documents/{id}
```
Удаляет документ и связанные файлы.

### Анализ технологий (TechnologyAnalysisController)

#### 1. Проанализировать тендер
```
POST /api/technologyanalysis/tender/{tenderId}/analyze?documentId={documentId}
```
Анализирует ТЗ тендера на совместимость с технологическим стеком.
Параметр `documentId` опционален - если не указан, анализируется извещение.

#### 2. Получить анализ тендера
```
GET /api/technologyanalysis/tender/{tenderId}
```
Возвращает результаты анализа для указанного тендера.

#### 3. Получить совместимые тендеры
```
GET /api/technologyanalysis/compatible-tenders?minMatchScore=60
```
Возвращает список тендеров, совместимых с технологическим стеком.
Параметр `minMatchScore` задает минимальный процент совпадения (по умолчанию 60%).

#### 4. Обновить анализ вручную
```
PUT /api/technologyanalysis/{id}
```
Тело запроса:
```json
{
  "matchScore": 85,
  "isCompatible": true,
  "notes": "Ручная корректировка"
}
```
Позволяет вручную скорректировать результаты анализа.

#### 5. Отметить как проверенный вручную
```
POST /api/technologyanalysis/{id}/verify
```
Тело запроса:
```json
{
  "verified": true,
  "notes": "Проверено экспертом"
}
```
Отмечает анализ как проверенный вручную.

#### 6. Статистика по технологиям
```
GET /api/technologyanalysis/statistics?fromDate=2024-01-01&toDate=2024-12-31
```
Возвращает статистику по частоте упоминания технологий в проанализированных тендерах.

## Алгоритм анализа

1. **Извлечение текста**: Текст извлекается из JSON-структуры документа
2. **Нормализация**: Текст приводится к нижнему регистру, удаляются стоп-слова
3. **Поиск технологий**: Ищутся упоминания технологий и их алиасов
4. **Расчет оценки**: 
   - Каждое упоминание технологии увеличивает счетчик
   - Технологии имеют разные веса (например, .NET и React имеют вес 2)
   - Процент совпадения рассчитывается относительно максимально возможной оценки
5. **Определение совместимости**: Тендер считается совместимым, если процент совпадения ≥ 60%

## Конфигурация

### Технологический стек
Конфигурация находится в `TechnologyStackConfig.cs` и включает:
- Список технологий с алиасами
- Веса технологий
- Настройки анализа (минимальный процент совпадения, максимальная длина текста)

### Настройки по умолчанию
```json
{
  "MinimumMatchScore": 60,
  "EnableAutoAnalysis": true,
  "RequireManualVerification": false,
  "MaxTextLength": 10000
}
```

## Использование

### 1. Скачивание документов
```bash
# Скачать все документы тендера с ID 1
curl -X POST http://localhost:5000/api/documents/tender/1/download-all

# Скачать только извещение
curl -X GET http://localhost:5000/api/documents/tender/1/notification
```

### 2. Анализ совместимости
```bash
# Проанализировать тендер с ID 1
curl -X POST http://localhost:5000/api/technologyanalysis/tender/1/analyze

# Получить совместимые тендеры
curl -X GET "http://localhost:5000/api/technologyanalysis/compatible-tenders?minMatchScore=70"
```

### 3. Фильтрация
```bash
# Получить все совместимые тендеры
curl -X GET http://localhost:5000/api/technologyanalysis/compatible-tenders

# Получить статистику по технологиям
curl -X GET "http://localhost:5000/api/technologyanalysis/statistics?fromDate=2024-01-01"
```

## Следующие шаги для реализации

### 1. Экспорт документов
Требуется установка дополнительных библиотек:
- **PDF**: iTextSharp или QuestPDF
- **DOCX**: DocumentFormat.OpenXml

### 2. Веб-интерфейс
Расширение фронтенда для:
- Отображения документов тендеров
- Показа результатов анализа
- Фильтрации по технологической совместимости
- Ручной проверки результатов анализа

### 3. Автоматизация
- Автоматическое скачивание документов для новых тендеров
- Плановый анализ ТЗ
- Уведомления о новых совместимых тендерах

### 4. Улучшение анализа
- Использование NLP для более точного определения контекста
- Учет синонимов и аббревиатур
- Анализ требований к опыту и квалификации

## Заключение

Реализованная функциональность позволяет:
1. Автоматически скачивать документы тендеров из ГосПлан
2. Анализировать технические задания на совместимость с вашим стеком технологий
3. Фильтровать тендеры по технологической совместимости
4. Проводить ручную проверку и корректировку результатов

Это обеспечивает точечную фильтрацию тендеров, позволяя фокусироваться только на тех закупках, требования которых соответствуют вашим технологическим возможностям.
