/**
 * Cyber UI Interactivity (Sidebar toggle, theme toggle, and real-time inactivity Auto-Lock watchdog)
 */
document.addEventListener('DOMContentLoaded', function() {
    // 1. Sidebar toggle for mobile
    const toggleBtn = document.getElementById('sidebarToggleBtn');
    const sidebar = document.querySelector('.app-sidebar');
    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', function() {
            sidebar.classList.toggle('show');
        });
    }

    // 2. Theme Initialization & Toggle
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const currentTheme = localStorage.getItem('securepass_theme') || 'dark';
    document.documentElement.setAttribute('data-theme', currentTheme);
    updateThemeIcon(currentTheme);

    if (themeToggleBtn) {
        themeToggleBtn.addEventListener('click', function() {
            const cur = document.documentElement.getAttribute('data-theme') || 'dark';
            const next = cur === 'dark' ? 'light' : 'dark';
            document.documentElement.setAttribute('data-theme', next);
            localStorage.setItem('securepass_theme', next);
            updateThemeIcon(next);

            const Toast = Swal.mixin({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 1500,
                background: next === 'dark' ? '#0B1224' : '#FFFFFF',
                color: next === 'dark' ? '#F8FAFC' : '#0F172A'
            });
            Toast.fire({
                icon: 'info',
                title: 'Switched to ' + (next === 'dark' ? 'Cyber Dark' : 'Light') + ' Mode'
            });
        });
    }

    function updateThemeIcon(theme) {
        if (!themeToggleBtn) return;
        const icon = themeToggleBtn.querySelector('i');
        if (icon) {
            icon.className = theme === 'dark' ? 'fa-solid fa-sun' : 'fa-solid fa-moon';
        }
    }

    // 3. Vault Inactivity Auto-Lock Watchdog
    const autoLockMinutes = parseInt(localStorage.getItem('securepass_autolock') || '15');
    if (autoLockMinutes > 0) {
        let lastActivity = Date.now();
        const timeoutMs = autoLockMinutes * 60 * 1000;
        let warningTriggered = false;

        function resetActivity() {
            lastActivity = Date.now();
            warningTriggered = false;
        }

        ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll', 'click'].forEach(function(evt) {
            window.addEventListener(evt, resetActivity, { passive: true });
        });

        // Watchdog interval checks every 10 seconds
        setInterval(function() {
            const elapsed = Date.now() - lastActivity;
            
            // Warning 30 seconds before auto-lock
            if (elapsed >= (timeoutMs - 30000) && !warningTriggered && elapsed < timeoutMs) {
                warningTriggered = true;
                const Toast = Swal.mixin({
                    toast: true,
                    position: 'top-end',
                    showConfirmButton: false,
                    timer: 6000,
                    background: '#0B1224',
                    color: '#F59E0B'
                });
                Toast.fire({
                    icon: 'warning',
                    title: 'Vault Inactivity Notice',
                    text: 'Vault will auto-lock in 30 seconds due to inactivity.'
                });
            }

            // Lock timeout reached
            if (elapsed >= timeoutMs) {
                Swal.fire({
                    icon: 'info',
                    title: 'Vault Auto-Locked',
                    text: 'Your vault has been securely locked due to inactivity.',
                    background: '#0B1224',
                    color: '#F8FAFC',
                    confirmButtonColor: '#6D5DF6',
                    confirmButtonText: 'Unlock Vault'
                }).then(function() {
                    window.location.href = '/Account/Login';
                });
            }
        }, 10000);
    }
});
