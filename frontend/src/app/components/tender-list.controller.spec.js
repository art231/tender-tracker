// Unit tests for TenderListController
describe('TenderListController', function() {
  var $scope, $controller, $q, $rootScope, TenderApiServiceMock;

  // Mock data matching the transformed structure from TenderApiService
  var mockTenderData = {
    items: [
      {
        id: 36770,
        externalId: "32514769131",
        purchaseNumber: "32514769131",
        title: "Оказание услуг по реализации мероприятий в области информационных технологий",
        customerName: "ИНН: 7706114267",
        publishDate: "2025-04-21T15:33:39Z",
        directLinkToSource: "https://zakupki.gov.ru/epz/order/notice/ea223/view/common-info.html?regNumber=32514769131",
        foundByQueryId: 9,
        foundByQueryKeyword: "информационная система",
        savedAt: "2026-01-29T21:37:54.000881Z",
        applicationDeadline: null,
        maxPrice: 3211649.00,
        region: "77",
        customerInn: null,
        additionalInfo: "Регион: 77"
      },
      {
        id: 36763,
        externalId: "32615646513",
        purchaseNumber: "32615646513",
        title: "Оказание услуг по техническому сопровождению информационной системы",
        customerName: "ИНН: 2130176633",
        publishDate: "2026-01-28T13:55:34Z",
        directLinkToSource: "https://zakupki.gov.ru/epz/order/notice/ea223/view/common-info.html?regNumber=32615646513",
        foundByQueryId: 9,
        foundByQueryKeyword: "информационная система",
        savedAt: "2026-01-29T21:37:54.00088Z",
        applicationDeadline: "2026-02-05T09:00:00Z",
        maxPrice: 15818768.00,
        region: "21",
        customerInn: null,
        additionalInfo: "Регион: 21"
      }
    ],
    totalCount: 10,
    page: 1,
    pageSize: 20,
    totalPages: 1
  };

  // Mock TenderApiService
  var createMockTenderApiService = function() {
    return {
      tenders$: {
        subscribe: jasmine.createSpy('tenders$ subscribe').and.callFake(function(callback) {
          // Simulate initial data
          callback(mockTenderData);
          return { unsubscribe: jasmine.createSpy('unsubscribe') };
        })
      },
      queries$: {
        subscribe: jasmine.createSpy('queries$ subscribe').and.callFake(function(callback) {
          callback([]);
          return { unsubscribe: jasmine.createSpy('unsubscribe') };
        })
      },
      loading$: {
        subscribe: jasmine.createSpy('loading$ subscribe').and.callFake(function(callback) {
          callback(false);
          return { unsubscribe: jasmine.createSpy('unsubscribe') };
        })
      },
      error$: {
        subscribe: jasmine.createSpy('error$ subscribe').and.callFake(function(callback) {
          callback(null);
          return { unsubscribe: jasmine.createSpy('unsubscribe') };
        })
      },
      loadTenders: jasmine.createSpy('loadTenders'),
      loadQueries: jasmine.createSpy('loadQueries').and.callFake(function() {
        // Simulate loading queries
        TenderApiServiceMock.queries$.subscribe.calls.mostRecent().args[0]([]);
      }),
      clearError: jasmine.createSpy('clearError')
    };
  };

  // Load Angular and module before each test
  beforeEach(module('tenderTrackerApp'));

  beforeEach(inject(function(_$controller_, _$rootScope_, _$q_) {
    $controller = _$controller_;
    $rootScope = _$rootScope_;
    $q = _$q_;
    $scope = $rootScope.$new();

    // Create mock service
    TenderApiServiceMock = createMockTenderApiService();

    // Create controller with mocked dependencies
    $controller('TenderListController', {
      $scope: $scope,
      TenderApiService: TenderApiServiceMock,
      $timeout: function(fn, delay) { fn(); } // Immediate timeout for tests
    });

    // Trigger digest cycle to initialize controller
    $rootScope.$apply();
  }));

  describe('Initialization', function() {
    it('should initialize with empty tenders', function() {
      expect($scope.tenders).toBeDefined();
      expect($scope.tenders.items).toEqual([]);
      expect($scope.tenders.totalCount).toBe(0);
    });

    it('should subscribe to tenders stream', function() {
      expect(TenderApiServiceMock.tenders$.subscribe).toHaveBeenCalled();
    });

    it('should call loadTenders on initialization', function() {
      expect(TenderApiServiceMock.loadTenders).toHaveBeenCalled();
    });
  });

  describe('Formatting functions', function() {
    it('formatCustomerName should handle "ИНН:" prefix', function() {
      var result = $scope.formatCustomerName('ИНН: 7706114267');
      expect(result).toBe('Организация (ИНН: 7706114267)');
    });

    it('formatCustomerName should handle empty value', function() {
      var result = $scope.formatCustomerName(null);
      expect(result).toBe('Не указан');
      
      result = $scope.formatCustomerName('');
      expect(result).toBe('Не указан');
    });

    it('formatCustomerName should return regular names unchanged', function() {
      var result = $scope.formatCustomerName('ООО "Рога и копыта"');
      expect(result).toBe('ООО "Рога и копыта"');
    });

    it('getRegionName should convert region codes to names', function() {
      expect($scope.getRegionName('77')).toBe('Москва');
      expect($scope.getRegionName('21')).toBe('Чувашская Республика');
      expect($scope.getRegionName('78')).toBe('Санкт-Петербург');
    });

    it('getRegionName should handle unknown codes', function() {
      expect($scope.getRegionName('99')).toBe('Регион 99');
    });

    it('getRegionName should handle empty values', function() {
      expect($scope.getRegionName(null)).toBe('Не указан');
      expect($scope.getRegionName('')).toBe('Не указан');
    });

    it('getRegionName should return region names unchanged', function() {
      expect($scope.getRegionName('Москва')).toBe('Москва');
      expect($scope.getRegionName('Санкт-Петербург')).toBe('Санкт-Петербург');
    });

    it('formatDate should handle null values', function() {
      expect($scope.formatDate(null)).toBe('Не указана');
      expect($scope.formatDate('')).toBe('Не указана');
    });

    it('formatDate should format valid dates', function() {
      var result = $scope.formatDate('2025-04-21T15:33:39Z');
      expect(result).toContain('2025');
      expect(result).toContain('апреля');
    });

    it('formatDate should handle invalid dates', function() {
      var result = $scope.formatDate('invalid-date');
      expect(result).toBe('Неверный формат даты');
    });

    it('formatMaxPrice should format currency', function() {
      expect($scope.formatMaxPrice(3211649.00)).toContain('3 211 649');
      expect($scope.formatMaxPrice(3211649.00)).toContain('₽');
    });

    it('formatMaxPrice should handle null values', function() {
      expect($scope.formatMaxPrice(null)).toBe('Не указана');
      expect($scope.formatMaxPrice(0)).toBe('Не указана');
    });

    it('formatPurchaseNumber should handle N/A', function() {
      var tender = { purchaseNumber: 'N/A' };
      expect($scope.formatPurchaseNumber(tender)).toBe('Не указан');
    });

    it('formatPurchaseNumber should handle null', function() {
      var tender = { purchaseNumber: null };
      expect($scope.formatPurchaseNumber(tender)).toBe('Не указан');
    });

    it('formatPurchaseNumber should return valid numbers', function() {
      var tender = { purchaseNumber: '32514769131' };
      expect($scope.formatPurchaseNumber(tender)).toBe('32514769131');
    });

    it('formatTitle should handle empty titles', function() {
      expect($scope.formatTitle(null)).toBe('Без названия');
      expect($scope.formatTitle('')).toBe('Без названия');
      expect($scope.formatTitle('Без названия')).toBe('Без названия');
    });

    it('formatTitle should return valid titles', function() {
      var title = 'Оказание услуг по реализации мероприятий';
      expect($scope.formatTitle(title)).toBe(title);
    });
  });

  describe('Pagination functions', function() {
    it('getPaginationRange should return correct range for few pages', function() {
      $scope.tenders.totalPages = 3;
      $scope.tenders.page = 1;
      
      var range = $scope.getPaginationRange();
      expect(range.length).toBe(3);
      expect(range[0].page).toBe(1);
      expect(range[1].page).toBe(2);
      expect(range[2].page).toBe(3);
    });

    it('getPaginationRange should handle ellipsis for many pages', function() {
      $scope.tenders.totalPages = 10;
      $scope.tenders.page = 5;
      
      var range = $scope.getPaginationRange();
      expect(range.length).toBeGreaterThan(5);
      // Should contain ellipsis
      var hasEllipsis = range.some(function(item) { return item.isEllipsis; });
      expect(hasEllipsis).toBe(true);
    });
  });

  describe('Filter functions', function() {
    it('applyFilters should call loadTenders', function() {
      $scope.applyFilters();
      expect(TenderApiServiceMock.loadTenders).toHaveBeenCalled();
    });

    it('clearFilters should reset filters and call loadTenders', function() {
      $scope.filters.search = 'test';
      $scope.filters.region = 'Москва';
      
      $scope.clearFilters();
      
      expect($scope.filters.search).toBe('');
      expect($scope.filters.region).toBe('');
      expect(TenderApiServiceMock.loadTenders).toHaveBeenCalled();
    });
  });

  describe('Sorting functions', function() {
    it('sortBy should set sort column and direction', function() {
      $scope.sortBy('purchaseNumber');
      expect($scope.filters.sortBy).toBe('purchaseNumber');
      expect($scope.filters.sortDescending).toBe(true);
      
      // Toggle direction
      $scope.sortBy('purchaseNumber');
      expect($scope.filters.sortDescending).toBe(false);
      
      // New column resets to descending
      $scope.sortBy('title');
      expect($scope.filters.sortBy).toBe('title');
      expect($scope.filters.sortDescending).toBe(true);
    });

    it('getSortIcon should return correct icons', function() {
      $scope.filters.sortBy = 'purchaseNumber';
      $scope.filters.sortDescending = true;
      expect($scope.getSortIcon('purchaseNumber')).toBe('bi-arrow-down');
      
      $scope.filters.sortDescending = false;
      expect($scope.getSortIcon('purchaseNumber')).toBe('bi-arrow-up');
      
      expect($scope.getSortIcon('title')).toBe('bi-arrow-down-up');
    });
  });

  describe('Deadline functions', function() {
    it('getDeadlineColorClass should return correct classes', function() {
      // Past deadline
      var pastDate = new Date();
      pastDate.setDate(pastDate.getDate() - 1);
      expect($scope.getDeadlineColorClass(pastDate.toISOString())).toBe('text-danger');
      
      // Near deadline (within 3 days)
      var nearDate = new Date();
      nearDate.setDate(nearDate.getDate() + 2);
      expect($scope.getDeadlineColorClass(nearDate.toISOString())).toBe('text-warning');
      
      // Close deadline (within 7 days)
      var closeDate = new Date();
      closeDate.setDate(closeDate.getDate() + 5);
      expect($scope.getDeadlineColorClass(closeDate.toISOString())).toBe('text-info');
      
      // Future deadline
      var futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 10);
      expect($scope.getDeadlineColorClass(futureDate.toISOString())).toBe('text-success');
      
      // Null deadline
      expect($scope.getDeadlineColorClass(null)).toBe('text-muted');
    });

    it('isDeadlineExpired should correctly identify expired deadlines', function() {
      var pastDate = new Date();
      pastDate.setDate(pastDate.getDate() - 1);
      expect($scope.isDeadlineExpired(pastDate.toISOString())).toBe(true);
      
      var futureDate = new Date();
      futureDate.setDate(futureDate.getDate() + 1);
      expect($scope.isDeadlineExpired(futureDate.toISOString())).toBe(false);
      
      expect($scope.isDeadlineExpired(null)).toBe(false);
    });
  });
});
