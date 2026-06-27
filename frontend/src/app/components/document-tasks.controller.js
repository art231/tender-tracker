(function() {
    'use strict';

    angular.module('tenderTrackerApp')
        .controller('DocumentTasksController', DocumentTasksController);

    DocumentTasksController.$inject = ['$scope', '$http', '$interval', 'NotificationService'];

    function DocumentTasksController($scope, $http, $interval, NotificationService) {
        var vm = this;
        var refreshInterval = null;
        
        // Инициализация
        vm.tasks = [];
        vm.stats = {};
        vm.filters = {
            status: '',
            docType: '',
            tenderId: null
        };
        vm.loading = false;
        vm.refreshing = false;
        vm.autoRefresh = true;
        vm.refreshInterval = 30; // секунды
        
        // Статусы задач для фильтрации
        vm.statuses = [
            { value: '', label: 'Все статусы' },
            { value: 'Pending', label: 'Ожидание' },
            { value: 'InProgress', label: 'В процессе' },
            { value: 'Completed', label: 'Завершено' },
            { value: 'Failed', label: 'Ошибка' },
            { value: 'Cancelled', label: 'Отменено' }
        ];
        
        // Типы документов
        vm.docTypes = [
            { value: '', label: 'Все типы' },
            { value: 'notification', label: 'Извещение' },
            { value: 'all', label: 'Все документы' },
            { value: 'technical_specification', label: 'Техническое задание' },
            { value: 'contract_draft', label: 'Проект контракта' },
            { value: 'evaluation_criteria', label: 'Критерии оценки' }
        ];
        
        // Функции
        vm.loadTasks = loadTasks;
        vm.loadStats = loadStats;
        vm.refresh = refresh;
        vm.toggleAutoRefresh = toggleAutoRefresh;
        vm.retryTask = retryTask;
        vm.cancelTask = cancelTask;
        vm.deleteTask = deleteTask;
        vm.createTask = createTask;
        vm.getStatusClass = getStatusClass;
        vm.getPriorityClass = getPriorityClass;
        vm.formatDate = formatDate;
        vm.clearFilters = clearFilters;
        
        // Инициализация
        init();
        
        function init() {
            loadTasks();
            loadStats();
            startAutoRefresh();
            
            // Очистка интервала при уничтожении контроллера
            $scope.$on('$destroy', function() {
                stopAutoRefresh();
            });
        }
        
        function loadTasks() {
            vm.loading = true;
            
            var params = {};
            if (vm.filters.status) params.status = vm.filters.status;
            if (vm.filters.docType) params.docType = vm.filters.docType;
            if (vm.filters.tenderId) params.tenderId = vm.filters.tenderId;
            
            $http.get('/api/DocumentDownloadTasks', { params: params })
                .then(function(response) {
                    vm.tasks = response.data;
                    vm.loading = false;
                    vm.refreshing = false;
                })
                .catch(function(error) {
                    console.error('Error loading tasks:', error);
                    NotificationService.error('Ошибка загрузки задач: ' + (error.data || error.statusText));
                    vm.loading = false;
                    vm.refreshing = false;
                });
        }
        
        function loadStats() {
            $http.get('/api/DocumentDownloadTasks/stats')
                .then(function(response) {
                    vm.stats = response.data;
                })
                .catch(function(error) {
                    console.error('Error loading stats:', error);
                });
        }
        
        function refresh() {
            vm.refreshing = true;
            loadTasks();
            loadStats();
        }
        
        function toggleAutoRefresh() {
            if (vm.autoRefresh) {
                startAutoRefresh();
            } else {
                stopAutoRefresh();
            }
        }
        
        function startAutoRefresh() {
            if (refreshInterval) {
                $interval.cancel(refreshInterval);
            }
            refreshInterval = $interval(function() {
                refresh();
            }, vm.refreshInterval * 1000);
        }
        
        function stopAutoRefresh() {
            if (refreshInterval) {
                $interval.cancel(refreshInterval);
                refreshInterval = null;
            }
        }
        
        function retryTask(taskId) {
            if (!confirm('Повторить выполнение задачи?')) return;
            
            $http.post('/api/DocumentDownloadTasks/' + taskId + '/retry')
                .then(function() {
                    NotificationService.success('Задача запланирована на повторное выполнение');
                    refresh();
                })
                .catch(function(error) {
                    console.error('Error retrying task:', error);
                    NotificationService.error('Ошибка при повторении задачи: ' + (error.data || error.statusText));
                });
        }
        
        function cancelTask(taskId) {
            if (!confirm('Отменить задачу?')) return;
            
            $http.post('/api/DocumentDownloadTasks/' + taskId + '/cancel')
                .then(function() {
                    NotificationService.success('Задача отменена');
                    refresh();
                })
                .catch(function(error) {
                    console.error('Error cancelling task:', error);
                    NotificationService.error('Ошибка при отмене задачи: ' + (error.data || error.statusText));
                });
        }
        
        function deleteTask(taskId) {
            if (!confirm('Удалить задачу?')) return;
            
            $http.delete('/api/DocumentDownloadTasks/' + taskId)
                .then(function() {
                    NotificationService.success('Задача удалена');
                    refresh();
                })
                .catch(function(error) {
                    console.error('Error deleting task:', error);
                    NotificationService.error('Ошибка при удалении задачи: ' + (error.data || error.statusText));
                });
        }
        
        function createTask() {
            var taskData = {
                tenderId: vm.newTaskTenderId,
                docType: vm.newTaskDocType,
                priority: vm.newTaskPriority || 'normal'
            };
            
            $http.post('/api/DocumentDownloadTasks', taskData)
                .then(function(response) {
                    NotificationService.success('Задача создана');
                    vm.newTaskTenderId = null;
                    vm.newTaskDocType = '';
                    vm.newTaskPriority = 'normal';
                    refresh();
                })
                .catch(function(error) {
                    console.error('Error creating task:', error);
                    NotificationService.error('Ошибка при создании задачи: ' + (error.data || error.statusText));
                });
        }
        
        function getStatusClass(status) {
            switch(status) {
                case 'Pending': return 'badge badge-warning';
                case 'InProgress': return 'badge badge-info';
                case 'Completed': return 'badge badge-success';
                case 'Failed': return 'badge badge-danger';
                case 'Cancelled': return 'badge badge-secondary';
                default: return 'badge badge-light';
            }
        }
        
        function getPriorityClass(priority) {
            switch(priority) {
                case 'high': return 'badge badge-danger';
                case 'normal': return 'badge badge-warning';
                case 'low': return 'badge badge-info';
                default: return 'badge badge-light';
            }
        }
        
        function formatDate(dateString) {
            if (!dateString) return '-';
            var date = new Date(dateString);
            return date.toLocaleString('ru-RU');
        }
        
        function clearFilters() {
            vm.filters = {
                status: '',
                docType: '',
                tenderId: null
            };
            loadTasks();
        }
    }
})();
