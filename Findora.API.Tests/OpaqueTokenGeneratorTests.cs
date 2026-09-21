using Findora.API.Services;

namespace Findora.API.Tests;

public class OpaqueTokenGeneratorTests
{
    [Fact]
    public void GenerateRawToken_ProducesDifferentValuesEachCall()
    {
        var first = OpaqueTokenGenerator.GenerateRawToken();
        var second = OpaqueTokenGenerator.GenerateRawToken();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Hash_IsDeterministicForTheSameInput()
    {
        var token = OpaqueTokenGenerator.GenerateRawToken();

        Assert.Equal(OpaqueTokenGenerator.Hash(token), OpaqueTokenGenerator.Hash(token));
    }

    [Fact]
    public void Hash_DiffersForDifferentTokens()
    {
        var a = OpaqueTokenGenerator.GenerateRawToken();
        var b = OpaqueTokenGenerator.GenerateRawToken();

        Assert.NotEqual(OpaqueTokenGenerator.Hash(a), OpaqueTokenGenerator.Hash(b));
    }

    [Fact]
    public void Hash_NeverEqualsTheRawToken()
    {
        // Basic sanity check that we're storing a derived hash, not the
        // plaintext token itself.
        var token = OpaqueTokenGenerator.GenerateRawToken();

        Assert.NotEqual(token, OpaqueTokenGenerator.Hash(token));
    }
}
