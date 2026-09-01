using SecurePass.Models.ViewModels;

namespace SecurePass.Services
{
    public interface IPasswordGeneratorService
    {
        GeneratedPasswordResult Generate(GeneratorOptions options);
    }
}
