const oauthResultElement = document.getElementById("oauthResult");

const oauthResult = {
	type: "iqra-integration-oauth-result",
	success: oauthResultElement.dataset.success === "true",
	reason: oauthResultElement.dataset.reason || "",
};

if (window.opener && !window.opener.closed) {
	window.opener.postMessage(oauthResult, window.location.origin);
	window.close();
} else {
	window.location.replace(oauthResultElement.dataset.redirectUrl);
}
