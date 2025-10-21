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
	const navBar = $("#nav-bar");
	const navList = $(".nav_list");
	const navLogo = $('.nav_logo');
	const navUpper = $('.upper-navigation');
	const navBottom = $('.bottom-navigation');

	var navLogoHeight = navLogo.outerHeight(true);
	var navListHeight = 0;
	navList.children().each(function () {
		navListHeight += $(this).outerHeight(true);
	});
	var navBottomHeight = navBottom.height();

	var totalAboveHeight = navLogoHeight + navListHeight + navBottomHeight;
	var windowHeight = window.innerHeight;

	var giveHeight = (windowHeight - (navBottomHeight + navLogoHeight));

	navUpper.stop(true, true);
	navList.stop(true, true);
	navUpper.animate({ "height": (navLogoHeight + giveHeight) }, { duration: 300 });
	if (totalAboveHeight > windowHeight) {	
		navList.css("overflow-y", "scroll").css("box-shadow", "inset rgb(203 229 78 / 10%) 0px -20px 10px -10px");
		navList.animate({ "height": giveHeight, "overflow-y": "scroll" }, { duration: 300 });
	} else {
		navList.css("overflow-y", "hidden").css("box-shadow", "");
		navList.animate({ "height": giveHeight, "overflow-y": "hidden" }, { duration: 300 });
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

	$innerContainer.stop(true);
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


			var interval = setInterval(() => {
				setDynamicNavHeight();
				setDynamicBodyHeight();
			}, 50);
			setTimeout(() => {
				clearInterval(interval);
			}, 500);
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
 * Analyzes the current URL to determine the dashboard context.
 * This is the central function that makes the entire system dynamic.
 * @returns {{basePath: string, tabPathIndex: number}} An object containing the
 *          base path for the current dashboard and the index of the tab segment in the URL.
 */
function getDashboardContext() {
	const pathParts = window.location.pathname.split('/').filter(p => p.length > 0);

	// Context 1: Business Dashboard (e.g., /business/123/overview)
	// It's identified by the first segment being "business".
	if (pathParts.length > 0 && pathParts[0] === 'business') {
		const basePath = pathParts.length >= 2 ? `/${pathParts[0]}/${pathParts[1]}` : `/${pathParts[0]}`;
		return {
			basePath: basePath,
			tabPathIndex: 2, // The tab path is the 3rd segment (index 2)
		};
	}

	// Context 2: User Dashboard (e.g., / or /settings)
	// This is the default case.
	return {
		basePath: '/',
		tabPathIndex: 0, // The tab path is the 1st segment (index 0)
	};
}

/**
 * Gets the stable base path of the URL based on the current dashboard context.
 * (e.g., "/" for user dashboard, "/business/123" for business dashboard).
 * @returns {string} The base URL path.
 */
function getBasePath() {
	return getDashboardContext().basePath;
}


/**
 * Extracts the tab-specific path from the URL using the correct index for the
 * current dashboard context. Returns null for invalid or missing tab paths.
 * @returns {string | null} The valid tab path or null.
 */
function getTabPathFromUrl() {
	const context = getDashboardContext();
	const pathParts = window.location.pathname.split("/").filter((p) => p.length > 0);

	if (pathParts.length > context.tabPathIndex) {
		const potentialTabPath = pathParts[context.tabPathIndex];
		if ($(`.l-navbar .nav_link[nav-url-path="${potentialTabPath}"]`).length > 0) {
			return potentialTabPath;
		}
	}
	return null;
}

/**
 * Gets sub-path segments from the URL that appear after the tab name.
 * For a URL like /business/6/campaigns/edit/123, this will return ['edit', '123'].
 * For a user dashboard URL like /settings/profile, this will return ['profile'].
 * @returns {string[]} An array of the sub-path segments.
 */
function getUrlSubPath() {
	const context = getDashboardContext();
	const pathParts = window.location.pathname.split("/").filter(p => p.length > 0);
	const subPathStartIndex = context.tabPathIndex + 1;

	if (pathParts.length > subPathStartIndex) {
		return pathParts.slice(subPathStartIndex);
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
	// Ensure we don't end up with a double slash for the root path
	const finalUrl = (basePath === '/' && newPath) ? `/${newPath}` : `${basePath}/${newPath}`;

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
	// Ensure we don't end up with a double slash for the root path
	const finalUrl = (basePath === '/' && newPath) ? `/${newPath}` : `${basePath}/${newPath}`;

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
		console.log("resize");
		const $body = $("body");
		$body.css("overflow", "hidden");
		makeSureNavToggleIconIsCorrect();
		if (resizeTimeout) {
			clearTimeout(resizeTimeout);
		}
		resizeTimeout = setTimeout(() => {
			console.log("resize complete");
			$body.css("overflow", "initial");
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

