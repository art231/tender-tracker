// Tender API Service with RxJS integration
angular.module('tenderTrackerApp')
  .factory('TenderApiService', ['$http', '$q', function($http, $q) {
    // Use rxjs global (from rxjs.umd.min.js) - RxJS 6+
    const rxjs = window.rxjs;
    const { BehaviorSubject } = rxjs;
    const { from } = rxjs;
    const { map, flatMap, catchError } = rxjs.operators;
    
    // RxJS Subjects for reactive data streams
    const tendersSubject = new BehaviorSubject([]);
    const queriesSubject = new BehaviorSubject([]);
    const loadingSubject = new BehaviorSubject(false);
    const errorSubject = new BehaviorSubject(null);

    const baseUrl = '/api';

    return {
      // Observable streams
      tenders$: tendersSubject.asObservable(),
      queries$: queriesSubject.asObservable(),
      loading$: loadingSubject.asObservable(),
      error$: errorSubject.asObservable(),

      // Load tenders with pagination and filters
      loadTenders: function(page = 1, pageSize = 20, filters = {}) {
        loadingSubject.next(true);
        errorSubject.next(null);

        const params = {
          page: page,
          pageSize: pageSize,
          ...filters
        };

        const subscription = from($http.get(`${baseUrl}/foundtenders`, { params: params }))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              tendersSubject.next(data);
              loadingSubject.next(false);
            },
            error => {
              console.error('Error loading tenders:', error);
              errorSubject.next(error.data || 'Failed to load tenders');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Load search queries
      loadQueries: function() {
        loadingSubject.next(true);

        const subscription = from($http.get(`${baseUrl}/searchqueries`))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              queriesSubject.next(data);
              loadingSubject.next(false);
            },
            error => {
              console.error('Error loading queries:', error);
              errorSubject.next(error.data || 'Failed to load search queries');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Add new search query
      addQuery: function(keyword, category) {
        loadingSubject.next(true);

        const subscription = from($http.post(`${baseUrl}/searchqueries`, {
          keyword: keyword,
          category: category,
          isActive: true
        }))
          .pipe(
            map(response => response.data),
            flatMap(() => this.loadQueries())
          )
          .subscribe(
            () => {
              loadingSubject.next(false);
            },
            error => {
              console.error('Error adding query:', error);
              errorSubject.next(error.data || 'Failed to add search query');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Update search query
      updateQuery: function(id, updates) {
        loadingSubject.next(true);

        const subscription = from($http.put(`${baseUrl}/searchqueries/${id}`, updates))
          .pipe(
            map(response => response.data),
            flatMap(() => this.loadQueries())
          )
          .subscribe(
            () => {
              loadingSubject.next(false);
            },
            error => {
              console.error('Error updating query:', error);
              errorSubject.next(error.data || 'Failed to update search query');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Delete search query
      deleteQuery: function(id) {
        loadingSubject.next(true);

        const subscription = from($http.delete(`${baseUrl}/searchqueries/${id}`))
          .pipe(
            flatMap(() => this.loadQueries())
          )
          .subscribe(
            () => {
              loadingSubject.next(false);
            },
            error => {
              console.error('Error deleting query:', error);
              errorSubject.next(error.data || 'Failed to delete search query');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Get tender statistics
      getStats: function() {
        const { of } = rxjs;
        return from($http.get(`${baseUrl}/foundtenders/stats`))
          .pipe(
            map(response => response.data),
            catchError(error => {
              console.error('Error getting stats:', error);
              return of({ TotalTenders: 0, LastUpdated: new Date() });
            })
          );
      },

      // Clear error
      clearError: function() {
        errorSubject.next(null);
      }
    };
  }])

  // Error interceptor for HTTP requests
  .factory('errorInterceptor', ['$q', function($q) {
    return {
      responseError: function(rejection) {
        console.error('HTTP Error:', rejection.status, rejection.data);
        
        // You can add custom error handling here
        // For example, show notification, redirect to login, etc.
        
        return $q.reject(rejection);
      }
    };
  }]);
