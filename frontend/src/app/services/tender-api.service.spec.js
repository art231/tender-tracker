// Unit tests for TenderApiService
describe('TenderApiService', function() {
  var TenderApiService, $httpBackend, $rootScope;
  var rxjs = window.rxjs;
  
  // Load the module before each test
  beforeEach(module('tenderTrackerApp'));
  
  // Inject dependencies
  beforeEach(inject(function(_TenderApiService_, _$httpBackend_, _$rootScope_) {
    TenderApiService = _TenderApiService_;
    $httpBackend = _$httpBackend_;
    $rootScope = _$rootScope_;
  }));
  
  // Clean up after each test
  afterEach(function() {
    $httpBackend.verifyNoOutstandingExpectation();
    $httpBackend.verifyNoOutstandingRequest();
  });
  
  describe('loadTenders', function() {
    it('should load tenders and update the stream', function(done) {
      var mockResponse = {
        tenders: [
          { id: 1, purchaseNumber: '123', title: 'Test Tender' }
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        totalPages: 1
      };
      
      // Mock HTTP request
      $httpBackend.expectGET('/api/foundtenders?page=1&pageSize=20')
        .respond(200, mockResponse);
      
      // Subscribe to tenders stream
      var receivedData = null;
      var subscription = TenderApiService.tenders$.subscribe(function(data) {
        receivedData = data;
      });
      
      // Call the method
      TenderApiService.loadTenders(1, 20, {});
      
      // Flush the HTTP request
      $httpBackend.flush();
      
      // Verify the stream received the data
      expect(receivedData).toEqual(mockResponse);
      
      // Clean up subscription
      subscription.unsubscribe();
      done();
    });
    
    it('should handle errors and update error stream', function(done) {
      // Mock HTTP error
      $httpBackend.expectGET('/api/foundtenders?page=1&pageSize=20')
        .respond(500, { message: 'Server error' });
      
      // Subscribe to error stream
      var errorReceived = null;
      var errorSubscription = TenderApiService.error$.subscribe(function(error) {
        errorReceived = error;
      });
      
      // Call the method
      TenderApiService.loadTenders(1, 20, {});
      
      // Flush the HTTP request
      $httpBackend.flush();
      
      // Verify error was received
      expect(errorReceived).toBe('Failed to load tenders');
      
      // Clean up subscription
      errorSubscription.unsubscribe();
      done();
    });
  });
  
  describe('loadQueries', function() {
    it('should load queries and update the stream', function(done) {
      var mockQueries = [
        { id: 1, keyword: 'test', category: 'IT', isActive: true }
      ];
      
      // Mock HTTP request
      $httpBackend.expectGET('/api/searchqueries')
        .respond(200, mockQueries);
      
      // Subscribe to queries stream
      var receivedQueries = null;
      var subscription = TenderApiService.queries$.subscribe(function(queries) {
        receivedQueries = queries;
      });
      
      // Call the method
      TenderApiService.loadQueries();
      
      // Flush the HTTP request
      $httpBackend.flush();
      
      // Verify the stream received the data
      expect(receivedQueries).toEqual(mockQueries);
      
      // Clean up subscription
      subscription.unsubscribe();
      done();
    });
  });
  
  describe('addQuery', function() {
    it('should add a query and reload queries', function(done) {
      var newQuery = { keyword: 'new', category: 'Test', isActive: true };
      var mockResponse = { id: 2, ...newQuery };
      
      // Mock POST request
      $httpBackend.expectPOST('/api/searchqueries', newQuery)
        .respond(201, mockResponse);
      
      // Mock GET request for reload
      $httpBackend.expectGET('/api/searchqueries')
        .respond(200, [mockResponse]);
      
      // Call the method
      TenderApiService.addQuery('new', 'Test');
      
      // Flush both requests
      $httpBackend.flush();
      
      // Verify both requests were made
      expect(true).toBe(true);
      done();
    });
  });
  
  describe('getStats', function() {
    it('should return stats from API', function(done) {
      var mockStats = { TotalTenders: 42, LastUpdated: '2026-01-24T15:00:00Z' };
      
      // Mock HTTP request
      $httpBackend.expectGET('/api/foundtenders/stats')
        .respond(200, mockStats);
      
      // Call the method and subscribe
      TenderApiService.getStats().subscribe(function(stats) {
        expect(stats).toEqual(mockStats);
        done();
      });
      
      // Flush the HTTP request
      $httpBackend.flush();
    });
    
    it('should handle errors and return default stats', function(done) {
      // Mock HTTP error
      $httpBackend.expectGET('/api/foundtenders/stats')
        .respond(500, {});
      
      // Call the method and subscribe
      TenderApiService.getStats().subscribe(function(stats) {
        expect(stats.TotalTenders).toBe(0);
        expect(stats.LastUpdated).toBeDefined();
        done();
      });
      
      // Flush the HTTP request
      $httpBackend.flush();
    });
  });
});
