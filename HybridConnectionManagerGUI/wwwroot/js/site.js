﻿let newConnectionModal;
let logsModal;
let detailModal;
let alertModal;
let confirmModal;

document.addEventListener('DOMContentLoaded', function () {

    // Initialize the New Connections modal
    newConnectionModal = new bootstrap.Modal(document.getElementById('newConnectionModal'));

    // Add event listener for the save button
    document.getElementById('saveNewConnection').addEventListener('click', saveNewConnection);

    // Initialize the Logs Modal
    logsModal = new bootstrap.Modal(document.getElementById('logsModal'));

    // Initialize log file select change handler
    document.getElementById('logFileSelect').addEventListener('change', function () {
        if (this.value) {
            loadLogContent(this.value);
        } else {
            document.querySelector('.log-content-container').style.display = 'none';
            document.getElementById('logContent').textContent = '';
        }
    });

    // Initialize the Azure Confirm Modal
    confirmModal = new bootstrap.Modal(document.getElementById('confirmModal'));

    // Initialize the Azure Alert Modal
    alertModal = new bootstrap.Modal(document.getElementById('alertModal'));

    InitializeNewConnectionsListeners();

    initializeAllListeners();
});

function InitializeNewConnectionsListeners() {
    const connectionTypeRadios = document.getElementsByName('connectionType');
    connectionTypeRadios.forEach(radio => {
        radio.addEventListener('change', async function () {
            const connectionStringForm = document.getElementById('connectionStringForm');
            const connectionStringInput = document.getElementById('connectionStringInput');
            const alternativeForm = document.getElementById('alternativeForm');
            const newConnectionsTable = document.getElementById('newConnectionsTable');
            const noConnectionsMessage = document.getElementById('noConnectionsMessage');
            const loadingSpinner = document.getElementById('loadingSpinner');

            if (this.value === 'connectionStringType') {
                connectionStringForm.style.display = 'block';
                alternativeForm.style.display = 'none';
                connectionStringInput.disabled = false;
                noConnectionsMessage.style.display = 'none';
                loadingSpinner.style.display = 'none';
                newConnectionsTable.style.display = 'none';
            } else {
                connectionStringForm.style.display = 'none';
                alternativeForm.style.display = 'block';
                connectionStringInput.disabled = true;
                connectionStringInput.value = '';

                try {
                    const response = await fetch('/Main/GetSubscriptions');
                    if (!response.ok) {
                        throw new Error('Failed to fetch subscriptions');
                    }
                    const subscriptions = await response.json();

                    const subscriptionSelect = document.getElementById('subscriptionSelect');
                    // Clear existing options except the first one
                    while (subscriptionSelect.options.length > 1) {
                        subscriptionSelect.remove(1);
                    }

                    // Add new options
                    subscriptions.forEach(subscription => {
                        const option = new Option(subscription.displayName, subscription.subscriptionId);
                        subscriptionSelect.add(option);
                    });

                    // Reset new connnections table
                    newConnectionsTable.style.display = 'none';
                    document.getElementById('newConnectionsTableBody').innerHTML = '';

                } catch (error) {
                    console.error('Error fetching subscriptions:', error);
                    alert('Error loading subscriptions. Please try again.');
                }
            }
        });

        document.getElementById('subscriptionSelect').addEventListener('change', async function () {
            const newConnectionsTable = document.getElementById('newConnectionsTable');
            const newConnectionsTableBody = document.getElementById('newConnectionsTableBody');
            const selectAllCheckbox = document.getElementById('selectAllConnections');
            const loadingSpinner = document.getElementById('loadingSpinner');
            const noConnectionsMessage = document.getElementById('noConnectionsMessage');

            if (this.value) {

                try {
                    newConnectionsTable.style.display = 'none';
                    loadingSpinner.style.display = 'flex';
                    noConnectionsMessage.style.display = 'none';

                    // Fetch resource groups for selected subscription
                    const response = await fetch(`/Main/GetHybridConnectionsForSubscription?subscriptionId=${encodeURIComponent(this.value)}`);
                    if (!response.ok) {
                        throw new Error('Failed to fetch connections for subscription');
                    }
                    const connections = await response.json();

                    newConnectionsTableBody.innerHTML = '';
                    loadingSpinner.style.display = 'none';

                    if (connections && connections.length > 0) {
                        connections.forEach(connection => {
                            const row = document.createElement('tr');
                            row.innerHTML = `
                                        <td>
                                            <input type="checkbox" class="form-check-input new-connection-checkbox" id="connectionCheckbox" name="connectionCheckbox"
                                                   value="${connection.connectionString}">
                                        </td>
                                        <td>${connection.name}</td>
                                        <td>${connection.namespace}</td>
                                        <td>${connection.endpoint}</td>
                                    `;
                            newConnectionsTableBody.appendChild(row);
                        });

                        // Initialize checkbox handlers
                        initializeNewConnectionCheckboxes();
                        // Show table and hide no connections message
                        newConnectionsTable.style.display = 'block';
                        noConnectionsMessage.style.display = 'none';
                    } else {
                        noConnectionsMessage.style.display = 'block';
                        newConnectionsTable.style.display = 'none';
                    }

                } catch (error) {
                    console.error('Error fetching connections:', error);
                    alert('Error loading connections for subscription. Please try again.');
                    newConnectionsTable.style.display = 'none';
                    loadingSpinner.style.display = 'none';
                    noConnectionsMessage.style.display = 'none';
                }
            } else {
                // Hide the table when no subscription is selected
                newConnectionsTable.style.display = 'none';
                loadingSpinner.style.display = 'none';
                noConnectionsMessage.style.display = 'none';
            }
        });
    });
}
function initializeCheckboxListeners() {
    // Listen for individual checkbox changes
    const individualCheckboxes = document.getElementsByName('selectedIds');
    individualCheckboxes.forEach(checkbox => {
        checkbox.addEventListener('change', function (e) {
            e.stopPropagation(); // Prevent row click when clicking checkbox
            updateRemoveButton();
        });
    });
}

