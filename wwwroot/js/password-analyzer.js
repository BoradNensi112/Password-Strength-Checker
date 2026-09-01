/**
 * SecurePass Real-Time Password Analyzer Engine
 * High-performance, zero-latency client-side security analysis
 */
const SecurePassAnalyzer = {
    commonPasswords: new Set([
        "123456", "password", "12345678", "qwerty", "123456789", "12345", "1234", "111111", "1234567",
        "dragon", "123123", "baseball", "iloveyou", "trustno1", "admin", "welcome", "login", "master",
        "football", "monkey", "sunshine", "princess", "superman", "shadow", "pass1234", "pass@123",
        "test1234", "qwertyuiop", "asdfghjkl", "zxcvbnm", "letmein", "changeme", "secret", "p@ssword",
        "p@ssw0rd", "admin123", "root", "toor", "user", "guest", "default", "securepass", "secure123",
        "1234567890", "password123", "pass123", "adminadmin", "testing123", "qwerty123"
    ]),

    keyboardWalks: [
        "qwertyuiop", "asdfghjkl", "zxcvbnm",
        "poiuytrewq", "lkjhgfdsa", "mnbvcxz",
        "1234567890", "0987654321",
        "abcdefghijklmnopqrstuvwxyz",
        "zyxwvutsrqponmlkjihgfedcba"
    ],

    analyze(password, personalInfo = '') {
        if (!password || password.length === 0) {
            return {
                score: 0,
                strengthLevel: 'Empty',
                colorHex: '#64748B',
                entropyBits: 0,
                crackTimeFormatted: 'Instant (< 1 sec)',
                hasMinLength: false,
                hasUppercase: false,
                hasLowercase: false,
                hasNumbers: false,
                hasSpecialChars: false,
                hasNoRepeatedChars: true,
                hasNoSequentialChars: true,
                isNotCommon: true,
                hasNoPersonalInfo: true,
                suggestions: ['Enter a password to begin real-time analysis.'],
                rules: []
            };
        }

        const len = password.length;
        const hasUpper = /[A-Z]/.test(password);
        const hasLower = /[a-z]/.test(password);
        const hasDigit = /[0-9]/.test(password);
        const hasSpecial = /[^A-Za-z0-9]/.test(password);
        const hasMinLen = len >= 8;

        const hasRepeated = this.hasConsecutiveRepeats(password);
        const hasSequential = this.hasSequentialPattern(password);
        const isCommon = this.isCommonPassword(password);
        const hasPersonal = this.hasPersonalInfo(password, personalInfo);

        // Pool calculation (R)
        let poolSize = 0;
        if (hasLower) poolSize += 26;
        if (hasUpper) poolSize += 26;
        if (hasDigit) poolSize += 10;
        if (hasSpecial) poolSize += 33;
        poolSize = poolSize > 0 ? poolSize : 1;

        // Shannon Entropy: E = L * log2(R)
        const entropy = len * Math.log2(poolSize);

        // Score formulation (0 to 100)
        let score = 0;
        if (len >= 20) score += 40;
        else if (len >= 16) score += 35;
        else if (len >= 12) score += 28;
        else if (len >= 8) score += 18;
        else score += len * 2;

        const variety = (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);
        score += variety * 10;

        if (entropy >= 80) score += 20;
        else if (entropy >= 60) score += 15;
        else if (entropy >= 40) score += 10;
        else if (entropy >= 25) score += 5;

        // Deductions & Penalties
        if (isCommon) score = Math.min(score, 15);
        if (hasPersonal) score -= 25;
        if (hasSequential) score -= 15;
        if (hasRepeated) score -= 15;
        if (len < 8) score = Math.min(score, 30);

        score = Math.max(5, Math.min(100, score));

        let strengthLevel = 'Weak';
        let colorHex = '#EF4444'; // Crimson Red
        if (score >= 90) {
            strengthLevel = 'Very Strong';
            colorHex = '#10B981'; // Emerald Green
        } else if (score >= 70) {
            strengthLevel = 'Strong';
            colorHex = '#22C55E'; // Neon Green
        } else if (score >= 40) {
            strengthLevel = 'Medium';
            colorHex = '#F59E0B'; // Amber Orange
        }

        const crackTime = this.calculateCrackTime(entropy, score, len);
        const suggestions = this.getSuggestions(password, hasUpper, hasLower, hasDigit, hasSpecial, isCommon, hasRepeated, hasSequential, hasPersonal, score);

        return {
            score,
            strengthLevel,
            colorHex,
            entropyBits: Math.round(entropy * 10) / 10,
            crackTimeFormatted: crackTime,
            hasMinLength: hasMinLen,
            hasUppercase: hasUpper,
            hasLowercase: hasLower,
            hasNumbers: hasDigit,
            hasSpecialChars: hasSpecial,
            hasNoRepeatedChars: !hasRepeated,
            hasNoSequentialChars: !hasSequential,
            isNotCommon: !isCommon,
            hasNoPersonalInfo: !hasPersonal,
            suggestions
        };
    },

    hasConsecutiveRepeats(str) {
        if (str.length < 3) return false;
        for (let i = 0; i < str.length - 2; i++) {
            if (str[i] === str[i + 1] && str[i + 1] === str[i + 2]) return true;
        }
        return false;
    },

    hasSequentialPattern(str) {
        if (str.length < 3) return false;
        const lower = str.toLowerCase();
        for (const pattern of this.keyboardWalks) {
            for (let i = 0; i <= pattern.length - 3; i++) {
                const sub = pattern.substring(i, i + 3);
                if (lower.includes(sub)) return true;
            }
        }
        return false;
    },

    isCommonPassword(str) {
        if (!str) return false;
        const clean = str.trim().toLowerCase();
        if (this.commonPasswords.has(clean)) return true;
        const normalized = clean.replace(/@/g, 'a')
                                .replace(/0/g, 'o')
                                .replace(/1/g, 'i')
                                .replace(/\$/g, 's')
                                .replace(/3/g, 'e')
                                .replace(/!/g, 'i');
        return this.commonPasswords.has(normalized);
    },

    hasPersonalInfo(password, personalInfo) {
        if (!personalInfo || !password) return false;
        const tokens = personalInfo.split(/[\s@._-]+/).filter(t => t.length >= 3);
        const lower = password.toLowerCase();
        for (const token of tokens) {
            if (lower.includes(token.toLowerCase())) return true;
        }
        return false;
    },

    calculateCrackTime(entropy, score, len) {
        if (score < 20 || len < 6) return 'Instant (< 1 sec)';
        const guesses = Math.pow(2, entropy);
        const seconds = guesses / 10000000000.0; // 10B guesses/sec

        if (seconds < 1) return 'A few milliseconds';
        if (seconds < 60) return Math.round(seconds) + ' seconds';
        if (seconds < 3600) return Math.round(seconds / 60) + ' minutes';
        if (seconds < 86400) return Math.round(seconds / 3600) + ' hours';
        if (seconds < 2592000) return Math.round(seconds / 86400) + ' days';
        if (seconds < 31536000) return Math.round(seconds / 2592000) + ' months';
        if (seconds < 31536000 * 100) return Math.round(seconds / 31536000) + ' years';
        if (seconds < 31536000 * 10000) return Math.round(seconds / 31536000).toLocaleString() + ' years';
        return 'Centuries (Quantum Proof)';
    },

    getSuggestions(password, hasUpper, hasLower, hasDigit, hasSpecial, isCommon, hasRepeated, hasSequential, hasPersonal, score) {
        const list = [];
        if (password.length < 12) list.push('Increase password length to at least 12–16 characters.');
        if (!hasUpper) list.push('Add at least one uppercase letter (A–Z).');
        if (!hasLower) list.push('Add at least one lowercase letter (a–z).');
        if (!hasDigit) list.push('Include numeric digits (0–9) for higher entropy.');
        if (!hasSpecial) list.push('Add special symbols (!@#$%^&*) to defend against dictionary attacks.');
        if (hasRepeated) list.push('Avoid repeating identical characters consecutively (e.g., "aaa").');
        if (hasSequential) list.push('Avoid sequential letters, numbers, or keyboard walks (e.g., "123", "abc", "qwerty").');
        if (isCommon) list.push('This password appears in common breach lists! Never use common dictionary words.');
        if (hasPersonal) list.push('Avoid using personal information (name, username, email) in your password.');
        if (list.length === 0 && score >= 90) list.push('Outstanding! This password fulfills elite cybersecurity standards.');
        return list;
    }
};

