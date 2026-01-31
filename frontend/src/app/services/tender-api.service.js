// Tender API Service with RxJS integration
angular.module('tenderTrackerApp')
  .factory('TenderApiService', ['$http', '$q', '$rootScope', function($http, $q, $rootScope) {
    console.log('Loading TenderApiService...');
    
    // Use global rxjs from rxjs.umd.min.js
    const rxjs = window.rxjs;
    if (!rxjs) {
      console.error('RxJS not loaded! Check that rxjs.umd.min.js is included in index.html');
      console.warn('RxJS not loaded, continuing with basic service (without RxJS operators)');
      // We'll continue but operators will be undefined
    }
    
    // Safely extract RxJS operators
    const { BehaviorSubject, from, of } = rxjs;
    let map, mergeMap, catchError, flatMap;
    
    try {
      const operators = rxjs.operators || {};
      map = operators.map || ((fn) => (source) => source.pipe((src) => from(Promise.resolve(src).then(fn))));
      mergeMap = operators.mergeMap || ((fn) => (source) => source.pipe((src) => from(Promise.resolve(src).then(fn))));
      catchError = operators.catchError || ((fn) => (source) => source.pipe((src) => from(Promise.resolve(src).catch(fn))));
      flatMap = mergeMap;
    } catch (e) {
      console.error('Error extracting RxJS operators:', e);
      // Fallback to simple implementations
      map = (fn) => (source) => source.pipe((src) => from(Promise.resolve(src).then(fn)));
      mergeMap = (fn) => (source) => source.pipe((src) => from(Promise.resolve(src).then(fn)));
      catchError = (fn) => (source) => source.pipe((src) => from(Promise.resolve(src).catch(fn)));
      flatMap = mergeMap;
    }
    
    console.log('RxJS loaded successfully');
    
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

    const baseUrl = 'http://localhost:5000/api';

    // Subjects for documents and analysis
    const documentsSubject = new BehaviorSubject([]);
    const analysisSubject = new BehaviorSubject(null);
    const compatibleTendersSubject = new BehaviorSubject([]);
    const technologyStatsSubject = new BehaviorSubject({});

    return {
      // Observable streams
      tenders$: tendersSubject.asObservable(),
      queries$: queriesSubject.asObservable(),
      loading$: loadingSubject.asObservable(),
      error$: errorSubject.asObservable(),
      documents$: documentsSubject.asObservable(),
      analysis$: analysisSubject.asObservable(),
      compatibleTenders$: compatibleTendersSubject.asObservable(),
      technologyStats$: technologyStatsSubject.asObservable(),

    // Load tenders with pagination and filters
    loadTenders: function(page = 1, pageSize = 20, filters = {}) {
      console.log('=== TenderApiService.loadTenders called ===');
      console.log('Page:', page, 'PageSize:', pageSize, 'Filters:', filters);
      
      loadingSubject.next(true);
      errorSubject.next(null);

      const params = {
        page: page,
        pageSize: pageSize,
        ...filters
      };

      console.log('Making HTTP request to:', `${baseUrl}/foundtenders`);
      console.log('Params:', params);

      // Make HTTP request
      $http.get(`${baseUrl}/foundtenders`, { params: params })
        .then(response => {
          console.log('=== Tenders API response received ===');
          console.log('Response status:', response.status);
          console.log('Response data:', response.data);
          
          // Handle both PascalCase and camelCase property names
          const tenders = response.data.tenders || response.data.Tenders || [];
          console.log('Tenders array:', tenders);
          console.log('Tenders count:', tenders.length);
          
          // Check if tenders is an array
          if (!Array.isArray(tenders)) {
            console.error('Tenders is not an array:', tenders);
            console.error('Type of tenders:', typeof tenders);
          }
          
          // Transform to expected structure
          const transformed = {
            items: tenders,
            totalCount: response.data.totalCount || 0,
            page: response.data.page || page,
            pageSize: response.data.pageSize || pageSize,
            totalPages: response.data.totalPages || 0
          };
          
          console.log('Transformed data:', transformed);
          console.log('Current tendersSubject value before update:', tendersSubject.getValue());
          
          // Update subject
          tendersSubject.next(transformed);
          loadingSubject.next(false);
          
          console.log('=== Tenders data emitted to stream ===');
          console.log('New tendersSubject value:', tendersSubject.getValue());
        })
        .catch(error => {
          console.error('Error loading tenders:', error);
          console.error('Error status:', error.status);
          console.error('Error data:', error.data);
          
          errorSubject.next(error.data || 'Failed to load tenders');
          loadingSubject.next(false);
        });
    },

      // Load search queries
      loadQueries: function() {
        console.log('=== TenderApiService.loadQueries called ===');
        loadingSubject.next(true);

        $http.get(`${baseUrl}/searchqueries`)
          .then(response => {
            console.log('=== Queries API response received ===');
            console.log('Queries data:', response.data);
            console.log('Number of queries:', response.data.length);
            
            queriesSubject.next(response.data);
            loadingSubject.next(false);
          })
          .catch(error => {
            console.error('Error loading queries:', error);
            errorSubject.next(error.data || 'Failed to load search queries');
            loadingSubject.next(false);
          });
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

      // Get tender details
      getTenderDetails: function(tenderId) {
        loadingSubject.next(true);
        errorSubject.next(null);

        const subscription = from($http.get(`${baseUrl}/foundtenders/${tenderId}/details`))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              loadingSubject.next(false);
              return data;
            },
            error => {
              console.error('Error getting tender details:', error);
              errorSubject.next(error.data || 'Failed to load tender details');
              loadingSubject.next(false);
              return null;
            }
          );
        return subscription;
      },

      // Clear error
      clearError: function() {
        errorSubject.next(null);
      },

      // ========== Document Methods ==========
      
      // Get documents for tender
      getTenderDocuments: function(tenderId) {
        loadingSubject.next(true);
        
        const subscription = from($http.get(`${baseUrl}/documents/tender/${tenderId}`))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              documentsSubject.next(data);
              loadingSubject.next(false);
            },
            error => {
              console.error('Error loading documents:', error);
              errorSubject.next(error.data || 'Failed to load documents');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Download document
      downloadDocument: function(tenderId, docType) {
        loadingSubject.next(true);
        
        const subscription = from($http.post(`${baseUrl}/documents/tender/${tenderId}/download`, {
          docType: docType
        }))
          .pipe(
            map(response => response.data),
            flatMap(() => this.getTenderDocuments(tenderId))
          )
          .subscribe(
            () => {
              loadingSubject.next(false);
            },
            error => {
              console.error('Error downloading document:', error);
              errorSubject.next(error.data || 'Failed to download document');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Download all documents for tender
      downloadAllDocuments: function(tenderId) {
        loadingSubject.next(true);
        
        const subscription = from($http.post(`${baseUrl}/documents/tender/${tenderId}/download-all`))
          .pipe(
            map(response => response.data),
            flatMap(() => this.getTenderDocuments(tenderId))
          )
          .subscribe(
            () => {
              loadingSubject.next(false);
            },
            error => {
              console.error('Error downloading all documents:', error);
              errorSubject.next(error.data || 'Failed to download all documents');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Get notification document
      getNotificationDocument: function(tenderId) {
        loadingSubject.next(true);
        
        const subscription = from($http.get(`${baseUrl}/documents/tender/${tenderId}/notification`))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              documentsSubject.next([data]);
              loadingSubject.next(false);
            },
            error => {
              console.error('Error getting notification document:', error);
              errorSubject.next(error.data || 'Failed to get notification document');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Delete document
      deleteDocument: function(documentId) {
        loadingSubject.next(true);
        
        const subscription = from($http.delete(`${baseUrl}/documents/${documentId}`))
          .subscribe(
            () => {
              loadingSubject.next(false);
            },
            error => {
              console.error('Error deleting document:', error);
              errorSubject.next(error.data || 'Failed to delete document');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // ========== Technology Analysis Methods ==========
      
      // Analyze tender
      analyzeTender: function(tenderId, documentId) {
        loadingSubject.next(true);
        
        const url = documentId 
          ? `${baseUrl}/technologyanalysis/tender/${tenderId}/analyze?documentId=${documentId}`
          : `${baseUrl}/technologyanalysis/tender/${tenderId}/analyze`;
        
        const subscription = from($http.post(url))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              analysisSubject.next(data);
              loadingSubject.next(false);
            },
            error => {
              console.error('Error analyzing tender:', error);
              errorSubject.next(error.data || 'Failed to analyze tender');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Get analysis for tender
      getAnalysis: function(tenderId) {
        loadingSubject.next(true);
        
        const subscription = from($http.get(`${baseUrl}/technologyanalysis/tender/${tenderId}`))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              analysisSubject.next(data);
              loadingSubject.next(false);
            },
            error => {
              console.error('Error getting analysis:', error);
              errorSubject.next(error.data || 'Failed to get analysis');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Get compatible tenders
      getCompatibleTenders: function(minMatchScore = 60) {
        loadingSubject.next(true);
        
        const subscription = from($http.get(`${baseUrl}/technologyanalysis/compatible-tenders`, {
          params: { minMatchScore: minMatchScore }
        }))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              compatibleTendersSubject.next(data);
              loadingSubject.next(false);
            },
            error => {
              console.error('Error getting compatible tenders:', error);
              errorSubject.next(error.data || 'Failed to get compatible tenders');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Update analysis
      updateAnalysis: function(analysisId, matchScore, isCompatible, notes) {
        loadingSubject.next(true);
        
        const subscription = from($http.put(`${baseUrl}/technologyanalysis/${analysisId}`, {
          matchScore: matchScore,
          isCompatible: isCompatible,
          notes: notes
        }))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              analysisSubject.next(data);
              loadingSubject.next(false);
            },
            error => {
              console.error('Error updating analysis:', error);
              errorSubject.next(error.data || 'Failed to update analysis');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Mark analysis as verified
      markAnalysisVerified: function(analysisId, verified = true, notes) {
        loadingSubject.next(true);
        
        const subscription = from($http.post(`${baseUrl}/technologyanalysis/${analysisId}/verify`, {
          verified: verified,
          notes: notes
        }))
          .subscribe(
            () => {
              loadingSubject.next(false);
            },
            error => {
              console.error('Error marking analysis as verified:', error);
              errorSubject.next(error.data || 'Failed to mark analysis as verified');
              loadingSubject.next(false);
            }
          );
        return subscription;
      },

      // Get technology statistics
      getTechnologyStatistics: function(fromDate, toDate) {
        loadingSubject.next(true);
        
        const params = {};
        if (fromDate) params.fromDate = fromDate;
        if (toDate) params.toDate = toDate;
        
        const subscription = from($http.get(`${baseUrl}/technologyanalysis/statistics`, { params: params }))
          .pipe(
            map(response => response.data)
          )
          .subscribe(
            data => {
              technologyStatsSubject.next(data);
              loadingSubject.next(false);
            },
            error => {
              console.error('Error getting technology statistics:', error);
              errorSubject.next(error.data || 'Failed to get technology statistics');
              loadingSubject.next(false);
            }
          );
        return subscription;
      }
    };
  }])

  // Error interceptor for HTTP requests
  .factory('errorInterceptor', ['$q', '$rootScope', function($q, $rootScope) {
    return {
      responseError: function(rejection) {
        console.error('HTTP Error:', rejection.status, rejection.data);
        
        // Extract meaningful error message
        let errorMessage = 'Произошла ошибка при выполнении запроса';
        
        if (rejection.status === 0) {
          errorMessage = 'Нет соединения с сервером. Проверьте подключение к интернету.';
        } else if (rejection.status === 401) {
          errorMessage = 'Требуется авторизация. Пожалуйста, войдите в систему.';
        } else if (rejection.status === 403) {
          errorMessage = 'Доступ запрещен. У вас недостаточно прав.';
        } else if (rejection.status === 404) {
          errorMessage = 'Запрашиваемый ресурс не найден.';
        } else if (rejection.status === 500) {
          errorMessage = 'Внутренняя ошибка сервера. Пожалуйста, попробуйте позже.';
        } else if (rejection.data && rejection.data.message) {
          errorMessage = rejection.data.message;
        } else if (rejection.data && typeof rejection.data === 'string') {
          errorMessage = rejection.data;
        }
        
        // Broadcast error event for global error handling
        $rootScope.$broadcast('httpError', {
          status: rejection.status,
          message: errorMessage,
          originalError: rejection
        });
        
        return $q.reject(rejection);
      }
    };
  }]);
