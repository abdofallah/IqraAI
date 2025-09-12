// --- Global State & Configuration ---
// These variables track the window state to avoid unnecessary calculations during resize events.

let resizeTimeout;
let lastRecordedWidth = $(window).width();
let lastRecordedHeight = $(window).height();
let initialTabState = null;

// --- Core Public Functions ---

/**
 * Adjusts the height of the main navigation list. If the list's content overflows
 * the available window height, it sets a fixed height and enables scrolling.
 */
function setDynamicNavHeight() {
	const navList = $(".nav_list");
	const navLogo = $('.nav_logo');
	const navBottom = $('.bottom-navigation');

	var navLogoHeight = navLogo.outerHeight() * 1.5;
	var navListHeight = navList[0].scrollHeight;
	var navBottomHeight = navBottom.height() * 1.5;

	var totalAboveHeight = navLogoHeight + navListHeight + navBottomHeight;
	var windowHeight = window.innerHeight;

	if (totalAboveHeight > windowHeight) {
		var giveHeight = (windowHeight - (navBottomHeight + navLogoHeight));
		navList.css("overflow-y", "scroll").css("box-shadow", "inset rgb(203 229 78 / 10%) 0px -20px 10px -10px");
		navList.animate({ "height": giveHeight }, { duration: 300 });
	} else {
		navList.css("overflow-y", "hidden").css("box-shadow", "");
		navList.animate({ "height": "100%" }, { duration: 300 });
	}
}

/**
 * Calculates and animates the `min-height` for a given content container.
 * This ensures the content area fills the vertical space correctly.
 * @param {string | null} containerId - The ID of the tab container to resize.
 */
function setDynamicBodyHeight(containerId = null) {
	const $body = $("body");
	const $window = $(window);
	const $header = $("#header");
	const $mainWrapper = $(".main-container-wrapper");

	$body.css("overflow", "hidden");
	containerId = containerId || $(".l-navbar .nav_link.active").attr("for");

	const $container = $(`#${containerId}`);
	const $innerContainer = $container.find(".inner-container");
	const $headerContainer = $container.find(".inner-header-container");

	const windowHeight = $window[0].innerHeight;
	const headerHeight = $header[0].clientHeight;
	const wrapperPadding = parseInt($mainWrapper.css("padding-top")) + parseInt($mainWrapper.css("padding-bottom"));

	const baseHeaderHeight = 60;
	const additionalHeaderHeight = !$headerContainer.length || $headerContainer.hasClass("d-none") ? 0 : $headerContainer[0].clientHeight;
	const headerTextHeight = baseHeaderHeight + additionalHeaderHeight;
	const bodyCalculatedHeight = windowHeight - (headerHeight + headerTextHeight + wrapperPadding + 15);

	const resizeEvent = new CustomEvent("containerResize", {
		detail: { containerId, targetHeight: bodyCalculatedHeight },
	});

	$innerContainer.animate(
		{ "min-height": bodyCalculatedHeight },
		{
			duration: 300,
			progress: (animation, progress) => {
				const currentHeight = $container.find(".inner-container")[0].clientHeight;
				document.dispatchEvent(
					new CustomEvent("containerResizeProgress", {
						detail: { containerId, currentHeight, progress, targetHeight: bodyCalculatedHeight },
					})
				);
			},
			complete: () => {
				setTimeout(() => $body.css("overflow", "initial"), 10);
				document.dispatchEvent(resizeEvent);
			},
		}
	);
}

/**
 * Binds the click event to the main sidebar toggle button (hamburger icon).
 * @param {string} toggleId - The ID of the toggle button.
 * @param {string} navId - The ID of the navigation bar.
 * @param {string} bodyId - The ID of the body element for padding adjustments.
 * @param {string} headerId - The ID of the header element for padding adjustments.
 */
function changeActiveSidebarLink(toggleId, navId, bodyId, headerId) {
	const toggle = document.getElementById(toggleId);
	const nav = document.getElementById(navId);
	const bodypd = document.getElementById(bodyId);
	const headerpd = document.getElementById(headerId);

	if (toggle && nav && bodypd && headerpd) {
		toggle.addEventListener("click", () => {
			nav.classList.toggle("show");
			toggle.classList.toggle("fa-xmark");
			bodypd.classList.toggle("body-pd");
			headerpd.classList.toggle("header-body-pd");

			setTimeout(() => {
				setDynamicNavHeight();
				setDynamicBodyHeight();
			}, 50);
		});
	}
}

/**
 * On smaller screens, ensures the sidebar toggle icon is correct (hamburger vs. 'x')
 * based on whether the sidebar is currently shown or hidden.
 */
