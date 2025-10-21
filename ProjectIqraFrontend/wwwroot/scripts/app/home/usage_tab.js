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
const usageOverallCostText = usageTab.find("#usageOverallCostText");
const usageTotalCallText = usageTab.find("#usageTotalCallText");
const usageTotalCallDurationText = usageTab.find("#usageTotalCallDurationText");
const usageAverageCallDurationText = usageTab.find("#usageAverageCallDurationText");
const usageAverageCallCostText = usageTab.find("#usageAverageCallCostText");
const usageTotalCallCostText = usageTab.find("#usageTotalCallCostText");

// Duration Chart
const usageChartCanvas = usageTab.find("#usageChart");
const usageChartSpinner = usageTab.find("#usageChartSpinner");

// Calls Chart
const usageCallsChartCanvas = usageTab.find("#usageCallsChart");
const usageCallsChartSpinner = usageTab.find("#usageCallsChartSpinner");

// Cost Chart
const usageOverallCostChartCanvas = usageTab.find("#usageOverallCostChart");
const usageOverallCostChartSpinner = usageTab.find("#usageOverallCostChartSpinner");

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
    usageOverallCostChartSpinner.removeClass('d-none');
    usageOverallCostChartCanvas.addClass('d-none');

    // Reset summary cards to loading state
    updateSummaryCard(usageOverallCostText);
    updateSummaryCard(usageTotalCallText);
    updateSummaryCard(usageTotalCallDurationText);
    updateSummaryCard(usageAverageCallDurationText);
    updateSummaryCard(usageTotalCallCostText);
    updateSummaryCard(usageAverageCallCostText);

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
            // 4. Update Summary Cards using the new data model

            // Get key metrics, providing defaults to avoid errors if data is missing
            const totalCalls = data.bySource?.conversation?.totalCount ?? 0;
            const callMinutesFeature = data.byFeature['Call_Minutes'];
            const totalCallDuration = callMinutesFeature?.totalQuantity ?? 0;

            const callVoicemailDetectionFeature = data.byFeature['Call_VoicemailDetection'];

            const totalCallCost = (callMinutesFeature?.totalCost ?? 0) + (callVoicemailDetectionFeature?.totalCost ?? 0);

            updateSummaryCard(usageOverallCostText, data.totalCost, 'currency');
            updateSummaryCard(usageTotalCallText, totalCalls, 'number');
            updateSummaryCard(usageTotalCallDurationText, totalCallDuration, 'minutes');

            const avgDuration = totalCalls > 0 ? totalCallDuration / totalCalls : 0;
            updateSummaryCard(usageAverageCallDurationText, avgDuration, 'minutes');

            const avgCost = totalCalls > 0 ? totalCallCost / totalCalls : 0;
            updateSummaryCard(usageAverageCallCostText, avgCost, 'currency');

            updateSummaryCard(usageTotalCallCostText, totalCallCost, 'currency');

            // 5. Update all charts using the 'data.charts' dictionary
            if (data.charts?.overallCostChart) {
                updateStackedChart(usageCostChartInstance, data.charts.overallCostChart);
            }

            if (data.charts?.durationChart) {
                usageChartCanvas.parent().parent().parent().removeClass('d-none');
                updateStackedChart(usageChartInstance, data.charts.durationChart);
            } else {
                usageChartCanvas.parent().parent().parent().addClass('d-none');
            }

            if (data.charts?.callCountChart) {
                usageCallsChartCanvas.parent().parent().parent().removeClass('d-none');
                updateStackedChart(usageCallsChartInstance, data.charts.callCountChart);
            } else {
                usageCallsChartCanvas.parent().parent().parent().addClass('d-none');
            }

            // --- END OF NEW LOGIC ---

            // 6. Hide loading state (this part remains the same)
            usageChartSpinner.addClass('d-none');
            usageChartCanvas.removeClass('d-none');
            usageCallsChartSpinner.addClass('d-none');
            usageCallsChartCanvas.removeClass('d-none');
            usageOverallCostChartSpinner.addClass('d-none');
            usageOverallCostChartCanvas.removeClass('d-none');
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
    const usageHistoryTableBody = $('#usageHistoryTable tbody');
    usageHistoryTableBody.empty();

    if (!items || items.length === 0) {
        usageHistoryTableBody.append(`
            <tr>
                <td colspan="4" class="text-center p-4 text-muted">No usage history found.</td>
            </tr>
        `);
        return;
    }

    items.forEach(item => {
        // --- 1. Find Business Name (re-using your existing logic) ---
        let businessName = `${item.businessId} | Unknown`;
        const businessData = CurrentBusinessesList.find(b => b.id == item.businessId);
        if (businessData) {
            businessName = `${item.businessId} | ${businessData.name}`;
        }

        // --- 2. Build the Consumption Column HTML ---
        // This is the core new logic. We loop through each consumed feature.
        let consumptionHtml = '';
        if (item.consumedFeatures && item.consumedFeatures.length > 0) {
            item.consumedFeatures.forEach(feature => {
                // Find the feature's display name from the global plan data
                const featureInfo = UserPlanData.features.find(f => f.key === feature.featureKey);
                const featureDisplayName = featureInfo ? featureInfo.displayName : feature.featureKey;

                // Get a styled badge for the consumption type (Included, Overage, etc.)
                const typeBadge = getConsumptionTypeBadge(feature.type);

                // Format the quantity. We use 'appliedUnitUsage' as it represents the plan consumption.
                const formattedQuantity = Number(feature.quantity).toLocaleString(undefined, {
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 4
                });

                // Create a styled block for this single feature consumption
                consumptionHtml += `
                    <div class="d-flex justify-content-between align-items-center p-2 mb-1 rounded" style="background-color: #111111;">
                        <span>
                            <strong>${featureDisplayName}</strong><br>
                            - Quantity: ${formattedQuantity}/${featureInfo.unitPlural}
                            ${(feature.type == "PayAsYouGo" || feature.consumedType == "Overage") ? `<br>- Usage Cost: ${formatUsageTabCurrency(feature.totalUsage)}` : ''}
                        </span>
                        ${typeBadge}
                    </div>
                `;
            });
        } else {
            consumptionHtml = '<span class="text-muted">No features consumed.</span>';
        }

        // --- 3. Assemble the Final Table Row ---
        const rowHtml = `
            <tr>
                <td>${formatUsageTabDate(item.timestamp)}</td>
                <td>${businessName}</td>
                <td>${item.sourceType}</td>
                <td>${consumptionHtml}</td>
            </tr>
        `;
        usageHistoryTableBody.append(rowHtml);
    });
}

