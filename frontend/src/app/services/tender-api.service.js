// Tender API Service with RxJS integration
angular.module('tenderTrackerApp')
  .factory('TenderApiService', ['$http', '$q', function($http, $q) {
    // Debug: Check if RxJS is loaded
    console.log('Loading TenderApiService...');
    console.log('window.rxjs:', window.rxjs);
    console.log('window.Rx:', window.Rx);
    
    // Use rxjs global (from rxjs.umd.min.js) - RxJS 6+
    // Note: RxJS 6+ uses window.rxjs, not window.Rx
    let rxjs = window.rxjs;
    
    // If rxjs is not available, try to load from global RxJS UMD bundle
    if (!rxjs && typeof window !== 'undefined') {
      // Check if rxjs.umd.min.js loaded but assigned to different global
      if (window.rxjs) {
        rxjs = window.rxjs;
      } else if (window.Rx) {
        // RxJS 5 or earlier uses window.Rx
        rxjs = window.Rx;
        console.warn('RxJS 5 detected (window.Rx). Consider upgrading to RxJS 6+.');
      } else {
        console.error('RxJS not loaded. Make sure rxjs.umd.min.js is included in index.html.');
        console.error('Current script loading order:');
        console.error('1. jQuery, 2. Bootstrap, 3. Angular, 4. RxJS, 5. App scripts');
        
        // Create a more complete dummy rxjs to prevent errors
        rxjs = {
          BehaviorSubject: class BehaviorSubject { 
            constructor(initialValue) { 
              this._value = initialValue;
              this._observers = [];
            }
            next(value) { 
              this._value = value;
              this._observers.forEach(observer => observer.next && observer.next(value));
            }
            asObservable() { 
              return {
                subscribe: (observer) => {
                  this._observers.push(observer);
                  if (observer.next) observer.next(this._value);
                  return {
                    unsubscribe: () => {
                      const index = this._observers.indexOf(observer);
                      if (index > -1) this._observers.splice(index, 1);
                    }
                  };
                }
              };
            }
          },
          from: (promise) => ({
            pipe: (...operators) => ({
              subscribe: (next, error, complete) => {
                Promise.resolve(promise).then(
                  value => next && next(value),
                  err => error && error(err)
                ).finally(() => complete && complete());
                return { unsubscribe: () => {} };
              }
            })
          }),
          of: (...values) => ({
            pipe: (...operators) => ({
              subscribe: (next, error, complete) => {
                values.forEach(value => next && next(value));
                complete && complete();
                return { unsubscribe: () => {} };
              }
            })
          }),
          operators: { 
            map: (fn) => (source) => source,
            flatMap: (fn) => (source) => source,
            catchError: (fn) => (source) => source
          }
        };
        
        // Assign to window for other services if needed
        window.rxjs = rxjs;
        console.warn('Created dummy RxJS implementation. Application may not work correctly.');
      }
    }
    
    const { BehaviorSubject, from, of } = rxjs;
    const operators = rxjs.operators || {};
    const { map, flatMap, catchError } = operators;
    
    console.log('RxJS loaded successfully:', { BehaviorSubject: !!BehaviorSubject, from: !!from, of: !!of });
    
    // RxJS Subjects for reactive data streams
    const tendersSubject = new BehaviorSubject({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
      totalPages: 0
    });
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

      console.log('Loading tenders with params:', params);
      
      const subscription = from($http.get(`${baseUrl}/foundtenders`, { params: params }))
        .pipe(
          map(response => {
            console.log('Tenders response:', response.data);
            console.log('Response data.tenders:', response.data.tenders);
            // Преобразуем структуру: переименовываем tenders -> items для совместимости с контроллером
            const transformed = {
              items: response.data.tenders || [],
              totalCount: response.data.totalCount || 0,
              page: response.data.page || 1,
              pageSize: response.data.pageSize || pageSize,
              totalPages: response.data.totalPages || 0
            };
            console.log('Transformed data:', transformed);
            return transformed;
          })
        )
        .subscribe(
          data => {
            console.log('Tenders loaded successfully:', data);
            console.log('Number of items:', data.items.length);
            tendersSubject.next(data);
            loadingSubject.next(false);
          },
          error => {
            console.error('Error loading tenders:', error);
            console.error('Error status:', error.status);
            console.error('Error data:', error.data);
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
