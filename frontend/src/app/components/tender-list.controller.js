// Tender List Controller
angular.module('tenderTrackerApp')
  .controller('TenderListController', ['$scope', 'TenderApiService',
    function($scope, TenderApiService) {
    
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
      sortBy: 'SavedAt',
      sortDescending: true
    };
    
    // Initialize
    function init() {
      // Subscribe to tenders stream
      const tendersSubscription = TenderApiService.tenders$
        .subscribe(tendersResponse => {
          $scope.$apply(() => {
            $scope.tenders = tendersResponse;
          });
        });
      
      // Subscribe to queries stream
      const queriesSubscription = TenderApiService.queries$
        .subscribe(queries => {
          $scope.$apply(() => {
            $scope.queries = queries;
          });
        });
      
      // Subscribe to loading stream
      const loadingSubscription = TenderApiService.loading$
        .subscribe(loading => {
          $scope.$apply(() => {
            $scope.loading = loading;
          });
        });
      
      // Subscribe to error stream
      const errorSubscription = TenderApiService.error$
        .subscribe(error => {
          $scope.$apply(() => {
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
      if (!dateString) return 'N/A';
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
      if (!queryId) return 'N/A';
      const query = $scope.queries.find(q => q.id === queryId);
      return query ? query.keyword : 'Unknown';
    };
    
    // Open tender link
    $scope.openTenderLink = function(tender) {
      if (tender.directLinkToSource) {
        window.open(tender.directLinkToSource, '_blank');
      } else {
        // Fallback to zakupki.gov.ru search
        const searchUrl = `https://zakupki.gov.ru/epz/order/quicksearch/search.html?searchString=${encodeURIComponent(tender.purchaseNumber)}`;
        window.open(searchUrl, '_blank');
      }
    };
    
    // Clear error
    $scope.clearError = function() {
      TenderApiService.clearError();
    };
    
    // Initialize controller
    init();
  }]);
