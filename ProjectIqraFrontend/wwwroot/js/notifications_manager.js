class NotificationsManager {
    constructor() {
        // Main container for notifications data
        this.notifications = [];

        // jQuery element references
        this.$bell = $('#header-notification');
        this.$counter = $('#notification-counter');
        this.$list = $('#notifications-list');
        this.$emptyState = $('#notification-empty-state');
        this.$dropdown = this.$bell.parent().find('.dropdown-menu');

        // Initial setup
        this.init();
    }

    /**
     * Initializes event listeners for the notification system.
     */
    init() {
        // Handle click on a notification item for "onRead" callback
        this.$list.on('click', '.notification-item', (e) => {
            const $item = $(e.currentTarget);
            const notificationId = $item.data('id');
            this.markAsRead(notificationId);
        });

        // Handle click on "Mark all as read" button
        $('#mark-all-read').on('click', (e) => {
            e.preventDefault(); // Prevent the link's default behavior
            e.stopPropagation(); // Stop the event from closing the dropdown
            this.markAllAsRead();
        });

        // Prevent dropdown from closing when clicking inside, unless it's an action button or link
        this.$dropdown.on('click', (e) => {
            if (!$(e.target).closest('a, .btn').length) {
                e.stopPropagation();
            }
        });
    }

    /**
     * The main method to add a new notification.
     * @param {object} notificationData - The notification object.
     * @param {string} [notificationData.id] - Unique ID (useful for backend items).
     * @param {string} notificationData.icon - Font Awesome icon class (e.g., 'fa-solid fa-user').
     * @param {string} notificationData.title - The main title of the notification.
     * @param {string} notificationData.description - The detailed text.
     * @param {Date} [notificationData.timestamp=new Date()] - The time of the event.
     * @param {boolean} [notificationData.isFixed=false] - If true, stays at the top.
     * @param {string} [notificationData.source='frontend'] - 'frontend' or 'backend'.
     * @param {function} [notificationData.onRead] - Callback for when read (for backend).
     * @param {object} [notificationData.action] - Optional action button.
     * @param {string} notificationData.action.text - Text for the button.
     * @param {string} [notificationData.action.class='btn-primary'] - Bootstrap button class.
     * @param {function} notificationData.action.callback - Function to execute on click.
     */
    addNotification(notificationData) {
        const notification = {
            id: notificationData.id || `notif_${new Date().getTime()}_${Math.random()}`,
            isRead: false,
            timestamp: new Date(),
            ...notificationData, // Overwrite defaults with provided data
        };

        this.notifications.unshift(notification); // Add to the beginning of the array
        this.render();
    }

    /**
     * Marks a notification as read and triggers onRead callback if available.
     * @param {string} id - The ID of the notification to mark as read.
     */
    markAsRead(id) {
        const notification = this.notifications.find(n => n.id === id);
        if (notification && !notification.isRead) {
            notification.isRead = true;

            // If it's a backend notification with a callback, call it!
            if (notification.source === 'backend' && typeof notification.onRead === 'function') {
                notification.onRead(notification.id);
            }

            this.render(); // Re-render to update UI (e.g., remove 'unread' class)
        }
    }

    /**
     * Renders all notifications into the dropdown list.
     */
    render() {
        this.$list.empty(); // Clear the current list

        if (this.notifications.length === 0) {
            this.$list.append(this.$emptyState.show());
            this.updateUnreadCount();
            return;
        }

        this.$emptyState.hide();

        // Sort: Fixed notifications on top, then by timestamp descending
        const sortedNotifications = [...this.notifications].sort((a, b) => {
            if (a.isFixed && !b.isFixed) return -1;
            if (!a.isFixed && b.isFixed) return 1;
            return new Date(b.timestamp) - new Date(a.timestamp);
        });

        sortedNotifications.forEach(notification => {
            const html = this._generateNotificationHTML(notification);
            const $item = $(html);

            // Attach the action button callback
            if (notification.action && typeof notification.action.callback === 'function') {
                $item.find('.notification-action-btn').on('click', (e) => {
                    e.stopPropagation(); // Prevent the item's own read event
                    notification.action.callback();
                });
            }

            this.$list.append($item);
        });

        this.updateUnreadCount();
    }

    /**
     * Updates the unread count badge on the bell icon.
     */
    updateUnreadCount() {
        const unreadCount = this.notifications.filter(n => !n.isRead).length;
        if (unreadCount > 0) {
            this.$counter.text(unreadCount).show();
        } else {
            this.$counter.hide();
        }
    }

    /**
     * Marks all unread notifications as read.
     */
    markAllAsRead() {
        let notificationsToUpdate = [];

        this.notifications.forEach(notification => {
            if (!notification.isRead) {
                notification.isRead = true;
                notificationsToUpdate.push(notification);
            }
        });

        // Only proceed if there were actual changes
        if (notificationsToUpdate.length > 0) {
            // Trigger onRead callbacks for all newly read backend notifications
            notificationsToUpdate.forEach(notification => {
                if (notification.source === 'backend' && typeof notification.onRead === 'function') {
                    notification.onRead(notification.id);
                }
            });

            // Re-render the UI to reflect the changes
            this.render();
        }
    }

    /**
     * Generates the HTML for a single notification item.
     * @param {object} notification - The notification object.
     * @returns {string} - The HTML string for the item.
     * @private
     */
    _generateNotificationHTML(notification) {
        const timeAgo = this._timeSince(new Date(notification.timestamp));
        const unreadClass = notification.isRead ? '' : 'unread';

        let actionButtonHTML = '';
        if (notification.action) {
            actionButtonHTML = `<div class="mt-2">
                <button class="btn btn-sm ${notification.action.class || 'btn-primary'} notification-action-btn">
                    ${notification.action.text}
                </button>
            </div>`;
        }

        return `
            <div class="notification-item d-flex align-items-center ${unreadClass}" data-id="${notification.id}">
                <div class="flex-shrink-0 me-3">
                    <div class="notification-icon text-primary">
                        <i class="${notification.icon}"></i>
                    </div>
                </div>
                <div class="flex-grow-1">
                    <div class="d-flex justify-content-between">
                        <div class="notification-content">
                            <b class="title">${notification.title}</b>
                            <p class="description mb-0">${notification.description}</p>
                        </div>
                        <small class="notification-meta ms-2 text-nowrap">${timeAgo}</small>
                    </div>
                    ${actionButtonHTML}
                </div>
            </div>
        `;
    }

    /**
     * Helper to calculate "time ago" string. For production, consider a library like date-fns.
     * @param {Date} date - The date to compare against now.
     * @private
     */
    _timeSince(date) {
        const seconds = Math.floor((new Date() - date) / 1000);
        let interval = seconds / 31536000;
        if (interval > 1) return Math.floor(interval) + "y ago";
        interval = seconds / 2592000;
        if (interval > 1) return Math.floor(interval) + "m ago";
        interval = seconds / 86400;
        if (interval > 1) return Math.floor(interval) + "d ago";
        interval = seconds / 3600;
        if (interval > 1) return Math.floor(interval) + "h ago";
        interval = seconds / 60;
        if (interval > 1) return Math.floor(interval) + "m ago";
        return Math.floor(seconds) + "s ago";
    }
}