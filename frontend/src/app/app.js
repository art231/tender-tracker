// Main AngularJS application module
angular.module('tenderTrackerApp', [])
  .config(['$httpProvider', function($httpProvider) {
    // Enable CORS
    $httpProvider.defaults.withCredentials = true;
    
    // Add response interceptor for error handling
    $httpProvider.interceptors.push('errorInterceptor');
  }])
  .run(['$rootScope', 'TenderApiService', function($rootScope, TenderApiService) {
    // Initialize application
    console.log('TenderTracker application started');
    
    // Load initial data
    TenderApiService.loadTenders();
    TenderApiService.loadQueries();
    
    // Set up global error handler
    $rootScope.$on('$routeChangeError', function(event, current, previous, rejection) {
      console.error('Route change error:', rejection);
    });
  }])
  // Range filter for pagination
  .filter('range', function() {
    return function(input, total) {
      total = parseInt(total);
      for (var i = 1; i <= total; i++) {
        input.push(i);
      }
      return input;
    };
  });