function updateRemoveButton() {
    const selectedCount = document.querySelectorAll('input[name="selectedIds"]:checked').length;
    const removeButton = document.getElementById('removeButton');
    removeButton.disabled = selectedCount === 0;
}

function openLogsModal() {
    // Load log files when opening the modal
    document.querySelector('.log-content-container').style.display = 'none';
    document.getElementById('logContent').textContent = '';
    loadLogFiles();
    logsModal.show();
}

async function loadLogFiles() {
    const select = document.getElementById('logFileSelect');
    select.innerHTML = '<option value="" selected>Loading...</option>';

    try {
        const response = await fetch('/Main/GetLogFiles');
        if (!response.ok) {
            throw new Error('Failed to fetch log files');
        }
        const fileNames = await response.json();

        // Reset select with default option
        select.innerHTML = '<option value="" selected>Choose a log file...</option>';

        // Add file options
        fileNames.forEach(fileName => {
            const option = new Option(fileName);
            select.add(option);
        });
    } catch (error) {
        console.error('Error loading log files:', error);
        select.innerHTML = '<option value="" selected>Error loading files...</option>';
    }
}

async function loadLogContent(fileName) {
    const contentContainer = document.querySelector('.log-content-container');
    const logContent = document.getElementById('logContent');

    try {
        const response = await fetch(`/Main/GetLogContent?fileName=${encodeURIComponent(fileName)}`);
        if (!response.ok) {
            throw new Error('Failed to fetch log content');
        }
        const content = await response.text();

        contentContainer.style.display = 'block';
        logContent.textContent = content;
    } catch (error) {
        console.error('Error loading log content:', error);
        logContent.textContent = 'Error loading log content. Please try again.';
    }
}

function copyLogContents() {
    const logContent = document.getElementById('logContent');
    if (!logContent) return;

    const text = logContent.textContent;

    // Try Electron's clipboard API first
    if (window.electronAPI) {
        window.electronAPI.copyToClipboard(text)
            .then(() => {
                showNotification('Log content copied to clipboard', 'success');
            })
            .catch(err => {
                console.error('Failed to copy log content using Electron:', err);
                // Fall back to web API if Electron's API fails
                fallbackCopy(text);
            });
    } else {
        // Fall back to web API if not in Electron
        fallbackCopy(text);
    }
}

