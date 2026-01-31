// Documents Controller
angular.module('tenderTrackerApp')
  .controller('DocumentsController', ['$scope', 'TenderApiService', '$timeout',
    function($scope, TenderApiService, $timeout) {
      console.log('DocumentsController constructor called');
      
      // Current tender ID from input (set by parent scope)
      $scope.tenderId = null;
      
      // Reactive data streams
      $scope.documents = [];
      $scope.analysis = null;
      $scope.loading = false;
      $scope.error = null;
      $scope.selectedDocument = null;
      
      // Document types for download
      $scope.documentTypes = [
        { value: 'notification', label: 'Извещение (ТЗ)' },
        { value: 'protocol', label: 'Протокол' },
        { value: 'contract', label: 'Контракт' },
        { value: 'specification', label: 'Техническая спецификация' },
        { value: 'other', label: 'Другие документы' }
      ];
      
      $scope.selectedDocType = 'notification';
      
      // Analysis settings
      $scope.analysisSettings = {
        minMatchScore: 60,
        showOnlyCompatible: true
      };
      
      // Initialize
      function init() {
        console.log('Initializing DocumentsController...');
        
        if ($scope.tenderId) {
          // Subscribe to documents stream
          const documentsSubscription = TenderApiService.documents$
            .subscribe(documents => {
              $scope.$evalAsync(() => {
                $scope.documents = documents;
                console.log('Documents updated:', documents.length);
              });
            });
          
          // Subscribe to analysis stream
          const analysisSubscription = TenderApiService.analysis$
            .subscribe(analysis => {
              $scope.$evalAsync(() => {
                $scope.analysis = analysis;
                console.log('Analysis updated:', analysis);
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
            documentsSubscription.unsubscribe();
            analysisSubscription.unsubscribe();
            loadingSubscription.unsubscribe();
            errorSubscription.unsubscribe();
          });
          
          // Load initial data
          loadDocuments();
          loadAnalysis();
        }
      }
      
      // Load documents for current tender
      function loadDocuments() {
        if ($scope.tenderId) {
          TenderApiService.getTenderDocuments($scope.tenderId);
        }
      }
      
      // Load analysis for current tender
      function loadAnalysis() {
        if ($scope.tenderId) {
          TenderApiService.getAnalysis($scope.tenderId);
        }
      }
      
      // Download document
      $scope.downloadDocument = function() {
        if ($scope.tenderId && $scope.selectedDocType) {
          TenderApiService.downloadDocument($scope.tenderId, $scope.selectedDocType);
        }
      };
      
      // Download all documents
      $scope.downloadAllDocuments = function() {
        if ($scope.tenderId) {
          TenderApiService.downloadAllDocuments($scope.tenderId);
        }
      };
      
      // Download notification document
      $scope.downloadNotification = function() {
        if ($scope.tenderId) {
          TenderApiService.getNotificationDocument($scope.tenderId);
        }
      };
      
      // Analyze tender
      $scope.analyzeTender = function(documentId) {
        if ($scope.tenderId) {
          TenderApiService.analyzeTender($scope.tenderId, documentId);
        }
      };
      
      // Delete document
      $scope.deleteDocument = function(documentId) {
        if (confirm('Вы уверены, что хотите удалить этот документ?')) {
          TenderApiService.deleteDocument(documentId);
        }
      };
      
      // Update analysis manually
      $scope.updateAnalysis = function() {
        if ($scope.analysis) {
          const matchScore = parseInt(prompt('Введите процент совпадения (0-100):', $scope.analysis.matchScore));
          if (!isNaN(matchScore) && matchScore >= 0 && matchScore <= 100) {
            const isCompatible = confirm('Тендер совместим с вашим стеком технологий?');
            const notes = prompt('Комментарии к анализу:', $scope.analysis.analysisNotes || '');
            
            TenderApiService.updateAnalysis(
              $scope.analysis.id,
              matchScore,
              isCompatible,
              notes
            );
          }
        }
      };
      
      // Mark analysis as verified
      $scope.markAsVerified = function() {
        if ($scope.analysis) {
          const notes = prompt('Комментарии к проверке:', $scope.analysis.analysisNotes || '');
          TenderApiService.markAnalysisVerified($scope.analysis.id, true, notes);
        }
      };
      
      // Select document
      $scope.selectDocument = function(document) {
        $scope.selectedDocument = document;
      };
      
      // Format date
      $scope.formatDate = function(dateString) {
        if (!dateString) return 'Не указана';
        return new Date(dateString).toLocaleString('ru-RU');
      };
      
      // Format document type
      $scope.formatDocType = function(docType) {
        const typeMap = {
          'notification': 'Извещение (ТЗ)',
          'protocol': 'Протокол',
          'contract': 'Контракт',
          'specification': 'Техническая спецификация',
          'other': 'Другие документы'
        };
        return typeMap[docType] || docType;
      };
      
      // Get analysis status class
      $scope.getAnalysisStatusClass = function() {
        if (!$scope.analysis) return 'text-muted';
        
        if ($scope.analysis.isCompatible) {
          return $scope.analysis.manuallyVerified ? 'text-success' : 'text-warning';
        } else {
          return 'text-danger';
        }
      };
      
      // Get analysis status text
      $scope.getAnalysisStatusText = function() {
        if (!$scope.analysis) return 'Не анализирован';
        
        let status = $scope.analysis.isCompatible ? 'Совместим' : 'Не совместим';
        if ($scope.analysis.manuallyVerified) {
          status += ' (проверено вручную)';
        } else {
          status += ' (автоматический анализ)';
        }
        return status;
      };
      
      // Get match score class
      $scope.getMatchScoreClass = function(score) {
        if (score >= 80) return 'text-success';
        if (score >= 60) return 'text-warning';
        return 'text-danger';
      };
      
      // Clear error
      $scope.clearError = function() {
        TenderApiService.clearError();
      };
      
      // View document source JSON
      $scope.viewSourceJson = function(document) {
        if (document.sourceJson) {
          const jsonStr = JSON.stringify(document.sourceJson, null, 2);
          const win = window.open('', '_blank');
          win.document.write('<pre>' + jsonStr + '</pre>');
        }
      };
      
      // Initialize controller
      init();
    }]);
