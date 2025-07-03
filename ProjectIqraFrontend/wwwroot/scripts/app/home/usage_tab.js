// --- Global Variables for Usage Tab ---
let usageChartInstance = null;
let currentUsagePage = 1;
let currentUsageNextCursor = null;
let currentUsagePrevCursor = null;
let isUsageLoading = false;
const USAGE_PAGE_SIZE = 10;

// --- Element Variables ---
const usageTab = $("#usage-tab");
const usageChartTitle = usageTab.find("#usageChartTitle");
const usageChartCanvas = usageTab.find("#usageChart");
const usageChartSpinner = usageTab.find("#usageChartSpinner");
const timeRangeButtons = usageTab.find('input[name="timeRange"]');
const usageHistoryTableBody = usageTab.find("#usageHistoryTable tbody");
const usagePagination = {
    controls: usageTab.find("#usagePaginationControls"),
    prevBtn: usageTab.find("#usagePrevButton"),
    nextBtn: usageTab.find("#usageNextButton"),
    pageInfo: usageTab.find("#usagePageInfo")
};

// API Functions
function FetchUsageSummaryFromAPI(timeRange, successCallback, errorCallback) {
    $.ajax({
        url: '/app/user/usage/summary',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            timeRange: timeRange
        }),
        success: (response) => {
            if (!response.success) return errorCallback(response);
            successCallback(response.data);
        },
        error: (error) => errorCallback(error)
    });
}

function FetchUsageHistoryFromAPI(pageSize, nextCursor, prevCursor, successCallback, errorCallback) {
    $.ajax({
        url: '/app/user/usage/history',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            limit: pageSize,
            nextCursor: nextCursor,
            previousCursor: prevCursor
        }),
        success: (response) => {
            if (!response.success) return errorCallback(response);
            successCallback(response.data);
        },
        error: (error) => errorCallback(error)
    });
}


// --- Chart Functions ---

function initializeUsageChart() {
    const ctx = usageChartCanvas[0].getContext('2d');
    usageChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: [],
            datasets: [{
                label: 'Minutes Used',
                data: [],
                backgroundColor: 'rgba(54, 162, 235, 0.6)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1,
                borderRadius: 4,
                barThickness: 'flex'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) { return value + ' min' }
                    }
                },
                x: {
                    grid: {
                        display: false
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });
}

function loadUsageChart(timeRange = 0) {
    usageChartSpinner.removeClass('d-none');
    usageChartCanvas.addClass('d-none');

    // Call the REAL API function now
    FetchUsageSummaryFromAPI(timeRange,
        (data) => {
            usageChartTitle.text(data.chartTitle);
            usageChartInstance.data.labels = data.labels;
            usageChartInstance.data.datasets[0].data = data.data;
            usageChartInstance.update();

            usageChartSpinner.addClass('d-none');
            usageChartCanvas.removeClass('d-none');
        },
        (error) => {
            console.error("Failed to load chart data", error);
            usageChartSpinner.addClass('d-none');
            usageChartCanvas.parent().html('<p class="text-center text-danger">Could not load chart data.</p>');
        }
    );
}


// --- Table & Pagination Functions ---

function renderUsageHistoryTable(items) {
    usageHistoryTableBody.empty();
    if (!items || items.length === 0) {
        usageHistoryTableBody.append(`
            <tr>
                <td colspan="5" class="text-center p-4 text-muted">No usage history found for this period.</td>
            </tr>
        `);
        return;
    }

    items.forEach(item => {
        const rowHtml = `
            <tr>
                <td>${formatDate(item.timestamp)}</td>
                <td>${item.businessName}</td>
                <td>${Number(item.minutesUsed).toFixed(2)} min</td>
                <td>${formatCurrency(item.totalCost)}</td>
                <td><code>${item.conversationSessionId}</code></td>
            </tr>
        `;
        usageHistoryTableBody.append(rowHtml);
    });
}

