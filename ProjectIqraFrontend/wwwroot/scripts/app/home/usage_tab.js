// Global Variables for Usage Tab
let usageChartInstance = null;
let usageCallsChartInstance = null;
let usageCostChartInstance = null;

let currentUsagePage = 1;
let currentUsageNextCursor = null;
let currentUsagePrevCursor = null;
let isUsageLoading = false;
const USAGE_PAGE_SIZE = 10;

const PLAN_MODELS = {
    StandardPayAsYouGo: 0,
    VolumeBasedTiered: 1,
    FixedPricePackage: 2
};

/** Element Variables **/
const usageTab = $("#usage-tab");

// New Controls
const usageDateRangePicker = usageTab.find("#usageDateRangePicker");
const usageGroupBySelect = usageTab.find("#usageGroupBySelect");

// Summary Cards
const usageTotalCallText = usageTab.find("#usageTotalCallText");
const usageTotalCallDurationText = usageTab.find("#usageTotalCallDurationText");
const usageAverageCallDurationText = usageTab.find("#usageAverageCallDurationText");
const usageAverageCallCostText = usageTab.find("#usageAverageCallCostText");
const usageTotalCostText = usageTab.find("#usageTotalCostText");

// Duration Chart
const usageChartCanvas = usageTab.find("#usageChart");
const usageChartSpinner = usageTab.find("#usageChartSpinner");

// Calls Chart
const usageCallsChartCanvas = usageTab.find("#usageCallsChart");
const usageCallsChartSpinner = usageTab.find("#usageCallsChartSpinner");

// Cost Chart
const usageCostChartCanvas = usageTab.find("#usageCostChart");
const usageCostChartSpinner = usageTab.find("#usageCostChartSpinner");

// History Table
const usageHistoryTableBody = usageTab.find("#usageHistoryTable tbody");
const usagePagination = {
    controls: usageTab.find("#usagePaginationControls"),
    prevBtn: usageTab.find("#usagePrevButton"),
    nextBtn: usageTab.find("#usageNextButton"),
    pageInfo: usageTab.find("#usagePageInfo")
};

// API Functions
function FetchUsageSummaryFromAPI(startDate, endDate, groupBy, successCallback, errorCallback) {
    $.ajax({
        url: '/app/user/usage/summary',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            startDate: startDate,
            endDate: endDate,
            groupBy: groupBy
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


// Chart Functions
function getRandomColor(seed) {
    const r = Math.floor((Math.abs(Math.sin(seed * 10)) * 255) % 180); // Keep it less saturated
    const g = Math.floor((Math.abs(Math.sin(seed * 11)) * 255) % 180);
    const b = Math.floor((Math.abs(Math.sin(seed * 12)) * 255) % 180);
    return `rgb(${r}, ${g}, ${b})`;
}

function createUsageChart(canvas, isStacked, yAxisCallback) {
    const ctx = canvas[0].getContext('2d');
    return new Chart(ctx, {
        type: 'bar',
        data: { labels: [], datasets: [] },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index' },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: yAxisCallback
                    },
                    stacked: isStacked
                },
                x: {
                    grid: {
                        display: false
                    },
                    stacked: isStacked
                }
            },
            plugins: {
                legend: {
                    position: 'top'
                },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            const formattedValue = yAxisCallback(context.parsed.y);
                            return ` ${context.dataset.label}: ${formattedValue}`;
                        },
                        footer: function (tooltipItems) {
                            let total = tooltipItems.reduce((sum, item) => sum + item.parsed.y, 0);

                            const formattedTotal = yAxisCallback(total);

                            return `\nTotal: ${formattedTotal}`;
                        }
                    }
                }
            }
        }
    });
}

function updateStackedChart(chartInstance, chartData) {
    chartInstance.data.labels = chartData.labels;

    chartInstance.data.datasets = chartData.datasets.map((ds, index) => {
        let businessName = `Unknown (ID: ${ds.businessId})`;
        const business = CurrentBusinessesList.find(b => b.id == ds.businessId);
        if (business) {
            businessName = business.name;
        }

        return {
            label: businessName,
            data: ds.data,
            backgroundColor: getRandomColor(index + 1),
            borderColor: '#ffffff',
            borderWidth: 1
        };
    });

    chartInstance.update();
}

