(function() {
    'use strict';

    angular.module('tenderTracker')
        .controller('NotificationSettingsController', NotificationSettingsController);

    NotificationSettingsController.$inject = ['$scope', '$http', 'NotificationService'];

    function NotificationSettingsController($scope, $http, NotificationService) {
        var vm = this;
        
        // Default user ID (в реальном приложении получать из аутентификации)
        vm.userId = 1;
        vm.settings = null;
        vm.loading = false;
        vm.saving = false;
        vm.testing = false;
        vm.message = '';
        vm.error = '';
        vm.success = '';

        // Notification types
        vm.notificationTypes = [
            { value: 'email', label: 'Email' },
            { value: 'telegram', label: 'Telegram' },
            { value: 'webhook', label: 'Webhook' }
        ];

        // Initialize
        vm.init = function() {
            loadSettings();
        };

        // Load notification settings
        function loadSettings() {
            vm.loading = true;
            vm.error = '';
            
            $http.get('/api/notificationsettings/' + vm.userId)
                .then(function(response) {
                    vm.settings = response.data;
                    vm.loading = false;
                })
                .catch(function(error) {
                    if (error.status === 404) {
                        // Settings not found, create default
                        vm.settings = getDefaultSettings();
                        vm.loading = false;
                    } else {
                        vm.error = 'Ошибка загрузки настроек: ' + (error.data || error.statusText);
                        vm.loading = false;
                        NotificationService.error(vm.error);
                    }
                });
        }

        // Get default settings
        function getDefaultSettings() {
            return {
                userId: vm.userId,
                notificationType: 'email',
                emailAddress: '',
                telegramChatId: '',
                webhookUrl: '',
                notifyOnNewTenders: true,
                notifyOnDeadlineApproaching: true,
                notifyOnTechnologyMatch: true,
                deadlineWarningDays: 3,
                filterCriteria: {}
            };
        }

        // Save settings
        vm.saveSettings = function() {
            if (!vm.settings) return;
            
            vm.saving = true;
            vm.error = '';
            vm.success = '';

            var url = '/api/notificationsettings';
            var method = 'POST';
            
            if (vm.settings.id) {
                url = '/api/notificationsettings/' + vm.userId;
                method = 'PUT';
            }

            $http({
                method: method,
                url: url,
                data: vm.settings
            })
            .then(function(response) {
                vm.settings = response.data;
                vm.saving = false;
                vm.success = 'Настройки успешно сохранены';
                NotificationService.success(vm.success);
            })
            .catch(function(error) {
                vm.error = 'Ошибка сохранения настроек: ' + (error.data || error.statusText);
                vm.saving = false;
                NotificationService.error(vm.error);
            });
        };

        // Delete settings
        vm.deleteSettings = function() {
            if (!confirm('Вы уверены, что хотите удалить настройки уведомлений?')) {
                return;
            }

            vm.saving = true;
            $http.delete('/api/notificationsettings/' + vm.userId)
                .then(function() {
                    vm.settings = getDefaultSettings();
                    vm.saving = false;
                    vm.success = 'Настройки удалены';
                    NotificationService.success(vm.success);
                })
                .catch(function(error) {
                    vm.error = 'Ошибка удаления настроек: ' + (error.data || error.statusText);
                    vm.saving = false;
                    NotificationService.error(vm.error);
                });
        };

        // Test notification
        vm.testNotification = function() {
            if (!vm.settings || !vm.settings.id) {
                vm.error = 'Сначала сохраните настройки';
                NotificationService.error(vm.error);
                return;
            }

            vm.testing = true;
            vm.error = '';
            vm.success = '';

            var testMessage = vm.message || 'Тестовое уведомление от TenderTracker';
            
            // В реальном приложении здесь будет вызов API для тестового уведомления
            // Пока просто имитируем успех
            setTimeout(function() {
                vm.testing = false;
                vm.success = 'Тестовое уведомление отправлено (имитация)';
                NotificationService.success(vm.success);
                $scope.$apply();
            }, 1000);
        };

        // Add filter criterion
        vm.addFilterCriterion = function() {
            if (!vm.settings.filterCriteria) {
                vm.settings.filterCriteria = {};
            }
            
            var key = prompt('Введите ключ фильтра (например: minPrice, maxPrice, region):');
            if (!key) return;
            
            var value = prompt('Введите значение фильтра:');
            if (value === null) return;
            
            vm.settings.filterCriteria[key] = value;
        };

        // Remove filter criterion
        vm.removeFilterCriterion = function(key) {
            if (confirm('Удалить фильтр "' + key + '"?')) {
                delete vm.settings.filterCriteria[key];
            }
        };

        // Initialize controller
        vm.init();
    }
})();