function updatePaginationButtons(hasNext, hasPrev) {
    usagePagination.nextBtn.prop('disabled', !hasNext);
    usagePagination.prevBtn.prop('disabled', !hasPrev);
}

function showTableLoading(isLoading) {
    if (isLoading) {
        usagePagination.controls.addClass('d-none');
        usageHistoryTableBody.html(`
            <tr class="loading-row">
                <td colspan="5" class="text-center p-4">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Loading...</span>
                    </div>
                </td>
            </tr>
        `);
    } else {
        usagePagination.controls.removeClass('d-none');
    }
}

function loadUsageHistory(cursor = null, direction = 'next') {
    if (isUsageLoading) return;
    isUsageLoading = true;
    showTableLoading(true);

    let targetPage = currentUsagePage;
    const pageBeforeRequest = currentUsagePage; // Store current page in case of error

    // Determine the intended target page number for optimistic UI update
    if (direction === 'next') {
        if (currentUsageNextCursor || !cursor) {
            targetPage++;
        }
    } else { // 'prev'
        if (currentUsagePrevCursor && targetPage > 1) {
            targetPage--;
        }
    }

    usagePagination.pageInfo.text(`Page ${targetPage}`);

    // Call the REAL API function now
    FetchUsageHistoryFromAPI(USAGE_PAGE_SIZE, direction === 'next' ? cursor : null, direction === 'prev' ? cursor : null,
        (data) => {
            currentUsageNextCursor = data.nextCursor;
            currentUsagePrevCursor = data.previousCursor;

            // The final page number is the one we intended to go to.
            // If we moved back to the first page, reset it.
            currentUsagePage = data.hasPreviousPage ? targetPage : 1;
            usagePagination.pageInfo.text(`Page ${currentUsagePage}`);

            renderUsageHistoryTable(data.items);
            updatePaginationButtons(data.hasNextPage, data.hasPreviousPage);

            isUsageLoading = false;
            showTableLoading(false);
        },
        (error) => {
            console.error("Failed to load usage history", error);
            // Revert page number on error
            currentUsagePage = pageBeforeRequest;
            usagePagination.pageInfo.text(`Page ${currentUsagePage}`);
            usageHistoryTableBody.html(`
                <tr>
                    <td colspan="5" class="text-center p-4 text-danger">Failed to load usage history.</td>
                </tr>
            `);

            isUsageLoading = false;
            showTableLoading(false);
        }
    );
}


// --- Event Handlers ---

function bindUsageTabEventHandlers() {
    // Chart time range change
    timeRangeButtons.on('change', function () {
        const selectedTimeRange = parseInt($(this).val()); // Ensure it's a number
        loadUsageChart(selectedTimeRange);
    });

    // Pagination
    usagePagination.nextBtn.on('click', () => {
        if (!usagePagination.nextBtn.prop('disabled')) {
            loadUsageHistory(currentUsageNextCursor, 'next');
        }
    });

    usagePagination.prevBtn.on('click', () => {
        if (!usagePagination.prevBtn.prop('disabled')) {
            loadUsageHistory(currentUsagePrevCursor, 'prev');
        }
    });
}


/** INIT **/
function InitUsageTab() {
    console.log("Initializing Usage Tab...");

    // Only init if the tab is visible/part of the page
    if (usageTab.length === 0) {
        console.log("Usage Tab not found, skipping initialization.");
        return;
    }

    // Check if Chart.js is loaded
    if (typeof Chart === 'undefined') {
        console.error("Chart.js is not loaded. Cannot initialize Usage Tab chart.");
        usageChartCanvas.parent().html('<p class="text-center text-danger">Error: Chart library failed to load.</p>');
        return;
    }

    initializeUsageChart();
    loadUsageChart(); // Load default (monthly) view
    loadUsageHistory(); // Load first page of history
    bindUsageTabEventHandlers();

    console.log("Usage Tab Initialized successfully.");
}