// File: web/ECommerceSystem.Storefront/wwwroot/js/site.js
// Storefront UI behaviour. Patterns reused from the DGCS Enrollment System: Lucide icons, off-canvas sidebar,
// clickable rows/cards (data-href), card and table search (data-card-filter / data-table-filter),
// toasts (showToast) and the confirm modal (data-confirm).
// Storefront additions: quantity steppers, auto-submit quantity, test-card buttons, double-submit guard
// (data-busy-text) and the live order timeline (polls every 2 s until the order is Confirmed or Cancelled).
(function () {
    'use strict';

    function refreshIcons() {
        if (window.lucide) {
            window.lucide.createIcons();
        }
    }

    // ---------- Toasts: showToast('message', 'success' | 'error' | 'info') ----------
    var toastIcons = { success: 'circle-check', error: 'circle-x', info: 'info' };

    window.showToast = function (message, type) {
        var container = document.getElementById('toast-container');
        if (!container) {
            return;
        }

        var kind = toastIcons[type] ? type : 'info';
        var toast = document.createElement('div');
        toast.className = 'toast-item ' + kind;
        toast.setAttribute('role', 'status');

        var icon = document.createElement('i');
        icon.setAttribute('data-lucide', toastIcons[kind]);

        var text = document.createElement('span');
        text.textContent = message;

        toast.appendChild(icon);
        toast.appendChild(text);
        container.appendChild(toast);
        refreshIcons();

        setTimeout(function () {
            toast.style.transition = 'opacity .3s';
            toast.style.opacity = '0';
            setTimeout(function () { toast.remove(); }, 300);
        }, 3800);
    };

    // ---------- Off-canvas sidebar (tablets and phones) ----------
    function initSidebar() {
        var toggle = document.getElementById('navToggle');
        var sidebar = document.getElementById('sidebar');
        var backdrop = document.getElementById('navBackdrop');
        if (!toggle || !sidebar || !backdrop) {
            return;
        }

        function openNav() { sidebar.classList.add('open'); backdrop.classList.add('open'); }
        function closeNav() { sidebar.classList.remove('open'); backdrop.classList.remove('open'); }

        toggle.addEventListener('click', function () {
            if (sidebar.classList.contains('open')) { closeNav(); } else { openNav(); }
        });
        backdrop.addEventListener('click', closeNav);
        sidebar.querySelectorAll('a').forEach(function (a) { a.addEventListener('click', closeNav); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') { closeNav(); } });
        window.addEventListener('resize', function () { if (window.innerWidth > 1024) { closeNav(); } });
    }

    // ---------- Clickable cards and rows: data-href ----------
    document.addEventListener('click', function (e) {
        var el = e.target.closest('[data-href]');
        if (!el) {
            return;
        }
        if (e.target.closest('a, button, input, select, textarea, form, label')) {
            return;
        }
        window.location = el.getAttribute('data-href');
    });

    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter' || !e.target.closest) {
            return;
        }
        if (e.target.closest('a, button, input, select, textarea')) {
            return;
        }
        var el = e.target.closest('[data-href][tabindex]');
        if (el) {
            window.location = el.getAttribute('data-href');
        }
    });

    // ---------- Search: data-card-filter (cards) and data-table-filter (table rows) ----------
    function initFilters() {
        document.querySelectorAll('input[data-card-filter]').forEach(function (input) {
            input.addEventListener('input', function () {
                var grid = document.querySelector(input.getAttribute('data-card-filter'));
                if (!grid) {
                    return;
                }
                var query = input.value.trim().toLowerCase();
                var shown = 0;
                Array.prototype.forEach.call(grid.children, function (card) {
                    var text = (card.getAttribute('data-search') || card.textContent).toLowerCase();
                    var match = text.indexOf(query) !== -1;
                    card.style.display = match ? '' : 'none';
                    if (match) { shown++; }
                });
                var emptyTarget = input.getAttribute('data-empty-target');
                var empty = emptyTarget ? document.querySelector(emptyTarget) : null;
                if (empty) {
                    empty.hidden = shown !== 0;
                }
            });
        });

        document.querySelectorAll('input[data-table-filter]').forEach(function (input) {
            input.addEventListener('input', function () {
                var table = document.querySelector(input.getAttribute('data-table-filter'));
                if (!table) {
                    return;
                }
                var query = input.value.trim().toLowerCase();
                table.querySelectorAll('tbody tr').forEach(function (row) {
                    row.style.display = row.textContent.toLowerCase().indexOf(query) !== -1 ? '' : 'none';
                });
            });
        });
    }

    // ---------- Confirm modal: <form data-confirm="message"> ----------
    function initConfirm() {
        var modal = document.getElementById('confirmModal');
        if (!modal) {
            return;
        }
        var messageEl = document.getElementById('confirmMessage');
        var okButton = document.getElementById('confirmOk');
        var cancelButton = document.getElementById('confirmCancel');
        var pendingForm = null;

        function closeModal() {
            modal.classList.remove('open');
            pendingForm = null;
        }

        cancelButton.addEventListener('click', closeModal);
        modal.addEventListener('click', function (e) { if (e.target === modal) { closeModal(); } });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') { closeModal(); } });

        okButton.addEventListener('click', function () {
            if (!pendingForm) {
                return;
            }
            var form = pendingForm;
            closeModal();
            form.dataset.confirmed = '1';
            form.submit();
        });

        document.addEventListener('submit', function (e) {
            var form = e.target.closest('form[data-confirm]');
            if (!form || form.dataset.confirmed === '1') {
                return;
            }
            e.preventDefault();
            messageEl.textContent = form.getAttribute('data-confirm');
            pendingForm = form;
            modal.classList.add('open');
            okButton.focus();
        });
    }

    // ---------- Double-submit guard: the button with data-busy-text disables itself ----------
    function initBusyGuard() {
        document.addEventListener('submit', function (e) {
            if (e.defaultPrevented) {
                return;
            }
            var form = e.target;
            var button = form.querySelector('[data-busy-text]');
            if (!button) {
                return;
            }
            if (form.dataset.busy === '1') {
                e.preventDefault();
                return;
            }
            form.dataset.busy = '1';
            button.disabled = true;
            button.innerHTML = '<span class="spinner"></span> ' + button.getAttribute('data-busy-text');
        });

        // Coming back with the browser's Back button must not leave the button disabled.
        window.addEventListener('pageshow', function (e) {
            if (e.persisted) {
                window.location.reload();
            }
        });
    }

    // ---------- Quantity steppers and auto-submit ----------
    function initQuantity() {
        document.querySelectorAll('[data-qty-step]').forEach(function (button) {
            button.addEventListener('click', function () {
                var input = button.parentElement.querySelector('input[type="number"]');
                if (!input) {
                    return;
                }
                var step = parseInt(button.getAttribute('data-qty-step'), 10);
                var min = parseInt(input.min || '1', 10);
                var max = parseInt(input.max || '100', 10);
                var value = (parseInt(input.value, 10) || min) + step;
                input.value = Math.min(max, Math.max(min, value));
            });
        });

        document.querySelectorAll('input[data-autosubmit]').forEach(function (input) {
            input.addEventListener('change', function () {
                if (input.form) {
                    input.form.submit();
                }
            });
        });
    }

    // ---------- Test-card buttons on the checkout page ----------
    function initTestCards() {
        document.querySelectorAll('[data-fill-card]').forEach(function (button) {
            button.addEventListener('click', function () {
                var input = document.querySelector('input[name="Form.CardNumber"]');
                if (!input) {
                    return;
                }
                input.value = button.getAttribute('data-fill-card');
                input.dispatchEvent(new Event('input', { bubbles: true }));
                if (window.jQuery && window.jQuery.fn.valid) {
                    window.jQuery(input).valid();
                }
                input.focus();
            });
        });
    }

    // ---------- Live order timeline ----------
    function initLiveOrder() {
        var live = document.getElementById('order-live');
        if (!live || live.getAttribute('data-final') === 'true') {
            return;
        }

        var url = live.getAttribute('data-live-url');
        var lastStatus = live.getAttribute('data-status');
        var polls = 0;
        var maxPolls = 150;

        function poll() {
            polls++;
            fetch(url, { cache: 'no-store', headers: { 'X-Requested-With': 'fetch' } })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error('HTTP ' + response.status);
                    }
                    return response.text();
                })
                .then(function (html) {
                    var current = document.getElementById('order-live');
                    if (!current) {
                        return;
                    }
                    current.outerHTML = html;
                    refreshIcons();

                    var fresh = document.getElementById('order-live');
                    var status = fresh.getAttribute('data-status');
                    if (status !== lastStatus) {
                        lastStatus = status;
                        if (status === 'Confirmed') {
                            window.showToast('Your order is confirmed!', 'success');
                        } else if (status === 'Cancelled') {
                            window.showToast('Your order was cancelled. See the timeline for the reason.', 'error');
                        } else {
                            window.showToast('Order update: ' + (fresh.getAttribute('data-status-label') || status), 'info');
                        }
                    }

                    if (fresh.getAttribute('data-final') !== 'true' && polls < maxPolls) {
                        setTimeout(poll, 2000);
                    }
                })
                .catch(function () {
                    if (polls < maxPolls) {
                        setTimeout(poll, 4000);
                    }
                });
        }

        setTimeout(poll, 2000);
    }

    document.addEventListener('DOMContentLoaded', function () {
        refreshIcons();
        initSidebar();
        initFilters();
        initConfirm();
        initBusyGuard();
        initQuantity();
        initTestCards();
        initLiveOrder();
    });
})();