function makeSureNavToggleIconIsCorrect() {
	const windowWidth = $(window).width();
	if (windowWidth < 768) {
		const toggle = document.getElementById("header-toggle");
		if ($(".l-navbar").hasClass("show")) {
			if (!toggle.classList.contains("fa-xmark")) {
				toggle.classList.add("fa-xmark");
			}
		} else {
			if (toggle.classList.contains("fa-xmark")) {
				toggle.classList.remove("fa-xmark");
			}
		}
	}
}

// --- URL State Management Helper Functions ---

/**
 * Gets the stable base path of the URL (e.g., "/business/123"), ignoring any
 * additional segments like the tab path or invalid paths.
 * @returns {string} The base URL path.
 */
function getBasePath() {
	const pathParts = window.location.pathname.split("/").filter((part) => part.length > 0);
	if (pathParts.length >= 2) {
		return `/${pathParts[0]}/${pathParts[1]}`;
	}
	return `/${pathParts.join("/")}`;
}

/**
 * Extracts the tab-specific path from the URL, but only if it corresponds to a
 * valid, existing navigation link. Returns null for invalid or missing tab paths.
 * @returns {string | null} The valid tab path or null.
 */
function getTabPathFromUrl() {
	const pathParts = window.location.pathname.split("/").filter((p) => p);
	if (pathParts.length > 2) {
		const potentialTabPath = pathParts[2];
		if ($(`.l-navbar .nav_link[nav-url-path="${potentialTabPath}"]`).length > 0) {
			return potentialTabPath;
		}
	}
	return null;
}

/**
 * For a URL like /business/6/campaigns/edit/123, this will return ['edit', '123'].
 * @returns {string[]} An array of the sub-path segments.
 */
function getUrlSubPath() {
	const pathParts = window.location.pathname.split("/").filter(p => p);
	// The sub-path starts after the base path and tab path (3 segments).
	if (pathParts.length > 3) {
		return pathParts.slice(3);
	}
	return [];
}

/**
 * Updates the browser's URL using history.pushState. This changes the visible URL
 * and adds an entry to the browser's history without reloading the page.
 * @param {string} newPath - The new tab path to append to the base URL.
 */
function updateUrlForTab(newPath) {
	const basePath = getBasePath();
	const finalUrl = newPath ? `${basePath}/${newPath}`.replace('//', '/') : basePath;
	if (window.location.pathname !== finalUrl) {
		history.pushState({ tabPath: newPath }, "", finalUrl);
	}
}

/**
 * Replaces the current URL using history.replaceState. This is used to correct the URL
 * on initial load without creating an unwanted entry in the browser's history.
 * @param {string} newPath - The new tab path to set as the current URL.
 */
function replaceUrlForTab(newPath) {
	const basePath = getBasePath();
	const finalUrl = newPath ? `${basePath}/${newPath}`.replace('//', '/') : basePath;
	if (window.location.pathname !== finalUrl) {
		history.replaceState({ tabPath: newPath }, "", finalUrl);
	}
}

// --- Tab Navigation Logic Function ---

/**
 * Handles the logic for a navigation link click. It validates the action, dispatches
 * a cancellable event, updates the URL, and performs the tab switching animation.
 * @param {Event} event - The jQuery click event object.
 */
async function handleNavLinkClick(event) {
	const currentElement = $(event.currentTarget);

	if (currentElement.attr("href")) {
		return true;
	}
	event.preventDefault();

	const forTab = currentElement.attr("for");
	const activeElement = $(".l-navbar .nav_link.active");
	const activeElementFor = activeElement.attr("for");

	if (activeElementFor === forTab || currentElement.hasClass("disabled")) {
		return;
	}

	const tabChangeEvent = new CustomEvent("tabChange", {
		bubbles: true,
		cancelable: true,
		detail: { from: activeElementFor, to: forTab },
	});

	const listeners = jQuery._data($("#nav-bar")[0], "events")?.tabChange || [];
	const results = await Promise.all(
		listeners.map(
			(listener) =>
				new Promise((resolve) => {
					const result = listener.handler(tabChangeEvent);
					result instanceof Promise ? result.then(() => resolve(!tabChangeEvent.defaultPrevented)) : resolve(!tabChangeEvent.defaultPrevented);
				})
		)
	);

	const shouldProceed = results.every((result) => result);
	if (!shouldProceed) {
		return;
	}

	const newPath = currentElement.attr("nav-url-path");
	updateUrlForTab(newPath);

	activeElement.removeClass("active");
	$("#tabs-list .main-container.show").each((index, element) => {
		$(element).removeClass("show");
		setTimeout(() => $(element).addClass("d-none"), 150);
	});

	const newTabElement = $(`#${forTab}`);
	const subPath = getUrlSubPath();
	$(document).trigger("tabShowing", { tabId: forTab, urlSubPath: subPath });
	setTimeout(() => {
		currentElement.addClass("active");
		newTabElement.removeClass("d-none");
		setTimeout(() => {
			newTabElement.addClass("show");
			setTimeout(() => {
				setDynamicBodyHeight(forTab);
				$(document).trigger("tabShown", { tabId: forTab, urlSubPath: subPath });
			}, 10);
		}, 10);
	}, 150);
}

