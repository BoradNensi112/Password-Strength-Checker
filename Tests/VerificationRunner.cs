using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using SecurePass.Services;
using SecurePass.Models.ViewModels;

namespace SecurePass.Tests
{
    public static class VerificationRunner
    {
        public static void RunTests()
        {
            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string?> {
                {"Security:EncryptionKey", "Test_Master_Key_AES256_Encryption_2026!"}
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

            var encryption = new EncryptionService(configuration);
            var analyzer = new PasswordAnalyzerService();
            var generator = new PasswordGeneratorService(analyzer);

            Console.WriteLine("=== [1] Testing AES-256 Encryption & Decryption ===");
            string sampleSecret = "BankPassword@99#SuperSecure!";
            string encrypted = encryption.Encrypt(sampleSecret);
            string decrypted = encryption.Decrypt(encrypted);
            bool encryptSuccess = sampleSecret == decrypted && encrypted != sampleSecret;
            Console.WriteLine($"Plain: {sampleSecret}");
            Console.WriteLine($"Encrypted: {encrypted}");
            Console.WriteLine($"Decrypted: {decrypted}");
            Console.WriteLine($"Result: {(encryptSuccess ? "PASS" : "FAIL")}");
            if (!encryptSuccess) throw new Exception("AES Encryption test failed");

            Console.WriteLine("\n=== [2] Testing BCrypt Hashing ===");
            string masterPass = "MasterSecretPass#2026";
            string hash = BCrypt.Net.BCrypt.HashPassword(masterPass);
            bool verifyPass = BCrypt.Net.BCrypt.Verify(masterPass, hash);
            bool verifyFail = !BCrypt.Net.BCrypt.Verify("WrongPass", hash);
            Console.WriteLine($"BCrypt Hash: {hash}");
            Console.WriteLine($"Verify Correct: {verifyPass}, Verify Wrong: {verifyFail}");
            if (!verifyPass || !verifyFail) throw new Exception("BCrypt verification test failed");

            Console.WriteLine("\n=== [3] Testing Password Strength Analyzer ===");
            var weakCheck = analyzer.Analyze("123456");
            Console.WriteLine($"'123456' -> Score: {weakCheck.Score}, Level: {weakCheck.StrengthLevel}, Common: {!weakCheck.IsNotCommonPassword}");
            if (weakCheck.Score > 20 || weakCheck.StrengthLevel != "Weak") throw new Exception("Weak password check failed");

            var strongCheck = analyzer.Analyze("K9#xP!vL#91");
            Console.WriteLine($"'K9#xP!vL#91' -> Score: {strongCheck.Score}, Level: {strongCheck.StrengthLevel}, Entropy: {strongCheck.EntropyBits} bits, CrackTime: {strongCheck.CrackTimeFormatted}");
            if (strongCheck.Score < 70) throw new Exception("Strong password check failed");

            var personalCheck = analyzer.Analyze("AlexMorgan2026!", "Alex Morgan alex@domain.com");
            Console.WriteLine($"'AlexMorgan2026!' with Personal Info -> HasNoPersonalInfo: {personalCheck.HasNoPersonalInfo}, Score: {personalCheck.Score}");
            if (personalCheck.HasNoPersonalInfo) throw new Exception("Personal info check failed");

            Console.WriteLine("\n=== [4] Testing Cryptographic Generator ===");
            var genOptions = new GeneratorOptions { Length = 20, IncludeUppercase = true, IncludeLowercase = true, IncludeNumbers = true, IncludeSymbols = true, AvoidSimilarChars = true };
            var generated = generator.Generate(genOptions);
            Console.WriteLine($"Generated Password (Length {generated.Password.Length}): {generated.Password}");
            Console.WriteLine($"Score: {generated.Score}, Level: {generated.StrengthLevel}, CrackTime: {generated.CrackTimeFormatted}");
            if (generated.Password.Length != 20 || generated.Score < 85) throw new Exception("Generator test failed");

            Console.WriteLine("\n>>> ALL VERIFICATION TESTS PASSED SUCCESSFULLY! <<<");
        }
    }
}
