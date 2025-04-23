$(document).ready(() => {
    const canvasElement = $('#imageSequenceCanvas');
    const leftLogoContainerElement = $('.left-logo-container');
    const mainIntroElement = $('#main-intro');

    const totalFrames = 127;
    const basePath = '/img/AI/';

    const adjustedBasePath = basePath.endsWith('/') ? basePath : basePath + '/';

    const fileNamePrefix = 'frame';
    const fileExtension = '.jpg';
    const paddingLength = 4;

    const imagesArray = Array.from({ length: totalFrames }, (v, k) => {
        const frameNumber = k + 1;
        const paddedFrameNumber = String(frameNumber).padStart(paddingLength, '0');
        return `${adjustedBasePath}${fileNamePrefix}${paddedFrameNumber}${fileExtension}`;
    });

    const originalImageWidth = 800;
    const originalImageHeight = 600;

    const logoOriginalX = 283;
    const logoOriginalY = 195;
    const logoOriginalWidth = 235;
    const logoOriginalHeight = 235;

    let currentFillMode = 'cover';

    const sequenceOptions = {
        images: imagesArray,
        loop: true,
        fps: 30,
        autoplay: true,
        preload: 'partial',
        preloadNumber: '1',
        fillMode: currentFillMode,
        onLoadingProgress: function (progress) {
        },
        onPreloadFinished: function (instance) {
            performLeftLogoResize();

            canvasElement.addClass('zooming');
            mainIntroElement.addClass('show');
            setTimeout(() => {
                leftLogoContainerElement.find('.left-logo').css('filter', 'drop-shadow(0px 0px 0px rgba(147, 180, 71, 0.3))');
                setTimeout(() => {
                    leftLogoContainerElement.find('.left-logo').addClass('glowing-loop');
                }, 1500);
            }, 1000);
        },
        onAnimationEnd: function (instance) {
        }
    };

    if (typeof AnimateImages === 'undefined') {
        console.error('AnimateImages library not found. Make sure the CDN script is loaded correctly.');
        return;
    }

    let sequence;
    try {
        sequence = new AnimateImages(canvasElement[0], sequenceOptions);
        leftLogoContainerElement.find('.left-logo').css('filter', 'drop-shadow(0px 0px 100px rgba(147, 180, 71, 0.3))');

    } catch (error) {
        console.error('Error initializing AnimateImages:', error);
        return;
    }

    function performLeftLogoResize() {
        const canvasEl = canvasElement[0];
        const canvasWidth = canvasEl.clientWidth;
        const canvasHeight = canvasEl.clientHeight;

        let scaleFactor;
        let imageDx = 0;
        let imageDy = 0;
        let sx = 0;
        let sy = 0;

        if (currentFillMode === 'contain') {
            const ratioX = canvasWidth / originalImageWidth;
            const ratioY = canvasHeight / originalImageHeight;
            scaleFactor = Math.min(ratioX, ratioY);

            const displayedImageWidth = originalImageWidth * scaleFactor;
            const displayedImageHeight = originalImageHeight * scaleFactor;

            imageDx = (canvasWidth - displayedImageWidth) / 2;
            imageDy = (canvasHeight - displayedImageHeight) / 2;

            sx = 0;
            sy = 0;

        } else if (currentFillMode === 'cover') {
            scaleFactor = Math.max(canvasWidth / originalImageWidth, canvasHeight / originalImageHeight);

            const sourceWidth = canvasWidth / scaleFactor;
            const sourceHeight = canvasHeight / scaleFactor;

            sx = (originalImageWidth - sourceWidth) / 2;
            sy = (originalImageHeight - sourceHeight) / 2;

            imageDx = 0;
            imageDy = 0;

        } else {
            console.warn(`Unsupported fillMode: ${currentFillMode}. Logo overlay positioning may be incorrect.`);
            leftLogoContainerElement.hide();
            return;
        }

        const logoRelativeToSourceX = logoOriginalX - sx;
        const logoRelativeToSourceY = logoOriginalY - sy;

        const logoCanvasX = imageDx + logoRelativeToSourceX * scaleFactor;
        const logoCanvasY = imageDy + logoRelativeToSourceY * scaleFactor;

        const logoCanvasWidth = logoOriginalWidth * scaleFactor;
        const logoCanvasHeight = logoOriginalHeight * scaleFactor;

        leftLogoContainerElement.css({
            left: logoCanvasX + 'px',
            top: logoCanvasY + 'px',
            width: logoCanvasWidth + 'px',
            height: logoCanvasHeight + 'px',
            display: 'block'
        });
    }

    $(window).resize(() => {
        setTimeout(performLeftLogoResize, 50);
    });

    performLeftLogoResize();
});