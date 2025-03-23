const ctx = document.getElementById('chart1');
const ctx2 = document.getElementById('chart2');
const ctx3 = document.getElementById('chart3');
const ctx4 = document.getElementById('chart4');

var barColors = [
  "rgba(147,240,59,1.0)",
  "rgba(245,79,82,1.0)",
  "rgba(255,163,47,1.0)",
  "rgba(255,236,33,1.0)",
  "rgba((149,82,234,1.0)",

];

var keys = [];
var smsCount = [];

var escalationCount = [];

//get list of Keys the user has access too
for (var key in courseSmsCountLookup) {
    if (courseSmsCountLookup.hasOwnProperty(key)) {
        keys.push(key);
    }
}

//get # of SMS per course
for (var key in courseSmsCountLookup) {
    if (courseSmsCountLookup.hasOwnProperty(key)) {
        smsCount.push(courseSmsCountLookup[key]);
    }
}

//Get # of escalation per course
for (var key in courseEscalationCountLookup) {
    if (courseEscalationCountLookup.hasOwnProperty(key)) {
        escalationCount.push(courseEscalationCountLookup[key]);
    }
}

new Chart(ctx, {
    type: 'bar',
    data: {
    labels: keys,
    datasets: [{
        label: '# of messages per Course',
        data: smsCount,
        borderWidth: 1
    }]
    },
    options: {
        responsive: true
    }
});

new Chart(ctx2, {
    type: 'pie',
    data: {
    labels: keys,
    datasets: [{
        label: '# of Votes',
        data: escalationCount,
    }]
    },
    options: {
        responsive: true
    }
});

new Chart(ctx3, {
    type: 'doughnut',
    data: {
    labels: ['Red', 'Blue', 'Yellow', 'Green', 'Purple', 'Orange'],
    datasets: [{
        label: '# of Votes',
        data: [12, 19, 3, 5, 2, 3],
        borderWidth: 1
    }]
    },
    options: {
        responsive: true
    }
});

new Chart(ctx4, {
    type: 'line',
    data: {
    labels: ['Red', 'Blue', 'Yellow', 'Green', 'Purple', 'Orange'],
    datasets: [{
        label: '# of Votes',
        data: [12, 19, 3, 5, 2, 3],
        borderWidth: 1
    }]
    },
    options: {
        responsive: true
    }
});