// --- Document Ready - Initialization and Event Binding ---

$(document).ready(() => {
	// 1. Initial Setup Calls
	// Binds the main sidebar toggle and sets initial visual states.
	changeActiveSidebarLink("header-toggle", "nav-bar", "body-pd", "header");
	makeSureNavToggleIconIsCorrect();
	setDynamicNavHeight();

	// 2. Handle Initial Tab State from URL on Page Load
	// This logic determines which tab to show when the page first loads.
	let initialTabPath = getTabPathFromUrl();

	// If the URL has no valid tab path (or an incorrect one), fall back to the default.
	if (!initialTabPath) {
		const defaultTabLink = $('.l-navbar .nav_link[is-default="true"]').first();
		if (defaultTabLink.length) {
			initialTabPath = defaultTabLink.attr('nav-url-path');
			// Correct the URL in the address bar without creating a new history entry.
			replaceUrlForTab(initialTabPath);
		}
	}

	// Activate the correct tab based on the final determined path.
	if (initialTabPath) {
		const targetElement = $(`.l-navbar .nav_link[nav-url-path="${initialTabPath}"]`);
		if (targetElement.length && !targetElement.hasClass("active")) {
			const tabId = targetElement.attr("for");
			$(".l-navbar .nav_link.active").removeClass("active");
			$("#tabs-list .main-container.show").removeClass("show").addClass("d-none");
			targetElement.addClass("active");
			$(`#${tabId}`).removeClass("d-none").addClass("show");

			const subPath = getUrlSubPath();
			initialTabState = { tabId: tabId, urlSubPath: subPath };
		}
	}

	// 3. Initial Height Calculation for all Tab Containers
	// Pre-calculates the height for all content panes to ensure smooth animations.
	$(".l-navbar .nav_link[for]").each((index, element) => {
		const containerId = $(element).attr("for");
		if (containerId) {
			setDynamicBodyHeight(containerId);
		}
	});

	// 4. Bind Global Event Listeners
	// Handles clicking a navigation link to switch tabs.
	$(document).on("click", ".l-navbar .nav_link", handleNavLinkClick);

	// Handles window resize events to keep the layout responsive.
	$(window).on("resize", () => {
		if (resizeTimeout) {
			clearTimeout(resizeTimeout);
		}
		makeSureNavToggleIconIsCorrect();
		resizeTimeout = setTimeout(() => {
			const currentWidth = $(window).width();
			const currentHeight = $(window).height();
			if (lastRecordedWidth !== currentWidth || lastRecordedHeight !== currentHeight) {
				lastRecordedWidth = currentWidth;
				lastRecordedHeight = currentHeight;
				const currentActiveTabId = $(".l-navbar .nav_link.active").attr("for");
				setDynamicBodyHeight(currentActiveTabId);
				setDynamicNavHeight();
			}
		}, 250);
	});

	// Handles browser back/forward button clicks.
	$(window).on("popstate", () => {
		const tabPath = getTabPathFromUrl();
		const defaultTab = $('.l-navbar .nav_link[is-default="true"]').first();
		let targetElement = tabPath ? $(`.l-navbar .nav_link[nav-url-path="${tabPath}"]`) : defaultTab;
		if (targetElement.length && !targetElement.hasClass('active')) {
			// Trigger a click to ensure all tab change logic is consistently applied.
			targetElement.trigger('click');
		}
	});

	/** This event fires after all scripts, images, etc., are loaded. **/
	$(window).on('load', function () {
		if (initialTabState) {
			var TabStateInterval = setInterval(
				() => {
					if (
						EverythingInitalized == true
					) {
						clearInterval(TabStateInterval);

						$(document).trigger("tabShowing", initialTabState);
						$(document).trigger("tabShown", initialTabState);
						initialTabState = null;
					}
				}
			, 100);
		}
	});
});