/**
 * Binds an input field to real-time UI widgets (meter, checklist, crack time, circular gauge)
 */
function bindPasswordStrengthChecker(config) {
    const input = document.querySelector(config.inputSelector);
    if (!input) return;

    function update() {
        const val = input.value || '';
        const personalInfo = config.personalInfoSelector ? (document.querySelector(config.personalInfoSelector)?.value || '') : '';
        const res = SecurePassAnalyzer.analyze(val, personalInfo);

        // Update linear bar if exists
        const barSelectors = [config.barSelector, '#checkerStrengthBar', '#regStrengthBar', '#profileStrengthBar', '.strength-bar-fill'].filter(Boolean);
        for (const s of barSelectors) {
            const bar = document.querySelector(s);
            if (bar) {
                bar.style.width = res.score + '%';
                bar.style.backgroundColor = res.colorHex;
                bar.style.boxShadow = '0 0 14px ' + res.colorHex + 'aa';
            }
        }

        // Update strength text label
        const strengthSelectors = [config.strengthTextSelector, '#checkerStrengthText', '#regStrengthText', '#profileStrengthText', '.strength-text'].filter(Boolean);
        for (const s of strengthSelectors) {
            const el = document.querySelector(s);
            if (el) {
                el.innerText = res.strengthLevel;
                el.style.color = res.colorHex;
            }
        }

        // Update score label
        const scoreSelectors = [config.scoreTextSelector, '#checkerScoreText', '#regScoreText', '#profileScoreText', '.strength-score-label'].filter(Boolean);
        for (const s of scoreSelectors) {
            const el = document.querySelector(s);
            if (el) el.innerText = res.score + '/100';
        }

        // Update circular gauge meter & numbers
        const gaugeMeter = document.querySelector(config.gaugeMeterSelector || '#gaugeMeter') || document.querySelector('#checkerGaugeMeter');
        if (gaugeMeter) {
            const circumference = 283;
            const offset = circumference - (res.score / 100) * circumference;
            gaugeMeter.style.strokeDashoffset = offset;
            gaugeMeter.style.stroke = res.colorHex;
        }

        const gaugeNum = document.querySelector('#gaugeNumber') || document.querySelector('#checkerGaugeNumber') || document.querySelector('.circular-gauge-number');
        if (gaugeNum) {
            gaugeNum.innerText = res.score;
            gaugeNum.style.color = res.colorHex;
        }

        const gaugeUnit = document.querySelector('#gaugeLevel') || document.querySelector('#checkerGaugeLevel') || document.querySelector('.circular-gauge-unit');
        if (gaugeUnit) {
            gaugeUnit.innerText = res.strengthLevel;
            gaugeUnit.style.color = res.colorHex;
        }

        // Update crack time & entropy
        const crackTimeEl = document.querySelector(config.crackTimeSelector || '#crackTimeText') || document.querySelector('#checkerCrackTime');
        if (crackTimeEl) crackTimeEl.innerText = res.crackTimeFormatted;

        const entropyEl = document.querySelector(config.entropySelector || '#entropyText') || document.querySelector('#checkerEntropy');
        if (entropyEl) entropyEl.innerText = res.entropyBits + ' bits';

        // Update hidden form fields
        if (config.hiddenScoreSelector) {
            const el = document.querySelector(config.hiddenScoreSelector);
            if (el) el.value = res.score;
        }
        if (config.hiddenStrengthSelector) {
            const el = document.querySelector(config.hiddenStrengthSelector);
            if (el) el.value = res.strengthLevel;
        }

        // Update checklist items
        updateRuleCheck('rule-min-length', res.hasMinLength);
        updateRuleCheck('rule-upper', res.hasUppercase);
        updateRuleCheck('rule-lower', res.hasLowercase);
        updateRuleCheck('rule-numbers', res.hasNumbers);
        updateRuleCheck('rule-symbols', res.hasSpecialChars);
        updateRuleCheck('rule-no-repeats', res.hasNoRepeatedChars);
        updateRuleCheck('rule-no-seq', res.hasNoSequentialChars);
        updateRuleCheck('rule-not-common', res.isNotCommon);
        updateRuleCheck('rule-no-personal', res.hasNoPersonalInfo);

        // Update suggestions box
        const listEl = document.querySelector(config.suggestionsListSelector || '#suggestionsList') || document.querySelector('#checkerSuggestionsList');
        if (listEl) {
            listEl.innerHTML = res.suggestions.map(function(s) {
                return '<li><i class="fa-solid fa-triangle-exclamation"></i> <span>' + s + '</span></li>';
            }).join('');
        }
    }

    ['input', 'keyup', 'change', 'paste'].forEach(function(evt) {
        input.addEventListener(evt, update);
    });

    // Run initial update on page load
    update();
}

function updateRuleCheck(id, passed) {
    const el = document.getElementById(id);
    if (!el) return;
    if (passed) {
        el.classList.add('passed');
        const icon = el.querySelector('.rule-icon i');
        if (icon) {
            icon.className = 'fa-solid fa-check text-success';
        }
    } else {
        el.classList.remove('passed');
        const icon = el.querySelector('.rule-icon i');
        if (icon) {
            icon.className = 'fa-solid fa-xmark text-muted';
        }
    }
}