function loadUsageOverview() {
    // 1. Show loading state
    usageChartSpinner.removeClass('d-none');
    usageChartCanvas.addClass('d-none');
    usageCallsChartSpinner.removeClass('d-none');
    usageCallsChartCanvas.addClass('d-none');
    usageCostChartSpinner.removeClass('d-none');
    usageCostChartCanvas.addClass('d-none');
    usageTotalCallText.text("...");
    usageTotalCallDurationText.text("...");
    usageAverageCallDurationText.text("...");
    usageAverageCallCostText.text("...");
    usageTotalCostText.text("...");

    // 2. Get parameters from controls
    const picker = usageDateRangePicker.data('daterangepicker');

    var startDate = picker.startDate.toISOString();
    const startDateUTCOffset = picker.startDate.utcOffset();
    if (startDateUTCOffset != 0) {
        startDate = moment.utc(startDate).add(startDateUTCOffset, 'minutes');
    }

    var endDate = picker.endDate.toISOString();
    const endDateUTCOffset = picker.endDate.utcOffset();
    if (endDateUTCOffset != 0) {
        endDate = moment.utc(endDate).add(endDateUTCOffset, 'minutes');
    }

    const groupBy = parseInt(usageGroupBySelect.val());

    // 3. Call the updated API function
    FetchUsageSummaryFromAPI(startDate, endDate, groupBy,
        (data) => {
            // Update Summary Cards
            usageTotalCallText.text(data.totalCalls.toLocaleString());
            usageTotalCallDurationText.text(`${data.totalDurationMinutes.toFixed(2)} min`);
            usageAverageCallDurationText.text(`${data.averageDurationSeconds.toFixed(1)} sec`);
            usageAverageCallCostText.text(formatCurrency(data.averageCallCost));
            usageTotalCostText.text(formatCurrency(data.totalCost));

            // Update all charts
            updateStackedChart(usageChartInstance, data.durationChart);
            updateStackedChart(usageCallsChartInstance, data.callsChart);
            updateStackedChart(usageCostChartInstance, data.costChart);

            // Hide loading state
            usageChartSpinner.addClass('d-none');
            usageChartCanvas.removeClass('d-none');
            usageCallsChartSpinner.addClass('d-none');
            usageCallsChartCanvas.removeClass('d-none');
            usageCostChartSpinner.addClass('d-none');
            usageCostChartCanvas.removeClass('d-none');
        },
        (error) => {
            console.error("Failed to load usage overview data", error);
            usageChartSpinner.addClass('d-none');
            usageCallsChartSpinner.addClass('d-none');
            usageChartCanvas.parent().html('<p class="text-center text-danger">Could not load chart data.</p>');
            usageCallsChartCanvas.parent().html('<p class="text-center text-danger">Could not load chart data.</p>');
        }
    );
}


// Table & Pagination Functions

