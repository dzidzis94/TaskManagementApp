// Task Management App JavaScript
document.addEventListener('DOMContentLoaded', function () {
    // Auto-hide alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });

    // Apakšuzdevumu sakļaušanas/izvēršanas funkcionalitāte
    const toggleButtons = document.querySelectorAll('.toggle-subtasks');
    toggleButtons.forEach(button => {
        button.addEventListener('click', function () {
            const taskId = this.getAttribute('data-task-id');
            const subTasks = document.getElementById(`subtasks-${taskId}`);
            const icon = this.querySelector('i');

            if (subTasks.style.display === 'none') {
                subTasks.style.display = 'block';
                icon.className = 'fas fa-chevron-down';
                this.title = 'Paslēpt apakšuzdevumus';
            } else {
                subTasks.style.display = 'none';
                icon.className = 'fas fa-chevron-right';
                this.title = 'Rādīt apakšuzdevumus';
            }
        });
    });

    // Task item hover effects
    const taskItems = document.querySelectorAll('.task-item');
    taskItems.forEach(item => {
        item.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-2px)';
        });

        item.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0)';
        });
    });

    // Due date validation
    const dueDateInput = document.querySelector('input[type="date"]');
    if (dueDateInput) {
        dueDateInput.addEventListener('change', function () {
            const selectedDate = new Date(this.value);
            const today = new Date();
            today.setHours(0, 0, 0, 0);

            if (selectedDate < today) {
                alert('Izvēlētais datums nevar būt pagātnē!');
                this.value = '';
            }
        });
    }

    // Form validation enhancement
    const forms = document.querySelectorAll('form');
    forms.forEach(form => {
        form.addEventListener('submit', function (e) {
            const requiredFields = this.querySelectorAll('[required]');
            let isValid = true;

            requiredFields.forEach(field => {
                if (!field.value.trim()) {
                    isValid = false;
                    field.classList.add('is-invalid');
                } else {
                    field.classList.remove('is-invalid');
                }
            });

            if (!isValid) {
                e.preventDefault();
                alert('Lūdzu aizpildiet visus obligātos laukus!');
            }
        });
    });
});

// Utility functions
function formatDate(dateString) {
    const options = { year: 'numeric', month: '2-digit', day: '2-digit' };
    return new Date(dateString).toLocaleDateString('lv-LV', options);
}

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

function hideLoading() {
    const loader = document.querySelector('.loading-overlay');
    if (loader) {
        loader.remove();
    }
}
// Lietotāja izvēles pārvaldīšana
document.addEventListener('DOMContentLoaded', function () {
    const assignmentSpecific = document.getElementById('assignmentSpecific');
    const assignmentAll = document.getElementById('assignmentAll');
    const userSelection = document.getElementById('userSelection');

    function toggleUserSelection() {
        if (assignmentAll.checked) {
            userSelection.style.display = 'none';
            // Notīra izvēlēto lietotāju
            document.getElementById('AssignedUserId').value = '';
        } else {
            userSelection.style.display = 'block';
        }
    }

    assignmentSpecific.addEventListener('change', toggleUserSelection);
    assignmentAll.addEventListener('change', toggleUserSelection);

    // Inicializē sākuma stāvokli
    toggleUserSelection();
});