document.addEventListener('DOMContentLoaded', function () {
    // Animate on load
    document.querySelectorAll('.animate-on-load').forEach(function (element) {
        window.setTimeout(function () {
            element.classList.add('animate-in');
        }, 80);
    });

    function animateMetricValues() {
        document.querySelectorAll('.metric-value[data-target]').forEach(function (element) {
            var target = parseInt(element.getAttribute('data-target'), 10) || 0;
            var current = 0;
            var step = Math.max(1, Math.ceil(target / 30));
            var interval = window.setInterval(function () {
                current += step;
                if (current >= target) {
                    element.textContent = target;
                    window.clearInterval(interval);
                } else {
                    element.textContent = current;
                }
            }, 25);
        });
    }

    animateMetricValues();

    // Initialize theme from localStorage
    const savedTheme = localStorage.getItem('theme-preference') || 'dark';
    if (savedTheme === 'light') {
        document.body.classList.add('light-mode');
    } else {
        document.body.classList.remove('light-mode');
    }

    // Theme toggle functionality
    const themeToggleBtn = document.getElementById('theme-toggle-btn');
    function updateToggleUI(btn) {
        const isLight = document.body.classList.contains('light-mode');
        if (!btn) return;
        btn.setAttribute('aria-pressed', isLight ? 'true' : 'false');
        btn.title = isLight ? 'Switch to dark mode' : 'Switch to light mode';
        // Add a data attribute for styling if needed
        btn.dataset.theme = isLight ? 'light' : 'dark';
    }

    if (themeToggleBtn) {
        // sync initial state
        updateToggleUI(themeToggleBtn);

        // Handle theme toggle click
        themeToggleBtn.addEventListener('click', function (e) {
            e.preventDefault();
            document.body.classList.toggle('light-mode');
            // Save theme preference
            const isLightMode = document.body.classList.contains('light-mode');
            localStorage.setItem('theme-preference', isLightMode ? 'light' : 'dark');
            updateToggleUI(themeToggleBtn);
        });

        // keyboard accessibility: Enter or Space to toggle
        themeToggleBtn.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                themeToggleBtn.click();
            }
        });
    }

    function wireFileInputs() {
        document.querySelectorAll('.upload-box input[type="file"]').forEach(function (input) {
            var labelText = input.closest('.upload-box')?.querySelector('.file-picker-text');
            if (!labelText) return;

            input.addEventListener('change', function () {
                if (input.files && input.files.length > 0) {
                    labelText.textContent = input.files[0].name;
                }
                else {
                    labelText.textContent = 'Choose resume file';
                }
            });
        });
    }

    function wireAnalyzeProgress() {
        var analyzeForm = document.getElementById('analyzeForm');
        if (!analyzeForm) return;

        var progressPanel = document.getElementById('analysisProgress');
        var progressFill = document.getElementById('analysisProgressFill');
        var progressValue = document.getElementById('analysisProgressPercent');
        var submitButton = analyzeForm.querySelector('button[type="submit"]');

        if (!progressPanel || !progressFill || !progressValue || !submitButton) return;

        var animationTimeouts = [];
        var currentProgress = 0;
        var isSubmitted = false;

        function updateProgress(value) {
            currentProgress = Math.max(0, Math.min(100, value));
            progressFill.style.width = currentProgress + '%';
            progressValue.textContent = currentProgress + '%';
        }

        function setProgressSequence() {
            updateProgress(10);
            animationTimeouts.push(window.setTimeout(function () { updateProgress(35); }, 600));
            animationTimeouts.push(window.setTimeout(function () { updateProgress(55); }, 1450));
            animationTimeouts.push(window.setTimeout(function () { updateProgress(72); }, 2700));
            animationTimeouts.push(window.setTimeout(function () { updateProgress(88); }, 4200));
        }

        function cleanupSequence() {
            animationTimeouts.forEach(function (timeoutId) { window.clearTimeout(timeoutId); });
            animationTimeouts.length = 0;
        }

        analyzeForm.addEventListener('submit', function () {
            if (isSubmitted) return;
            isSubmitted = true;

            progressPanel.hidden = false;
            progressPanel.style.opacity = 0;
            window.setTimeout(function () {
                progressPanel.style.opacity = 1;
            }, 10);

            setProgressSequence();
            window.setTimeout(function () {
                submitButton.disabled = true;
                submitButton.classList.add('disabled');
            }, 0);

            window.addEventListener('beforeunload', function () {
                cleanupSequence();
                updateProgress(100);
            });
        });
    }

    wireFileInputs();
    wireAnalyzeProgress();

    // Trigger entrance and shimmer animations for hero elements
    (function triggerHeroAnimations() {
        const heroContainer = document.querySelector('.hero-container');
        const heroTitle = document.querySelector('.hero-title');
        const heroDesc = document.querySelector('.hero-description');

        if (heroContainer) {
            // ensure class present for existing CSS animation handler
            heroContainer.classList.add('animate-on-load');
            setTimeout(() => heroContainer.classList.add('animate-in'), 120);
        }

        if (heroTitle) {
            heroTitle.classList.add('animate-on-load');
            setTimeout(() => {
                heroTitle.classList.add('animate-in');
                // add shimmer only for light mode to keep dark mode intact
                if (document.body.classList.contains('light-mode')) {
                    heroTitle.classList.add('shimmer');
                }
            }, 220);
        }

        if (heroDesc) {
            heroDesc.classList.add('animate-on-load');
            setTimeout(() => heroDesc.classList.add('animate-in'), 320);
        }
    })();
});
