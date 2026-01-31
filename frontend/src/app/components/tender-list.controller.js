// Tender List Controller
angular.module('tenderTrackerApp')
  .controller('TenderListController', ['$scope', 'TenderApiService', '$timeout',
    function($scope, TenderApiService, $timeout) {
      console.log('TenderListController constructor called');
    
    // Reactive data streams
    $scope.tenders = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
      totalPages: 0
    };
    
    $scope.loading = false;
    $scope.error = null;
    $scope.queries = [];
    
    // Filters
    $scope.filters = {
      search: '',
      queryId: null,
      fromDate: null,
      toDate: null,
      applicationDeadlineFrom: null,
      applicationDeadlineTo: null,
      showExpired: false,
      minPrice: null,
      maxPrice: null,
      region: '',
      purchaseType: '',
      sortBy: 'SavedAt',
      sortDescending: true
    };
    
    // Available regions for filter (names)
    $scope.regions = [
      'Москва',
      'Санкт-Петербург',
      'Московская область',
      'Ленинградская область',
      'Новосибирская область',
      'Свердловская область',
      'Краснодарский край',
      'Республика Татарстан',
      'Нижегородская область',
      'Челябинская область',
      'Ростовская область',
      'Самарская область',
      'Республика Башкортостан',
      'Красноярский край',
      'Пермский край',
      'Воронежская область',
      'Волгоградская область',
      'Саратовская область',
      'Тюменская область',
      'Иркутская область'
    ];
    
    // Region code to name mapping (common Russian region codes)
    $scope.regionCodes = {
      '77': 'Москва',
      '78': 'Санкт-Петербург',
      '50': 'Московская область',
      '47': 'Ленинградская область',
      '54': 'Новосибирская область',
      '66': 'Свердловская область',
      '23': 'Краснодарский край',
      '16': 'Республика Татарстан',
      '52': 'Нижегородская область',
      '74': 'Челябинская область',
      '61': 'Ростовская область',
      '63': 'Самарская область',
      '02': 'Республика Башкортостан',
      '24': 'Красноярский край',
      '59': 'Пермский край',
      '36': 'Воронежская область',
      '34': 'Волгоградская область',
      '64': 'Саратовская область',
      '72': 'Тюменская область',
      '38': 'Иркутская область',
      '21': 'Чувашская Республика',
      '01': 'Республика Адыгея',
      '04': 'Республика Алтай',
      '22': 'Алтайский край',
      '28': 'Амурская область',
      '29': 'Архангельская область',
      '30': 'Астраханская область',
      '31': 'Белгородская область',
      '32': 'Брянская область',
      '33': 'Владимирская область',
      '35': 'Вологодская область',
      '79': 'Еврейская автономная область',
      '75': 'Забайкальский край',
      '37': 'Ивановская область',
      '07': 'Кабардино-Балкарская Республика',
      '39': 'Калининградская область',
      '08': 'Республика Калмыкия',
      '40': 'Калужская область',
      '41': 'Камчатский край',
      '09': 'Карачаево-Черкесская Республика',
      '42': 'Кемеровская область',
      '43': 'Кировская область',
      '44': 'Костромская область',
      '45': 'Курганская область',
      '46': 'Курская область',
      '10': 'Республика Карелия',
      '11': 'Республика Коми',
      '51': 'Мурманская область',
      '83': 'Ненецкий автономный округ',
      '48': 'Оренбургская область',
      '49': 'Орловская область',
      '53': 'Пензенская область',
      '25': 'Приморский край',
      '55': 'Омская область',
      '56': 'Орловская область',
      '57': 'Рязанская область',
      '58': 'Сахалинская область',
      '59': 'Свердловская область',
      '60': 'Смоленская область',
      '61': 'Тамбовская область',
      '62': 'Тверская область',
      '63': 'Томская область',
      '64': 'Тульская область',
      '65': 'Тюменская область',
      '18': 'Удмуртская Республика',
      '76': 'Ярославская область',
      '67': 'Ульяновская область',
      '68': 'Челябинская область',
      '87': 'Чукотский автономный округ',
      '89': 'Ямало-Ненецкий автономный округ',
      '92': 'Севастополь',
      '91': 'Республика Крым',
      '93': 'Республика Крым'
    };
    
    // Purchase types
    $scope.purchaseTypes = [
      { value: '', label: 'Все типы' },
      { value: '44-FZ', label: '44-ФЗ' },
      { value: '223-FZ', label: '223-ФЗ' },
      { value: 'commercial', label: 'Коммерческие закупки' },
      { value: 'other', label: 'Другие' }
    ];
    
    // Initialize
    function init() {
      console.log('=== TenderListController INIT ===');
      console.log('Current $scope.tenders:', $scope.tenders);
      
      // Subscribe to tenders stream FIRST, before loading data
      const tendersSubscription = TenderApiService.tenders$
        .subscribe(tendersResponse => {
          console.log('=== TENDERS STREAM UPDATE ===');
          console.log('Response received:', tendersResponse);
          console.log('Items count:', tendersResponse.items ? tendersResponse.items.length : 0);
          console.log('Items:', tendersResponse.items);
          
          $scope.$evalAsync(() => {
            // Always update when new data arrives
            console.log('Updating $scope.tenders with new data');
            console.log('Old items count:', $scope.tenders.items ? $scope.tenders.items.length : 0);
            console.log('New items count:', tendersResponse.items ? tendersResponse.items.length : 0);
            
            // Simple assignment with JSON parse/stringify for deep copy
            try {
              $scope.tenders = JSON.parse(JSON.stringify(tendersResponse));
              console.log('$scope.tenders after update:', $scope.tenders);
              console.log('Items count after update:', $scope.tenders.items ? $scope.tenders.items.length : 0);
            } catch (e) {
              console.error('Error copying tenders data:', e);
              // Fallback to direct assignment
              $scope.tenders = tendersResponse;
            }
          });
        });
      
      // Subscribe to queries stream
      const queriesSubscription = TenderApiService.queries$
        .subscribe(queries => {
          console.log('Queries stream update:', queries.length, 'queries');
          $scope.$evalAsync(() => {
            $scope.queries = queries;
          });
        });
      
      // Subscribe to loading stream
      const loadingSubscription = TenderApiService.loading$
        .subscribe(loading => {
          console.log('Loading stream update:', loading);
          $scope.$evalAsync(() => {
            $scope.loading = loading;
          });
        });
      
      // Subscribe to error stream
      const errorSubscription = TenderApiService.error$
        .subscribe(error => {
          console.log('Error stream update:', error);
          $scope.$evalAsync(() => {
            $scope.error = error;
          });
        });
      
      // Clean up subscriptions on controller destroy
      $scope.$on('$destroy', () => {
        console.log('TenderListController destroying, unsubscribing...');
        tendersSubscription.unsubscribe();
        queriesSubscription.unsubscribe();
        loadingSubscription.unsubscribe();
        errorSubscription.unsubscribe();
      });
      
      // Load initial data NOW, after subscriptions are set up
      console.log('Loading initial tenders data...');
      loadTenders();
    }
    
    // Load tenders with current filters
    function loadTenders() {
      TenderApiService.loadTenders(
        $scope.tenders.page,
        $scope.tenders.pageSize,
        $scope.filters
      );
    }
    
    // Apply filters with debouncing to prevent excessive API calls
    $scope.applyFilters = function() {
      $scope.tenders.page = 1; // Reset to first page
      // Cancel any pending timeout
      if ($scope.filterTimeout) {
        $timeout.cancel($scope.filterTimeout);
      }
      // Debounce the filter application by 300ms
      $scope.filterTimeout = $timeout(function() {
        loadTenders();
      }, 300);
    };
    
    // Clear filters
    $scope.clearFilters = function() {
      $scope.filters = {
        search: '',
        queryId: null,
        fromDate: null,
        toDate: null,
        applicationDeadlineFrom: null,
        applicationDeadlineTo: null,
        showExpired: false,
        minPrice: null,
        maxPrice: null,
        region: '',
        purchaseType: '',
        sortBy: 'SavedAt',
        sortDescending: true
      };
      $scope.tenders.page = 1;
      loadTenders();
    };
    
    // Change page
    $scope.changePage = function(page) {
      if (page < 1 || page > $scope.tenders.totalPages) {
        return;
      }
      $scope.tenders.page = page;
      loadTenders();
    };
    
    // Change page size
    $scope.changePageSize = function() {
      $scope.tenders.page = 1;
      loadTenders();
    };
    
    // Sort by column
    $scope.sortBy = function(column) {
      if ($scope.filters.sortBy === column) {
        $scope.filters.sortDescending = !$scope.filters.sortDescending;
      } else {
        $scope.filters.sortBy = column;
        $scope.filters.sortDescending = true;
      }
      loadTenders();
    };
    
    // Get sort icon
    $scope.getSortIcon = function(column) {
      if ($scope.filters.sortBy !== column) {
        return 'bi-arrow-down-up';
      }
      return $scope.filters.sortDescending ? 'bi-arrow-down' : 'bi-arrow-up';
    };
    
    // Format date
    $scope.formatDate = function(dateString) {
      if (!dateString) return 'Не указана';
      try {
        const date = new Date(dateString);
        if (isNaN(date.getTime())) {
          return 'Неверный формат даты';
        }
        return date.toLocaleString('ru-RU', {
          year: 'numeric',
          month: 'long',
          day: 'numeric',
          hour: '2-digit',
          minute: '2-digit'
        });
      } catch (e) {
        console.error('Error formatting date:', e, dateString);
        return 'Ошибка формата даты';
      }
    };
    
    // Format date for input
    $scope.formatDateForInput = function(dateString) {
      if (!dateString) return '';
      const date = new Date(dateString);
      return date.toISOString().split('T')[0];
    };
    
    // Get query name by ID
    $scope.getQueryName = function(queryId) {
      if (!queryId) return 'Не указан';
      const query = $scope.queries.find(q => q.id === queryId);
      return query ? query.keyword : 'Неизвестный запрос';
    };
    
    // Format purchase number - show "Не указан" if purchaseNumber is N/A or empty
    $scope.formatPurchaseNumber = function(tender) {
      if (!tender.purchaseNumber || tender.purchaseNumber === 'N/A') {
        return 'Не указан';
      }
      return tender.purchaseNumber;
    };
    
    // Format title
    $scope.formatTitle = function(title) {
      if (!title || title === 'Без названия') {
        return 'Без названия';
      }
      return title;
    };
    
    // Format customer name - extract from "ИНН: 7706114267" format
    $scope.formatCustomerName = function(customerName) {
      if (!customerName) {
        return 'Не указан';
      }
      
      // If customerName starts with "ИНН:", try to extract organization name
      if (customerName.startsWith('ИНН:')) {
        // In current API data, we only have INN, not organization name
        // Return a more user-friendly format
        return `Организация (${customerName})`;
      }
      
      return customerName;
    };
    
    // Format max price
    $scope.formatMaxPrice = function(maxPrice) {
      if (!maxPrice) {
        return 'Не указана';
      }
      return new Intl.NumberFormat('ru-RU', { style: 'currency', currency: 'RUB' }).format(maxPrice);
    };
    
    // Get region name from code
    $scope.getRegionName = function(regionCode) {
      if (!regionCode) {
        return 'Не указан';
      }
      
      // Check if it's already a name (not a code)
      if ($scope.regions.includes(regionCode)) {
        return regionCode;
      }
      
      // Look up in region codes mapping
      const regionName = $scope.regionCodes[regionCode];
      return regionName || `Регион ${regionCode}`;
    };

    // Format additional info
    $scope.formatAdditionalInfo = function(tender) {
      if (tender.additionalInfo) {
        return tender.additionalInfo;
      }
      
      const parts = [];
      if (tender.region) {
        const regionName = $scope.getRegionName(tender.region);
        parts.push(`Регион: ${regionName}`);
      }
      if (tender.customerInn) {
        parts.push(`ИНН: ${tender.customerInn}`);
      }
      
      return parts.length > 0 ? parts.join(', ') : 'Нет дополнительной информации';
    };
    
    // Get deadline color class
    $scope.getDeadlineColorClass = function(deadline) {
      if (!deadline) {
        return 'text-muted';
      }
      
      const now = new Date();
      const deadlineDate = new Date(deadline);
      const daysUntilDeadline = Math.ceil((deadlineDate - now) / (1000 * 60 * 60 * 24));
      
      if (daysUntilDeadline < 0) {
        return 'text-danger'; // Истекший срок
      } else if (daysUntilDeadline <= 3) {
        return 'text-warning'; // Скоро истекает (3 дня или меньше)
      } else if (daysUntilDeadline <= 7) {
        return 'text-info'; // Близкий срок (7 дней или меньше)
      } else {
        return 'text-success'; // Еще есть время
      }
    };
    
    // Check if deadline is expired
    $scope.isDeadlineExpired = function(deadline) {
      if (!deadline) {
        return false;
      }
      return new Date(deadline) < new Date();
    };
    
    // Open tender link with smart search
    $scope.openTenderLink = function(tender) {
      // Если есть прямая ссылка из API, используем её
      if (tender.directLinkToSource) {
        window.open(tender.directLinkToSource, '_blank');
        return;
      }
      
      // Умный поиск с приоритетами
      let searchString = '';
      
      // Приоритет 1: Номер закупки (самый точный)
      if (tender.purchaseNumber && tender.purchaseNumber !== 'N/A') {
        searchString = tender.purchaseNumber;
      }
      // Приоритет 2: Комбинация названия и заказчика
      else if (tender.title && tender.customerName) {
        // Ограничиваем длину поискового запроса
        const titlePart = tender.title.length > 50 ? tender.title.substring(0, 50) + '...' : tender.title;
        searchString = `${titlePart} ${tender.customerName}`;
      }
      // Приоритет 3: Только название
      else if (tender.title) {
        searchString = tender.title.length > 100 ? tender.title.substring(0, 100) + '...' : tender.title;
      }
      // Приоритет 4: Внешний ID
      else {
        searchString = tender.externalId || '';
      }
      
      // Формируем URL для поиска на zakupki.gov.ru
      const searchUrl = `https://zakupki.gov.ru/epz/order/quicksearch/search.html?searchString=${encodeURIComponent(searchString)}`;
      window.open(searchUrl, '_blank');
    };
    
    // Open smart search with multiple options
    $scope.openSmartSearch = function(tender) {
      // Создаем модальное окно или выпадающее меню с вариантами поиска
      const searchOptions = [];
      
      // Вариант 1: По номеру закупки
      if (tender.purchaseNumber && tender.purchaseNumber !== 'N/A') {
        searchOptions.push({
          name: 'По номеру закупки',
          url: `https://zakupki.gov.ru/epz/order/quicksearch/search.html?searchString=${encodeURIComponent(tender.purchaseNumber)}`
        });
      }
      
      // Вариант 2: По названию
      if (tender.title && tender.title !== 'Без названия') {
        const titleSearch = tender.title.length > 50 ? tender.title.substring(0, 50) + '...' : tender.title;
        searchOptions.push({
          name: 'По названию',
          url: `https://zakupki.gov.ru/epz/order/quicksearch/search.html?searchString=${encodeURIComponent(titleSearch)}`
        });
      }
      
      // Вариант 3: По заказчику
      if (tender.customerName) {
        searchOptions.push({
          name: 'По заказчику',
          url: `https://zakupki.gov.ru/epz/order/quicksearch/search.html?searchString=${encodeURIComponent(tender.customerName)}`
        });
      }
      
      // Вариант 4: Комбинированный поиск
      if (tender.title && tender.customerName) {
        const combinedSearch = `${tender.title.substring(0, 30)} ${tender.customerName}`;
        searchOptions.push({
          name: 'По названию и заказчику',
          url: `https://zakupki.gov.ru/epz/order/quicksearch/search.html?searchString=${encodeURIComponent(combinedSearch)}`
        });
      }
      
      // Если есть варианты, открываем первый (самый релевантный)
      if (searchOptions.length > 0) {
        window.open(searchOptions[0].url, '_blank');
        
        // Для демонстрации можно показать все варианты в консоли
        console.log('Доступные варианты поиска для тендера:', searchOptions);
      } else {
        // Fallback
        $scope.openTenderLink(tender);
      }
    };

    // Open tender details modal
    $scope.openTenderDetails = function(tender) {
      $scope.selectedTender = tender;
      $scope.tenderDetails = null;
      $scope.loadingDetails = true;
      $scope.detailsError = null;
      
      // Show modal
      const modal = new bootstrap.Modal(document.getElementById('tenderDetailsModal'));
      modal.show();
      
      // Load tender details
      TenderApiService.getTenderDetails(tender.id)
        .then(function(details) {
          $scope.$apply(function() {
            $scope.tenderDetails = details;
            $scope.loadingDetails = false;
          });
        })
        .catch(function(error) {
          $scope.$apply(function() {
            $scope.detailsError = error || 'Не удалось загрузить детальную информацию';
            $scope.loadingDetails = false;
          });
        });
    };

    // Close tender details modal
    $scope.closeTenderDetails = function() {
      $scope.selectedTender = null;
      $scope.tenderDetails = null;
      $scope.loadingDetails = false;
      $scope.detailsError = null;
      
      const modal = bootstrap.Modal.getInstance(document.getElementById('tenderDetailsModal'));
      if (modal) {
        modal.hide();
      }
    };

    // Format technologies for display
    $scope.formatTechnologies = function(matchedTechnologies) {
      if (!matchedTechnologies || !matchedTechnologies.length) {
        return 'Технологии не найдены';
      }
      
      try {
        const technologies = JSON.parse(matchedTechnologies);
        return technologies.map(t => t.technology).join(', ');
      } catch (e) {
        return 'Ошибка форматирования технологий';
      }
    };

    // Get document type icon
    $scope.getDocumentIcon = function(docType) {
      if (docType.includes('notification') || docType.includes('notice')) {
        return 'bi-megaphone';
      } else if (docType.includes('protocol')) {
        return 'bi-file-text';
      } else if (docType.includes('clarification')) {
        return 'bi-chat-dots';
      } else if (docType.includes('contract')) {
        return 'bi-file-earmark-check';
      } else {
        return 'bi-file-earmark';
      }
    };

    // Get analysis compatibility badge class
    $scope.getCompatibilityBadgeClass = function(isCompatible) {
      return isCompatible ? 'bg-success' : 'bg-danger';
    };

    // Get analysis compatibility text
    $scope.getCompatibilityText = function(isCompatible) {
      return isCompatible ? 'Совместим' : 'Не совместим';
    };
    
    // Download document
    $scope.downloadDocument = function(tenderId, docType) {
      TenderApiService.downloadDocument(tenderId, docType)
        .then(function() {
          console.log('Document downloaded successfully');
        })
        .catch(function(error) {
          console.error('Error downloading document:', error);
          $scope.detailsError = 'Не удалось скачать документ';
        });
    };
    
    // Download notification document
    $scope.downloadNotification = function(tenderId) {
      TenderApiService.getNotificationDocument(tenderId)
        .then(function() {
          console.log('Notification document downloaded successfully');
        })
        .catch(function(error) {
          console.error('Error downloading notification:', error);
          $scope.detailsError = 'Не удалось скачать извещение';
        });
    };
    
    // Download all documents
    $scope.downloadAllDocuments = function(tenderId) {
      TenderApiService.downloadAllDocuments(tenderId)
        .then(function() {
          console.log('All documents downloaded successfully');
        })
        .catch(function(error) {
          console.error('Error downloading all documents:', error);
          $scope.detailsError = 'Не удалось скачать все документы';
        });
    };
    
    // Clear error
    $scope.clearError = function() {
      TenderApiService.clearError();
    };
    
    // Get pagination range for display (max 5 pages with ellipsis)
    // Cached to prevent infinite digest cycles
    $scope.getPaginationRange = (function() {
      let cachedRange = null;
      let cachedPage = null;
      let cachedTotalPages = null;
      
      return function() {
        const currentPage = $scope.tenders.page;
        const totalPages = $scope.tenders.totalPages;
        
        // Return cached result if page and totalPages haven't changed
        if (cachedRange && cachedPage === currentPage && cachedTotalPages === totalPages) {
          return cachedRange;
        }
        
        const delta = 2; // Number of pages to show around current page
        const range = [];
        
        // If total pages is 7 or less, show all pages
        if (totalPages <= 7) {
          for (let i = 1; i <= totalPages; i++) {
            range.push({ page: i, isEllipsis: false });
          }
        } else {
          // Always show first page
          range.push({ page: 1, isEllipsis: false });
          
          // Calculate start and end of middle range
          let start = Math.max(2, currentPage - delta);
          let end = Math.min(totalPages - 1, currentPage + delta);
          
          // Adjust if we're near the beginning
          if (currentPage - delta <= 2) {
            end = Math.min(totalPages - 1, 2 * delta + 1);
          }
          
          // Adjust if we're near the end
          if (currentPage + delta >= totalPages - 1) {
            start = Math.max(2, totalPages - 2 * delta);
          }
          
          // Add ellipsis after first page if needed
          if (start > 2) {
            range.push({ page: null, isEllipsis: true });
          }
          
          // Add middle pages
          for (let i = start; i <= end; i++) {
            range.push({ page: i, isEllipsis: false });
          }
          
          // Add ellipsis before last page if needed
          if (end < totalPages - 1) {
            range.push({ page: null, isEllipsis: true });
          }
          
          // Always show last page
          if (totalPages > 1) {
            range.push({ page: totalPages, isEllipsis: false });
          }
        }
        
        // Cache the result
        cachedRange = range;
        cachedPage = currentPage;
        cachedTotalPages = totalPages;
        
        return range;
      };
    })();
    
    // Initialize controller
    init();
    
    // Debug: Log initial state
    console.log('TenderListController initialized');
    console.log('Initial tenders:', $scope.tenders);
    console.log('Initial loading:', $scope.loading);
  }]);
