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
      sortBy: 'SavedAt',
      sortDescending: true
    };
    
    // Initialize
    function init() {
      console.log('Initializing TenderListController...');
      console.log('Current $scope.tenders:', $scope.tenders);
      // Subscribe to tenders stream
      const tendersSubscription = TenderApiService.tenders$
        .subscribe(tendersResponse => {
          console.log('Tenders stream update:', tendersResponse);
          console.log('Items count:', tendersResponse.items ? tendersResponse.items.length : 0);
          $scope.$evalAsync(() => {
            $scope.tenders = tendersResponse;
            console.log('$scope.tenders updated:', $scope.tenders);
            console.log('$scope.tenders.items:', $scope.tenders.items);
          });
        });
      
      // Subscribe to queries stream
      const queriesSubscription = TenderApiService.queries$
        .subscribe(queries => {
          $scope.$evalAsync(() => {
            $scope.queries = queries;
          });
        });
      
      // Subscribe to loading stream
      const loadingSubscription = TenderApiService.loading$
        .subscribe(loading => {
          $scope.$evalAsync(() => {
            $scope.loading = loading;
          });
        });
      
      // Subscribe to error stream
      const errorSubscription = TenderApiService.error$
        .subscribe(error => {
          $scope.$evalAsync(() => {
            $scope.error = error;
          });
        });
      
      // Clean up subscriptions on controller destroy
      $scope.$on('$destroy', () => {
        tendersSubscription.unsubscribe();
        queriesSubscription.unsubscribe();
        loadingSubscription.unsubscribe();
        errorSubscription.unsubscribe();
      });
      
      // Load initial data
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
    
    // Apply filters
    $scope.applyFilters = function() {
      $scope.tenders.page = 1; // Reset to first page
      loadTenders();
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
      return new Date(dateString).toLocaleString('ru-RU');
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
    
    // Format customer name
    $scope.formatCustomerName = function(customerName) {
      if (!customerName) {
        return 'Не указан';
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
    
    // Format additional info
    $scope.formatAdditionalInfo = function(tender) {
      if (tender.additionalInfo) {
        return tender.additionalInfo;
      }
      
      const parts = [];
      if (tender.region) {
        parts.push(`Регион: ${tender.region}`);
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
    
    // Clear error
    $scope.clearError = function() {
      TenderApiService.clearError();
    };
    
    // Initialize controller
    init();
  }]);
