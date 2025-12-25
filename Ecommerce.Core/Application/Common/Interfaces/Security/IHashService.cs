namespace Ecommerce.Core.Application.Common.Interfaces.Security;

public interface IHashService
{
    string Hash(string input);
    bool Verify(string input, string hashedInput);
}