function fallbackCopy(text) {
    navigator.clipboard.writeText(text)
        .then(() => {
            showNotification('Log content copied to clipboard', 'success');
        })
        .catch(err => {
            console.error('Failed to copy log content:', err);
            showNotification('Failed to copy log content', 'error');
        });
}

function updateSaveButtonState() {
    const saveButton = document.getElementById('saveNewConnection');
    const hasSelectedConnections = Array.from(document.getElementsByName('connectionCheckbox'))
        .some(cb => cb.checked);

    saveButton.disabled = !hasSelectedConnections;
}

function initializeAllListeners() {

    initializeCheckboxListeners();
    updateRemoveButton();

    document.querySelectorAll('.item-row').forEach(row => {
        row.addEventListener('click', function () {
            const details = {
                namespace: this.dataset.namespace,
                name: this.dataset.name,
                endpoint: this.dataset.endpoint,
                status: this.dataset.status,
                createdOn: this.dataset.createdOn,
                lastUpdated: this.dataset.lastUpdated
            };
            showDetails(details);
        });
    });
}

function initializeNewConnectionCheckboxes() {
    // Individual checkbox handlers
    const newConnectionCheckboxes = document.getElementsByName('connectionCheckbox');
    Array.from(newConnectionCheckboxes).forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            updateSaveButtonState();
        });
    });
}

function refreshContent() {
    $("#refreshButton").prop("disabled", true);

    $.ajax({
        url: '/Main/GetUpdatedData', // Replace with your actual controller and action
        type: 'GET',
        success: function (result) {
            $("#contentArea").html(result);
            setTimeout(() => {
                initializeAllListeners();
            }, 0);
        },
        error: function (error) {
            console.error('Error refreshing content:', error);
            alert('Error refreshing content. Please try again.');
        },
        complete: function () {
            $("#refreshButton").prop("disabled", false);
        }
    });
}

function removeSelectedConnections() {
    const selectedBoxes = document.querySelectorAll('input[name="selectedIds"]:checked');
    if (selectedBoxes.length === 0) return;

    const count = selectedBoxes.length;
    const confirmMessage = `The selected Hybrid Connections will be removed from your machine and your App Service will no longer be able to connect to these endpoints.`;

    showAzureConfirm(confirmMessage, "Remove Hybrid Connection")
        .then(confirmed => {
            if (!confirmed) return;

            const selectedItems = Array.from(selectedBoxes).map(checkbox => ({
                namespace: checkbox.dataset.namespace,
                name: checkbox.dataset.name
            }));

            $.ajax({
                url: '/Main/Remove',
                type: 'POST',
                data: JSON.stringify({ connections: selectedItems }),
                contentType: 'application/json',
                success: function (result) {
                    if (result.success) {
                        const count = selectedItems.length;
                        showNotification(
                            `Successfully removed ${count} ${count === 1 ? 'connection' : 'connections'}`,
                            'success'
                        );
                        refreshContent();
                    } else {
                        showNotification('Error removing connections: ' + result.message, 'error');
                    }
                },
                error: function (error) {
                    console.error('Error removing connections:', error);
                    showNotification('Error removing connections. Please try again.', 'error');
                }
            });
        });
}

function removeCurrentConnection() {
    // Using the currentItem that's already set in showDetails()
    if (currentItem) {
        const confirmMessage = `This Hybrid Connection will be removed from your machine and your App Service will no longer be able to connect to this endpoint.`;

        showAzureConfirm(confirmMessage, "Remove Hybrid Connection")
            .then(confirmed => {
                if (!confirmed) return;

                $.ajax({
                    url: '/Main/Remove',
                    type: 'POST',
                    data: JSON.stringify({ connections: [{ namespace: currentItem.namespace, name: currentItem.name }] }),
                    contentType: 'application/json',
                    success: function (result) {
                        if (result.success) {
                            detailModal.hide();
                            showNotification(
                                `Successfully removed ${currentItem.namespace}/${currentItem.name}`,
                                'success'
                            );
                            refreshContent();
                        } else {
                            showNotification('Error removing connection: ' + result.message, 'error');
                        }
                    },
                    error: function (error) {
                        console.error('Error removing connection:', error);
                        showNotification('Error removing connection. Please try again.', 'error');
                    }
                });
            });
    } else {
        showNotification('Unable to delete connection', 'error');
    }
}