function renderUsageHistoryTable(items) {
    usageHistoryTableBody.empty();
    if (!items || items.length === 0) {
        usageHistoryTableBody.append(`
            <tr>
                <td colspan="5" class="text-center p-4 text-muted">No usage history found.</td>
            </tr>
        `);
        return;
    }

    items.forEach(item => {
        let billingDetailsHtml = '';

        if (item.planModel.value === PLAN_MODELS.FixedPricePackage) {
            billingDetailsHtml = `
                Deducted: <strong>${Number(item.totalMinutesDeducted).toFixed(3)} min</strong>
            `;

            if (item.totalOverageCost > 0) {
                billingDetailsHtml += `
                    <br>
                    <small class="text-danger">Overage: ${formatCurrency(item.totalOverageCost)}</small>
                `;
            }
        } else {
            billingDetailsHtml = formatCurrency(item.totalCost);
        }

        var businessName = `Unknown (${item.businessId})`;
        var businessData = CurrentBusinessesList.find(b => b.id == item.businessId);
        if (businessData) {
            businessName = businessData.name;
        }

        const rowHtml = `
            <tr>
                <td>${formatDate(item.timestamp)}</td>
                <td>${businessName}</td>
                <td>${Number(item.minutesUsed).toFixed(3)} min</td>
                <td>${billingDetailsHtml}</td>
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


// Event Handlers
function initializeUsageControls() {
    const start = moment.utc().startOf('month');
    const end = moment.utc();

    const minDate = moment.utc('2025-01-01');
    const maxDate = moment.utc();

    function updatePickerText(start, end) {
        usageDateRangePicker.find('span').html(start.format('MMMM D, YYYY') + ' - ' + end.format('MMMM D, YYYY'));
    }

    usageDateRangePicker.daterangepicker({
        startDate: start,
        endDate: end,
        minDate: minDate,
        maxDate: maxDate,
        showDropdowns: true,
        alwaysShowCalendars: true,
        timeZone: "+04:00",
        opens:"auto",
        ranges: {
            'Today': [moment.utc(), moment.utc()],
            'Last 7 Days': [moment.utc().subtract(6, 'days'), moment.utc()],
            'This Month': [moment.utc().startOf('month'), moment.utc()],
            'Last 30 Days': [moment.utc().subtract(29, 'days'), moment.utc()],
            'Last Month': [moment.utc().subtract(1, 'month').startOf('month'), moment.utc().subtract(1, 'month').endOf('month')]
        }
    }, updatePickerText);

    updatePickerText(start, end); // Initial text
}

function bindUsageTabEventHandlers() {
    // Date Range Picker Change
    usageDateRangePicker.on('apply.daterangepicker', function (ev, picker) {
        const diffDays = picker.endDate.diff(picker.startDate, 'days');

        usageGroupBySelect.find('option').prop('disabled', true);

        if (diffDays === 0) {
            usageGroupBySelect.find('option[value="0"]').prop('disabled', false);
        }

        if (diffDays >= 0) {
            usageGroupBySelect.find('option[value="1"]').prop('disabled', false);
        }

        if (picker.startDate.year() !== picker.endDate.year() || picker.startDate.month() !== picker.endDate.month()) {
            usageGroupBySelect.find('option[value="2"]').prop('disabled', false);
        }

        if (diffDays === 0) {
            usageGroupBySelect.val('0');
        } else if (diffDays > 31) {
            usageGroupBySelect.val('1');
        } else {
            usageGroupBySelect.val('1');
        }

        if (usageGroupBySelect.find('option:selected').is(':disabled')) {
            usageGroupBySelect.val(usageGroupBySelect.find('option:not(:disabled)').first().val());
        }

        loadUsageOverview();
    });

    // Group By Dropdown Change
    usageGroupBySelect.on('change', function () {
        loadUsageOverview();
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

    if (typeof Chart === 'undefined' || typeof moment === 'undefined' || typeof $.fn.daterangepicker === 'undefined') {
        console.error("A required library (Chart.js, Moment.js, or Daterangepicker) is not loaded.");
        // Display a more comprehensive error message
        usageTab.find('.inner-container').html('<p class="text-center text-danger p-5">Error: A required library failed to load. The usage dashboard cannot be displayed.</p>');
        return;
    }

    usageChartInstance = createUsageChart(usageChartCanvas, true, (value) => {
        return `${Number(value).toFixed(3)} min`;
    });

    usageCallsChartInstance = createUsageChart(usageCallsChartCanvas, true, (value) => {
        return Number.isInteger(value) ? value : '';
    });

    usageCostChartInstance = createUsageChart(usageCostChartCanvas, true, (value) => {
        return formatCurrency(value);
    });

    initializeUsageControls();

    bindUsageTabEventHandlers();

    loadUsageOverview();
    loadUsageHistory();

    console.log("Usage Tab Initialized successfully.");
}