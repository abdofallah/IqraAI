class SignedFileHelper {
	static RenewMarginMs = 120000;
	static _pendingData = new Map();

	static isFresh(file, now = Date.now()) {
		if (!file || typeof file.url !== "string" || file.url === "") return false;
		const expiresAt = Date.parse(file.expiresAt);
		if (Number.isNaN(expiresAt)) return false;
		return expiresAt - now > SignedFileHelper.RenewMarginMs;
	}

	static fetchData(url) {
		if (SignedFileHelper._pendingData.has(url)) return SignedFileHelper._pendingData.get(url);

		const request = new Promise((resolve, reject) => {
			$.ajax({
				url: url,
				type: "GET",
				success: (response) => {
					if (!response || !response.success) {
						reject(response);
						return;
					}

					resolve(response.data);
				},
				error: (error) => {
					reject(error);
				},
			});
		}).finally(() => {
			SignedFileHelper._pendingData.delete(url);
		});

		SignedFileHelper._pendingData.set(url, request);
		return request;
	}

	static async renew(fetchFile) {
		try {
			const file = await fetchFile();
			if (!file || typeof file.url !== "string" || file.url === "") return null;
			return file;
		} catch (error) {
			console.error("Error occured while renewing a signed file link: ", error);
			return null;
		}
	}

	static use(options) {
		const state = { current: options.file, applied: null, renewed: false, renewing: null, unavailable: false };

		const apply = (url) => {
			state.applied = url;
			options.apply(url);
		};

		const markUnavailable = () => {
			if (state.unavailable) return;
			state.unavailable = true;
			options.onUnavailable();
		};

		const renew = () => {
			if (state.renewing) return state.renewing;

			state.renewed = true;
			state.renewing = SignedFileHelper.renew(options.fetchFile).then((file) => {
				state.renewing = null;
				if (file === null) {
					markUnavailable();
					return false;
				}

				state.current = file;
				if (options.onRenewed) options.onRenewed(file);
				apply(file.url);
				return true;
			});

			return state.renewing;
		};

		const handle = {
			current: () => state.current,
			failed: async (failedUrl) => {
				await handle.ready;
				if (state.unavailable) return;
				if (failedUrl !== undefined && failedUrl !== state.applied) return;
				if (state.renewing) {
					await state.renewing;
					return;
				}
				if (state.renewed) {
					markUnavailable();
					return;
				}
				await renew();
			},
		};

		if (SignedFileHelper.isFresh(state.current)) {
			apply(state.current.url);
			handle.ready = Promise.resolve(true);
		} else {
			handle.ready = renew();
		}

		return handle;
	}

	static showImage(image, options) {
		const token = {};
		let handle = null;

		image.data("signedFileToken", token);
		image.off("error.signedFile").on("error.signedFile", () => {
			if (handle && image.data("signedFileToken") === token) handle.failed(image.attr("src"));
		});

		handle = SignedFileHelper.use({
			file: options.file,
			fetchFile: options.fetchFile,
			onRenewed: options.onRenewed,
			apply: (url) => {
				if (image.data("signedFileToken") !== token) return;
				if (image.attr("src") !== url) image.attr("src", url);
			},
			onUnavailable: () => {
				if (image.data("signedFileToken") !== token) return;
				SignedFileHelper.releaseImage(image);
				image.attr("src", options.fallbackSrc);
			},
		});

		return handle;
	}

	static releaseImage(image) {
		image.removeData("signedFileToken");
		image.off("error.signedFile");
	}
}