function getConsumptionTypeBadge(consumptionType) {
    switch (consumptionType) {
        case 'Included':
            return '<span class="badge bg-success">Included</span>';
        case 'Overage':
            return '<span class="badge bg-danger">Overage</span>';
        case 'PayAsYouGo':
            return '<span class="badge bg-primary">Pay-as-you-go</span>';
        case 'Unknown':
        default:
            return '<span class="badge bg-secondary">Unknown</span>';
    }
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

function updateSummaryCard(element, value, format = 'number') {
    let formattedValue = '...';
    if (value !== undefined && value !== null) {
        switch (format) {
            case 'currency':
                formattedValue = formatUsageTabCurrency(value);
                break;
            case 'minutes':
                formattedValue = `${Number(value).toFixed(2)} min`;
                break;
            case 'seconds':
                formattedValue = `${Number(value).toFixed(1)} sec`;
                break;
            case 'number':
            default:
                formattedValue = Number(value).toLocaleString();
                break;
        }
    }
    element.text(formattedValue);
}

function formatUsageTabCurrency(value, decimals = 4) {
    return `$${Number(value).toFixed(decimals)}`;
}

function formatUsageTabDate(dateString) {
    const options = { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' };
    return new Date(dateString).toLocaleDateString(undefined, options);
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
    usageChartInstance = createUsageChart(usageChartCanvas, true, (value) => {
        return `${Number(value).toFixed(3)} min`;
    });

    usageCallsChartInstance = createUsageChart(usageCallsChartCanvas, true, (value) => {
        return Number.isInteger(value) ? value : '';
    });

    usageCostChartInstance = createUsageChart(usageOverallCostChartCanvas, true, (value) => {
        return formatUsageTabCurrency(value);
    });

    initializeUsageControls();

    bindUsageTabEventHandlers();

    loadUsageOverview();
    loadUsageHistory();
}