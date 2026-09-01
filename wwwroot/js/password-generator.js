/**
 * SecurePass Cryptographic Password Generator
 */
const SecurePassGenerator = {
    lowercase: 'abcdefghijklmnopqrstuvwxyz',
    uppercase: 'ABCDEFGHIJKLMNOPQRSTUVWXYZ',
    numbers: '0123456789',
    symbols: '!@#$%^&*()_+-=[]{}|;:,.<>?',
    similarChars: '0OolI1|`\'\"',

    generate(options) {
        const length = Math.max(8, Math.min(64, options.length || 16));
        const avoidSimilar = options.avoidSimilar !== false;

        let lower = this.lowercase;
        let upper = this.uppercase;
        let num = this.numbers;
        let sym = this.symbols;

        if (avoidSimilar) {
            lower = lower.split('').filter(c => !this.similarChars.includes(c)).join('');
            upper = upper.split('').filter(c => !this.similarChars.includes(c)).join('');
            num = num.split('').filter(c => !this.similarChars.includes(c)).join('');
            sym = sym.split('').filter(c => !this.similarChars.includes(c)).join('');
        }

        const pools = [];
        const resultChars = [];

        if (options.includeLowercase && lower.length > 0) {
            pools.push(lower);
            resultChars.push(this.getRandomChar(lower));
        }
        if (options.includeUppercase && upper.length > 0) {
            pools.push(upper);
            resultChars.push(this.getRandomChar(upper));
        }
        if (options.includeNumbers && num.length > 0) {
            pools.push(num);
            resultChars.push(this.getRandomChar(num));
        }
        if (options.includeSymbols && sym.length > 0) {
            pools.push(sym);
            resultChars.push(this.getRandomChar(sym));
        }

        if (pools.length === 0) {
            pools.push(lower);
            resultChars.push(this.getRandomChar(lower));
        }

        const combinedPool = pools.join('');
        while (resultChars.length < length) {
            resultChars.push(this.getRandomChar(combinedPool));
        }

        // Fisher-Yates cryptographically secure shuffle
        for (let i = resultChars.length - 1; i > 0; i--) {
            const j = this.getRandomInt(i + 1);
            const temp = resultChars[i];
            resultChars[i] = resultChars[j];
            resultChars[j] = temp;
        }

        const password = resultChars.join('');
        const analysis = SecurePassAnalyzer.analyze(password);

        return {
            password: password,
            score: analysis.score,
            strengthLevel: analysis.strengthLevel,
            colorHex: analysis.colorHex,
            crackTimeFormatted: analysis.crackTimeFormatted,
            entropyBits: analysis.entropyBits
        };
    },

    getRandomChar(pool) {
        const idx = this.getRandomInt(pool.length);
        return pool[idx];
    },

    getRandomInt(max) {
        const array = new Uint32Array(1);
        window.crypto.getRandomValues(array);
        return array[0] % max;
    }
};
