/*
================================================================================
|                                                                              |
|                             CUSTOM JAVASCRIPT                              |
|                                                                              |
================================================================================
*/

// -------------------- TOAST NOTIFICATIONS --------------------
// Globally available for triggering from Razor views
function showToast(message, type = 'success') {
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        // Create container if it doesn't exist
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        document.body.appendChild(toastContainer);
    }

    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.textContent = message;

    toastContainer.appendChild(toast);

    // Animate in
    setTimeout(() => {
        toast.classList.add('show');
    }, 100);

    // Animate out and remove
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 500);
    }, 3000);
}

document.addEventListener('DOMContentLoaded', function () {

    // -------------------- SMOOTH PAGE TRANSITIONS --------------------
    const handleLinkClick = (e) => {
        const link = e.target.closest('a');
        // Ensure it's a valid, local link, not a bootstrap toggle, and not a link designed to open in a new tab.
        if (link && link.href && link.href.startsWith(window.location.origin) && !link.getAttribute('data-bs-toggle') && link.target !== '_blank') {
            e.preventDefault();
            document.body.classList.add('fade-out');
            setTimeout(() => {
                window.location.href = link.href;
            }, 300); // Shorter duration for snappier feel
        }
    };

    document.body.addEventListener('click', handleLinkClick);

    // Fades in the new page
    window.addEventListener('pageshow', (event) => {
        // The 'persisted' property is true if the page is from the cache (e.g., back button)
        if (event.persisted) {
            document.body.classList.remove('fade-out');
        }
    });

    // -------------------- LOADING ANIMATIONS --------------------
    const mainContent = document.querySelector('.main-content');
    if (mainContent) {
        mainContent.style.opacity = 0;
        setTimeout(() => {
            mainContent.style.transition = 'opacity 0.5s';
            mainContent.style.opacity = 1;
        }, 50); // Faster initial load
    }
});

// Add fade-in effect on initial load
window.addEventListener('load', () => {
    document.body.classList.add('fade-in');
});