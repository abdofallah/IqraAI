$(document).ready(() => {

    const analyticsChart = document.getElementById('analyticsChart');

    new Chart(analyticsChart, {
        type: 'bar',
        data: {
            datasets: [
                {
                    label: 'Orders Placed',
                    data: [7, 24, 5, 23, 12, 1, 5],
                    borderWidth: 1,
                    backgroundColor: 'red',
                    barThickness: '20'
                },
                {
                    label: 'Appointments Booked',
                    data: [4, 19, 3, 32, 20, 17, 12],
                    borderWidth: 1,
                    backgroundColor: 'lightblue',
                    barThickness: '20'
                },
                {
                    label: 'Total Calls',
                    data: [10, 32, 11, 42, 33, 25, 23],
                    borderWidth: 1,
                    backgroundColor: 'green',
                    barThickness: '20',
                    borderRadius: {
                        topRight: '6',
                        topLeft: '6'
                    }
                }
            ],
            labels: ['Jan 1', 'Jan 2', 'Jan 3', 'Jan 4', 'Jan 5', 'Jan 6', 'Jan 7'],
        },
        options: {
            scales: {
                y: {
                    beginAtZero: true,
                    stacked: true,
                },
                x: {
                    stacked: true,
                }
            },
            responsive: true,
            maintainAspectRatio: false
        }
    });

});