function showAzureConfirm(message, title = "Confirm Action") {
    return new Promise((resolve) => {
        const messageElement = document.getElementById('confirmMessage');
        const titleElement = document.getElementById('confirmModalTitle');
        const confirmButton = document.getElementById('confirmButton');

        messageElement.textContent = message;
        titleElement.textContent = title;

        // Clear any previous event listeners
        const newConfirmButton = confirmButton.cloneNode(true);
        confirmButton.parentNode.replaceChild(newConfirmButton, confirmButton);

        // Add new event listener
        newConfirmButton.addEventListener('click', function () {
            confirmModal.hide();
            resolve(true);
        });

        // Add event listener for cancel (modal dismiss)
        const modalElement = document.getElementById('confirmModal');
        const handleModalHidden = function () {
            modalElement.removeEventListener('hidden.bs.modal', handleModalHidden);
            resolve(false);
        };
        modalElement.addEventListener('hidden.bs.modal', handleModalHidden);

        // Show the modal
        confirmModal.show();
    });
}

function openNewConnectionModal() {
    // Clear the form
    const newConnectionForm = document.getElementById('newConnectionForm');
    const connectionStringForm = document.getElementById('connectionStringForm');
    const connectionStringInput = document.getElementById('connectionStringInput');
    const alternativeForm = document.getElementById('alternativeForm');
    const newConnectionsTable = document.getElementById('newConnectionsTable');
    const connectionStringRadio = document.getElementById("connectionStringRadio");
    const loadingSpinner = document.getElementById('loadingSpinner');
    const noConnectionsMessage = document.getElementById('noConnectionsMessage');

    newConnectionForm.reset();
    loadingSpinner.style.display = 'none';
    connectionStringForm.style.display = 'block';
    newConnectionsTable.style.display = 'none';
    connectionStringRadio.checked = true;
    alternativeForm.style.display = 'none';
    connectionStringInput.disabled = false;
    noConnectionsMessage.style.display = 'none';

    // Show the modal
    newConnectionModal.show();

    setTimeout(() => {
        document.getElementById('connectionStringInput').focus();
    }, 100);
}

function saveNewConnection() {
    const form = document.getElementById('newConnectionForm');
    const connectionType = document.querySelector('input[name="connectionType"]:checked').value;

    if (connectionType === 'connectionStringType') {
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const saveButton = document.getElementById('saveNewConnection');

        saveButton.disabled = true;
        saveButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Creating...';

        const data = {
            connectionString: document.getElementById('connectionStringInput').value
        };

        $.ajax({
            url: '/Main/Add',
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (result) {
                if (result.success) {
                    newConnectionModal.hide();
                    refreshContent();
                    showNotification(
                        'Successfully added new connection',
                        'success'
                    );
                } else {
                    showAzureAlert('Error creating connection: ' + result.message, 'Input Error')
                        .then(() => {
                            document.getElementById('connectionStringInput').focus();
                        });
                }
            },
            error: function (error) {
                console.error('Error creating connection:', error);
                showAzureAlert('Error creating connection: ' + result.message, 'Operation Error')
                    .then(() => {
                        document.getElementById('connectionStringInput').focus();
                    });
            },
            complete: function () {
                saveButton.disabled = false;
                saveButton.innerHTML = 'Create';
            }
        });
    }
    else {
        const selectedSubscription = document.getElementById('subscriptionSelect').value;
        const selectedBoxes = document.querySelectorAll('input[name="connectionCheckbox"]:checked');

        if (!selectedSubscription) {
            alert('Please select a subscription');
            return;
        }

        if (selectedBoxes.length === 0) return;

        const selectedItems = Array.from(selectedBoxes).map(checkbox => ({
            connectionString: checkbox.value
        }));

        const saveButton = document.getElementById('saveNewConnection');
        saveButton.disabled = true;
        saveButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Creating...';

        const data = {
            connections: selectedItems,
        };

        $.ajax({
            url: '/Main/AddMultiple',
            type: 'POST',
            data: JSON.stringify(data),
            contentType: 'application/json',
            success: function (result) {
                if (result.success) {
                    newConnectionModal.hide();
                    refreshContent();
                    const count = selectedItems.length;
                    showNotification(
                        `Successfully added ${count} ${count === 1 ? 'connection' : 'connections'}`,
                        'success'
                    );
                } else {
                    newConnectionModal.hide();
                    refreshContent();
                    showNotification(
                        `Failed to add some connections`,
                        'error'
                    );
                }
            },
            error: function (error) {
                newConnectionModal.hide();
                refreshContent();
                console.error('Error creating connection:', error);
                showNotification(
                    `Failed to add some connections`,
                    'error'
                );
            },
            complete: function () {
                saveButton.disabled = false;
                saveButton.innerHTML = 'Create';
            }
        });
    }
}

