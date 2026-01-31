// Technology Filter Controller
angular.module('tenderTrackerApp')
  .controller('TechnologyFilterController', ['$scope', 'TenderApiService', '$timeout',
    function($scope, TenderApiService, $timeout) {
      console.log('TechnologyFilterController constructor called');
      
      // Reactive data streams
      $scope.compatibleTenders = [];
      $scope.technologyStats = {};
      $scope.loading = false;
      $scope.error = null;
      
      // Filter settings
      $scope.filters = {
        minMatchScore: 60,
        showOnlyVerified: false,
        showOnlyCompatible: true,
        technology: '',
        fromDate: null,
        toDate: null
      };
      
      // Available technologies for filtering
      $scope.availableTechnologies = [
        '.NET', 'C#', 'ASP.NET', '.NET Core', 'Entity Framework',
        'PostgreSQL', 'Postgres', 'PSQL',
        'React', 'React.js', 'Redux', 'Next.js',
        'Java', 'Spring', 'Spring Boot', 'Hibernate',
        'Angular', 'Angular.js', 'TypeScript',
        'Android', 'Kotlin', 'Android SDK', 'Flutter',
        'Docker', 'Kubernetes', 'CI/CD', 'AWS', 'Azure', 'GCP',
        'Machine Learning', 'ML', 'TensorFlow', 'PyTorch',
        'RAG', 'LLM', 'LangChain', 'Retrieval-Augmented Generation'
      ];
      
      // Initialize
      function init() {
        console.log('Initializing TechnologyFilterController...');
        
        // Subscribe to compatible tenders stream
        const compatibleTendersSubscription = TenderApiService.compatibleTenders$
          .subscribe(tenders => {
            $scope.$evalAsync(() => {
              $scope.compatibleTenders = tenders;
              console.log('Compatible tenders updated:', tenders.length);
            });
          });
        
        // Subscribe to technology stats stream
        const statsSubscription = TenderApiService.technologyStats$
          .subscribe(stats => {
            $scope.$evalAsync(() => {
              $scope.technologyStats = stats;
              console.log('Technology stats updated:', stats);
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
          compatibleTendersSubscription.unsubscribe();
          statsSubscription.unsubscribe();
          loadingSubscription.unsubscribe();
          errorSubscription.unsubscribe();
        });
        
        // Load initial data
        loadCompatibleTenders();
        loadTechnologyStats();
      }
      
      // Load compatible tenders
      function loadCompatibleTenders() {
        TenderApiService.getCompatibleTenders($scope.filters.minMatchScore);
      }
      
      // Load technology statistics
      function loadTechnologyStats() {
        TenderApiService.getTechnologyStatistics($scope.filters.fromDate, $scope.filters.toDate);
      }
      
      // Apply filters
      $scope.applyFilters = function() {
        loadCompatibleTenders();
        loadTechnologyStats();
      };
      
      // Clear filters
      $scope.clearFilters = function() {
        $scope.filters = {
          minMatchScore: 60,
          showOnlyVerified: false,
          showOnlyCompatible: true,
          technology: '',
          fromDate: null,
          toDate: null
        };
        applyFilters();
      };
      
      // Format date
      $scope.formatDate = function(dateString) {
        if (!dateString) return 'Не указана';
        return new Date(dateString).toLocaleString('ru-RU');
      };
      
      // Format match score with color
      $scope.formatMatchScore = function(score) {
        let colorClass = 'text-danger';
        if (score >= 80) colorClass = 'text-success';
        else if (score >= 60) colorClass = 'text-warning';
        
        return `<span class="${colorClass} fw-bold">${score}%</span>`;
      };
      
      // Get analysis status badge
      $scope.getAnalysisStatusBadge = function(analysis) {
        if (!analysis) return '<span class="badge bg-secondary">Нет анализа</span>';
        
        if (analysis.isCompatible) {
          if (analysis.manuallyVerified) {
            return '<span class="badge bg-success">Совместим (проверено)</span>';
          } else {
            return '<span class="badge bg-warning">Совместим (авто)</span>';
          }
        } else {
          return '<span class="badge bg-danger">Не совместим</span>';
        }
      };
      
      // Get matched technologies list
      $scope.getMatchedTechnologiesList = function(analysis) {
        if (!analysis || !analysis.matchedTechnologies || analysis.matchedTechnologies.length === 0) {
          return 'Не найдено';
        }
        
        return analysis.matchedTechnologies
          .map(tech => `${tech.technology} (${tech.count})`)
          .join(', ');
      };
      
      // Navigate to documents page
      $scope.goToDocuments = function(tenderId) {
        window.location.hash = `#/documents/${tenderId}`;
      };
      
      // View tender on zakupki.gov.ru
      $scope.viewTender = function(tender) {
        if (tender.directLinkToSource) {
          window.open(tender.directLinkToSource, '_blank');
        } else {
          const searchString = tender.purchaseNumber && tender.purchaseNumber !== 'N/A' 
            ? tender.purchaseNumber 
            : tender.title || '';
          window.open(`https://zakupki.gov.ru/epz/order/quicksearch/search.html?searchString=${encodeURIComponent(searchString)}`, '_blank');
        }
      };
      
      // Clear error
      $scope.clearError = function() {
        TenderApiService.clearError();
      };
      
      // Get technology frequency
      $scope.getTechnologyFrequency = function(techName) {
        if (!$scope.technologyStats.technologies || !$scope.technologyStats.technologies[techName]) {
          return 0;
        }
        return $scope.technologyStats.technologies[techName];
      };
      
      // Get top technologies
      $scope.getTopTechnologies = function() {
        if (!$scope.technologyStats.technologies) return [];
        
        return Object.entries($scope.technologyStats.technologies)
          .map(([tech, count]) => ({ tech, count }))
          .sort((a, b) => b.count - a.count)
          .slice(0, 10);
      };
      
      // Initialize controller
      init();
    }]);
