// General Site-wide JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // Auto-hide Bootstrap alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            // Ensure the alert still exists before trying to close it
            if (alert.parentNode) {
                const bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            }
        }, 5000);
    });

    // Sub-task toggling functionality
    document.body.addEventListener('click', function(e) {
        if (e.target.matches('.toggle-subtasks, .toggle-subtasks *')) {
            const button = e.target.closest('.toggle-subtasks');
            const taskId = button.getAttribute('data-task-id');
            const subTasks = document.getElementById(`subtasks-${taskId}`);
            const icon = button.querySelector('i');

            if (subTasks.style.display === 'none' || subTasks.style.display === '') {
                subTasks.style.display = 'block';
                icon.classList.remove('fa-chevron-right');
                icon.classList.add('fa-chevron-down');
                button.title = 'Hide sub-tasks';
            } else {
                subTasks.style.display = 'none';
                icon.classList.remove('fa-chevron-down');
                icon.classList.add('fa-chevron-right');
                button.title = 'Show sub-tasks';
            }
        }
    });

    // Due date validation - prevent past dates
    const dueDateInput = document.querySelector('input[type="date"]');
    if (dueDateInput) {
        dueDateInput.addEventListener('change', function () {
            const selectedDate = new Date(this.value);
            const today = new Date();
            today.setHours(0, 0, 0, 0); // Reset time to compare dates only

            if (selectedDate < today) {
                alert('The selected date cannot be in the past!');
                this.value = ''; // Clear the invalid date
            }
        });
    }

    // User assignment selection logic on the task creation form
    const assignmentSpecific = document.getElementById('assignmentSpecific');
    const assignmentAll = document.getElementById('assignmentAll');
    const userSelection = document.getElementById('userSelection');

    function toggleUserSelection() {
        if (!userSelection) return;

        if (assignmentAll && assignmentAll.checked) {
            userSelection.style.display = 'none';
            // Clear selected users
            const select = userSelection.querySelector('select');
            if(select) {
                Array.from(select.options).forEach(option => option.selected = false);
            }
        } else {
            userSelection.style.display = 'block';
        }
    }

    if (assignmentSpecific && assignmentAll) {
        assignmentSpecific.addEventListener('change', toggleUserSelection);
        assignmentAll.addEventListener('change', toggleUserSelection);
        // Initialize state on page load
        toggleUserSelection();
    }
});

// Utility function to show a loading spinner
function showLoading() {
    const loader = document.createElement('div');
    loader.className = 'loading-overlay';
    loader.innerHTML = `
        <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
        </div>
    `;
    document.body.appendChild(loader);
}

// Utility function to hide the loading spinner
function hideLoading() {
    const loader = document.querySelector('.loading-overlay');
    if (loader) {
        loader.remove();
    }
}