function showAzureAlert(message, title = "Error") {
    const messageElement = document.getElementById('alertMessage');
    const titleElement = document.getElementById('alertModalTitle');

    messageElement.textContent = message;
    titleElement.textContent = title;

    // Show the modal
    alertModal.show();

    // Return a promise that resolves when the modal is closed
    return new Promise(resolve => {
        document.getElementById('alertModal').addEventListener('hidden.bs.modal', function handler() {
            document.getElementById('alertModal').removeEventListener('hidden.bs.modal', handler);
            resolve();
        });
    });
}

function showDetails(item) {
    currentItem = item;

    // Update modal content
    document.getElementById('modal-name').textContent = item.name;

    // If you have these additional fields in your model:
    document.getElementById('modal-created-on').textContent = formatDate(item.createdOn);
    document.getElementById('modal-last-updated').textContent = formatDate(item.lastUpdated);
    document.getElementById('modal-status').textContent = item.status;
    document.getElementById('modal-namespace').textContent = item.namespace;
    document.getElementById('modal-endpoint').textContent = item.endpoint;

    // Show the modal
    detailModal = new bootstrap.Modal(document.getElementById('detailsModal'));
    detailModal.show();
}

function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return date.toLocaleString();
}

function getAzurePortalUrl(namespace, name) {
    return "https://portal.azure.com/#" + "@@" + "/resource/providers/Microsoft.Relay/namespaces/" + encodeURIComponent(namespace) + "/hybridConnections/" + encodeURIComponent(name);
}

function openInAzurePortal() {
    // Using the currentItem that's already set in showDetails()
    if (currentItem && currentItem.namespace && currentItem.name) {
        const portalUrl = getAzurePortalUrl(currentItem.namespace, currentItem.name);
        window.open(portalUrl, '_blank');
    } else {
        console.error('Missing connection details for Azure Portal URL');
        showNotification('Unable to open Azure Portal: Missing connection details', 'error');
    }
}

function showNotification(message, type = 'success') {
    const banner = document.getElementById('notificationBanner');
    const messageElement = document.getElementById('notificationMessage');
    const iconElement = banner.querySelector('.notification-icon i');

    // Reset classes
    banner.className = 'notification-banner';
    banner.classList.add(type);

    // Update icon based on type
    if (type === 'success') {
        iconElement.className = 'fas fa-check-circle';
    } else if (type === 'error') {
        iconElement.className = 'fas fa-exclamation-circle';
    }

    // Set message
    messageElement.textContent = message;

    // Show banner with animation
    banner.style.display = 'flex';

    // Auto hide after 5 seconds
    const hideTimer = setTimeout(() => {
        hideNotification();
    }, 5000);

    // Store timer ID on the banner element
    banner.dataset.hideTimer = hideTimer;
}

function hideNotification() {
    const banner = document.getElementById('notificationBanner');

    // Clear any existing hide timer
    if (banner.dataset.hideTimer) {
        clearTimeout(parseInt(banner.dataset.hideTimer));
    }

    // Add fade-out animation
    banner.classList.add('fade-out');

    // Hide after animation completes
    setTimeout(() => {
        banner.style.display = 'none';
        banner.classList.remove('fade-out');
    }, 300);
}