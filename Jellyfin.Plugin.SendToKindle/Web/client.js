(() => {
    'use strict';

    const buttonId = 'sendToKindleButton';
    let refreshTimer;
    let lastItemId;

    function currentItemId() {
        const queryIndex = window.location.hash.indexOf('?');
        if (queryIndex < 0) {
            return null;
        }

        return new URLSearchParams(window.location.hash.slice(queryIndex + 1)).get('id');
    }

    function removeButton() {
        document.getElementById(buttonId)?.remove();
    }

    function notify(message) {
        if (window.Dashboard?.alert) {
            window.Dashboard.alert(message);
        } else {
            window.alert(message);
        }
    }

    async function pollJob(jobId, button) {
        while (button.isConnected) {
            await new Promise(resolve => window.setTimeout(resolve, 1500));
            const job = await window.ApiClient.ajax({
                type: 'GET',
                url: window.ApiClient.getUrl(`SendToKindle/Jobs/${jobId}`),
                dataType: 'json'
            });
            const label = button.querySelector('.sendToKindleLabel');
            label.textContent = job.Message || 'Send to Kindle';
            const succeeded = job.Status === 'Succeeded' || job.Status === 3;
            const failed = job.Status === 'Failed' || job.Status === 4;
            if (succeeded || failed) {
                button.disabled = false;
                if (failed) {
                    notify(job.Message || 'Send to Kindle failed.');
                }

                return;
            }
        }
    }

    async function enqueue(itemId, button) {
        button.disabled = true;
        button.querySelector('.sendToKindleLabel').textContent = 'Queued';
        try {
            const job = await window.ApiClient.ajax({
                type: 'POST',
                url: window.ApiClient.getUrl('SendToKindle/Jobs'),
                contentType: 'application/json',
                dataType: 'json',
                data: JSON.stringify({ ItemId: itemId })
            });
            await pollJob(job.JobId, button);
        } catch (error) {
            button.disabled = false;
            button.querySelector('.sendToKindleLabel').textContent = 'Send to Kindle';
            notify(error?.message || 'Unable to queue this book.');
        }
    }

    async function refresh() {
        const itemId = currentItemId();
        if (!itemId || !window.ApiClient) {
            lastItemId = null;
            removeButton();
            return;
        }

        if (lastItemId === itemId && document.getElementById(buttonId)) {
            return;
        }

        lastItemId = itemId;
        try {
            const user = await window.ApiClient.getCurrentUser();
            if (!user?.Policy?.IsAdministrator) {
                removeButton();
                return;
            }

            const item = await window.ApiClient.getItem(user.Id, itemId);
            if (item?.Type !== 'Book') {
                removeButton();
                return;
            }

            const page = document.querySelector('.itemDetailPage:not(.hide)') || document.querySelector('.itemDetailPage');
            const actions = page?.querySelector('.mainDetailButtons, .detailButtons');
            if (!actions || document.getElementById(buttonId)) {
                return;
            }

            const button = document.createElement('button');
            button.id = buttonId;
            button.type = 'button';
            button.className = 'button-flat btnAction emby-button';
            button.title = 'Send to Kindle';
            button.innerHTML = '<span class="material-icons send" aria-hidden="true"></span><span class="buttonText sendToKindleLabel">Send to Kindle</span>';
            button.addEventListener('click', () => enqueue(itemId, button));
            actions.appendChild(button);
        } catch {
            removeButton();
        }
    }

    function scheduleRefresh() {
        window.clearTimeout(refreshTimer);
        refreshTimer = window.setTimeout(refresh, 100);
    }

    window.addEventListener('hashchange', scheduleRefresh);
    new MutationObserver(scheduleRefresh).observe(document.body, { childList: true, subtree: true });
    scheduleRefresh();
})();
