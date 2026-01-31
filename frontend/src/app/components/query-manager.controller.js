// Query Manager Controller
angular.module('tenderTrackerApp')
  .controller('QueryManagerController', ['$scope', 'TenderApiService', '$timeout',
    function($scope, TenderApiService, $timeout) {
    
    // Reactive data streams
    $scope.queries = [];
    $scope.loading = false;
    $scope.error = null;
    
    // Form model
    $scope.newQuery = {
      keyword: '',
      category: ''
    };
    
    // Initialize
    function init() {
      console.log('QueryManagerController: Initializing...');
      
      // Subscribe to queries stream
      const queriesSubscription = TenderApiService.queries$
        .subscribe(queries => {
          console.log('QueryManagerController: Received queries:', queries);
          console.log('Number of queries:', queries.length);
          $scope.$evalAsync(() => {
            $scope.queries = queries;
            console.log('QueryManagerController: $scope.queries updated:', $scope.queries);
          });
        });
      
      // Subscribe to loading stream
      const loadingSubscription = TenderApiService.loading$
        .subscribe(loading => {
          console.log('QueryManagerController: Loading state:', loading);
          $scope.$evalAsync(() => {
            $scope.loading = loading;
          });
        });
      
      // Subscribe to error stream
      const errorSubscription = TenderApiService.error$
        .subscribe(error => {
          console.log('QueryManagerController: Error:', error);
          $scope.$evalAsync(() => {
            $scope.error = error;
          });
        });
      
      // Clean up subscriptions on controller destroy
      $scope.$on('$destroy', () => {
        queriesSubscription.unsubscribe();
        loadingSubscription.unsubscribe();
        errorSubscription.unsubscribe();
      });
    }
    
    // Add new search query
    $scope.addQuery = function() {
      if (!$scope.newQuery.keyword.trim()) {
        $scope.error = 'Keyword is required';
        return;
      }
      
      TenderApiService.addQuery(
        $scope.newQuery.keyword.trim(),
        $scope.newQuery.category.trim() || null
      );
      
      // Reset form
      $scope.newQuery = {
        keyword: '',
        category: ''
      };
    };
    
    // Toggle query active status
    $scope.toggleQuery = function(query) {
      TenderApiService.updateQuery(query.id, {
        isActive: !query.isActive
      });
    };
    
    // Update query
    $scope.updateQuery = function(query) {
      if (!query.keyword.trim()) {
        $scope.error = 'Keyword is required';
        return;
      }
      
      TenderApiService.updateQuery(query.id, {
        keyword: query.keyword.trim(),
        category: query.category.trim() || null
      });
    };
    
    // Delete query
    $scope.deleteQuery = function(queryId) {
      if (confirm('Are you sure you want to delete this search query?')) {
        TenderApiService.deleteQuery(queryId);
      }
    };
    
    // Clear error
    $scope.clearError = function() {
      TenderApiService.clearError();
    };
    
    // Format date
    $scope.formatDate = function(dateString) {
      return new Date(dateString).toLocaleString('ru-RU');
    };
    
    // Initialize controller
    init();
  }]);
