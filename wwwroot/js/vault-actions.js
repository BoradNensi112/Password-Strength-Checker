/**
 * SecurePass Vault Interactive Actions (Reveal, Copy, Star, Duplicate Detection, SweetAlerts)
 */
const SecurePassVault = {
    async revealPassword(id, buttonEl, targetPasswordEl, token) {
        try {
            // If already revealed -> immediate toggle back to masked!
            if (targetPasswordEl.classList.contains('password-revealed')) {
                targetPasswordEl.innerText = '••••••••••••';
                targetPasswordEl.classList.remove('password-revealed');
                targetPasswordEl.classList.add('password-masked');
                targetPasswordEl.style.color = '';
                targetPasswordEl.style.fontWeight = '';
                buttonEl.innerHTML = '<i class="fa-solid fa-eye"></i>';
                return;
            }

            // If already fetched and cached in dataset -> reveal instantly
            if (targetPasswordEl.dataset.realpass) {
                targetPasswordEl.innerText = targetPasswordEl.dataset.realpass;
                targetPasswordEl.classList.remove('password-masked');
                targetPasswordEl.classList.add('password-revealed');
                targetPasswordEl.style.color = '#22C55E';
                targetPasswordEl.style.fontWeight = 'bold';
                buttonEl.innerHTML = '<i class="fa-solid fa-eye-slash"></i>';
                return;
            }

            buttonEl.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i>';
            buttonEl.disabled = true;

            const res = await fetch('/Vault/RevealPassword?qid=' + encodeURIComponent(id), {
                method: 'POST'
            });

            const data = await res.json();
            if (data.success) {
                targetPasswordEl.dataset.realpass = data.password;
                targetPasswordEl.innerText = data.password;
                targetPasswordEl.classList.remove('password-masked');
                targetPasswordEl.classList.add('password-revealed');
                targetPasswordEl.style.color = '#22C55E';
                targetPasswordEl.style.fontWeight = 'bold';
                buttonEl.innerHTML = '<i class="fa-solid fa-eye-slash"></i>';
                buttonEl.disabled = false;

                // Auto hide after 15 seconds for maximum security
                setTimeout(function() {
                    if (targetPasswordEl.classList.contains('password-revealed')) {
                        targetPasswordEl.innerText = '••••••••••••';
                        targetPasswordEl.classList.remove('password-revealed');
                        targetPasswordEl.classList.add('password-masked');
                        targetPasswordEl.style.color = '';
                        targetPasswordEl.style.fontWeight = '';
                        buttonEl.innerHTML = '<i class="fa-solid fa-eye"></i>';
                    }
                }, 15000);
            } else {
                Swal.fire({
                    icon: 'error',
                    title: 'Security Notice',
                    text: data.message || 'Could not decrypt password.',
                    background: '#0B1224',
                    color: '#F8FAFC',
                    confirmButtonColor: '#6D5DF6'
                });
                buttonEl.innerHTML = '<i class="fa-solid fa-eye"></i>';
                buttonEl.disabled = false;
            }
        } catch (e) {
            console.error(e);
            buttonEl.innerHTML = '<i class="fa-solid fa-eye"></i>';
            buttonEl.disabled = false;
        }
    },

    async copyToClipboard(text, label) {
        label = label || 'Password';
        if (!text || text === '••••••••••••' || text === '••••••••') {
            Swal.fire({
                icon: 'info',
                title: 'Reveal First',
                text: 'Please click the Reveal (eye) button first to copy plain password.',
                background: '#0B1224',
                color: '#F8FAFC',
                confirmButtonColor: '#6D5DF6',
                timer: 2500
            });
            return;
        }

        try {
            await navigator.clipboard.writeText(text);
            const Toast = Swal.mixin({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 2000,
                timerProgressBar: true,
                background: '#0B1224',
                color: '#22C55E'
            });
            Toast.fire({
                icon: 'success',
                title: label + ' copied to clipboard!'
            });
        } catch (err) {
            console.error('Failed to copy', err);
        }
    },

    async toggleFavorite(id, starEl, token) {
        try {
            const res = await fetch('/Vault/ToggleFavorite?qid=' + encodeURIComponent(id), {
                method: 'POST'
            });

            const data = await res.json();
            if (data.success) {
                if (data.isFavorite) {
                    starEl.classList.remove('fa-regular');
                    starEl.classList.add('fa-solid');
                    starEl.style.color = '#F59E0B';
                } else {
                    starEl.classList.remove('fa-solid');
                    starEl.classList.add('fa-regular');
                    starEl.style.color = 'var(--text-muted)';
                }
            }
        } catch (e) {
            console.error(e);
        }
    },

    confirmDelete(formId, title) {
        Swal.fire({
            title: 'Delete Password?',
            text: 'Are you sure you want to permanently remove credentials for "' + title + '"? This cannot be undone.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#EF4444',
            cancelButtonColor: '#64748B',
            confirmButtonText: 'Yes, Delete Permanently',
            cancelButtonText: 'Cancel',
            background: '#0B1224',
            color: '#F8FAFC'
        }).then(function(result) {
            if (result.isConfirmed) {
                document.getElementById(formId).submit();
            }
        });
    },

    bindDuplicateChecker(passwordInputSelector, warningContainerSelector, currentId) {
        currentId = currentId || null;
        const input = document.querySelector(passwordInputSelector);
        const warningBox = document.querySelector(warningContainerSelector);
        if (!input || !warningBox) return;

        let debounceTimer;
        input.addEventListener('input', function() {
            clearTimeout(debounceTimer);
            const password = input.value;
            if (!password || password.length < 4) {
                warningBox.style.display = 'none';
                return;
            }

            debounceTimer = setTimeout(async function() {
                try {
                    const res = await fetch('/Vault/CheckDuplicate', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ password: password, currentId: currentId })
                    });
                    const data = await res.json();
                    if (data.isDuplicate) {
                        warningBox.innerHTML = '<i class="fa-solid fa-triangle-exclamation text-warning me-2"></i> <strong>Warning:</strong> ' + data.message;
                        warningBox.style.display = 'block';
                    } else {
                        warningBox.style.display = 'none';
                    }
                } catch (e) {
                    console.error(e);
                }
            }, 300);
        });
    }
};
