using System.Text.Json;
using Ecommerce.Shared.Wrapper;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Ecommerce.Test.Authentication.Helpers;

public class AssertApiHelper(ITestOutputHelper output)
{

    #region Core Logging

    private void Log(string message)
    {
        output.WriteLine($"[ASSERT] {message}");
    }

    private void LogResult<T>(string description, ResponseType<T> result)
    {
        try
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            output.WriteLine($"[RESULT - {description}]");
            output.WriteLine(json);
        }
        catch
        {
            output.WriteLine("[RESULT] Could not serialize response.");
        }
    }

    #endregion

    #region Success Assertions

    /// <summary>
    /// Asserts that the result is a successful response.
    /// Also validates optional message and data.
    /// </summary>
    public void ShouldSucceed<T>(
        ResponseType<T> result,
        string? expectedMessage = null,
        T? expectedData = default)
    {
        try
        {
            Assert.True(result.Success, "Expected Success=true but got false.");
            Log("Response succeeded.");

            if (!string.IsNullOrWhiteSpace(expectedMessage))
                Assert.That(result.Message, Is.EqualTo(expectedMessage));

            if (expectedData is not null)
                Assert.That(result.Data, Is.EqualTo(expectedData));
        }
        catch (Exception ex)
        {
            Log("FAILED SUCCESS ASSERTION.");
            LogResult("Failure Details", result);
            throw new XunitException(ex.Message);
        }
    }

    #endregion

    #region Failure Assertions

    /// <summary>
    /// Asserts that a response failed with optional message + error list validation.
    /// </summary>
    public void ShouldFail<T>(
        ResponseType<T> result,
        string? expectedMessage = null,
        List<string>? expectedErrors = null)
    {
        try
        {
            Assert.False(result.Success, "Expected Success=false but got true.");
            Log("Response failed as expected.");

            if (!string.IsNullOrWhiteSpace(expectedMessage))
                Assert.That(result.Message, Is.EqualTo(expectedMessage));

            if (expectedErrors is not null && expectedErrors.Any())
            {
                Assert.NotNull(result.Errors);

                foreach (var err in expectedErrors)
                    Assert.Contains(err, result.Errors);
            }
        }
        catch (Exception ex)
        {
            Log("FAILED FAILURE ASSERTION.");
            LogResult("Failure Details", result);
            throw new XunitException(ex.Message);
        }
    }

    #endregion

    #region Collection Assertions

    public void ShouldAllSucceed<T>(
        IEnumerable<ResponseType<T>> results,
        string? expectedMessage = null)
    {
        int index = 0;

        foreach (var res in results)
        {
            try
            {
                ShouldSucceed(res, expectedMessage);
                index++;
            }
            catch
            {
                Log($"Failed response at index {index}");
                throw;
            }
        }

        Log($"All {index} results succeeded.");
    }

    #endregion

    /*#region Pagination

    public void ShouldBePaginated<T>(
        ResponseType<PagedList<T>> result,
        int expectedPage,
        int expectedPageSize,
        int? expectedTotalCount = null)
    {
        ShouldSucceed(result);

        Assert.AreEqual(expectedPage, result.Data.PageNumber);
        Assert.AreEqual(expectedPageSize, result.Data.PageSize);

        if (expectedTotalCount.HasValue)
            Assert.AreEqual(expectedTotalCount.Value, result.Data.TotalCount);

        Log("Pagination validated.");
    }

    #endregion*/

    #region Data Assertions

    public void ShouldMatch<T>(ResponseType<T> result, Func<T, bool> predicate, string failureMessage)
    {
        ShouldSucceed(result);

        Assert.True(predicate(result.Data!), failureMessage);

        Log($"Predicate matched: {failureMessage}");
    }

    public void ShouldEqual<T>(ResponseType<T> result, T expectedValue)
        where T : IEquatable<T>
    {
        ShouldSucceed(result);
        Assert.That(result.Data, Is.EqualTo(expectedValue));
        Log("Data matches expected value.");
    }

    public void ShouldHaveValidationError<T>(
        ResponseType<T> result,
        string fieldName,
        string? errorFragment = null)
    {
        ShouldFail(result);

        Assert.NotNull(result.Errors);

        var matchingErrors = result.Errors
            .Where(e => e.Contains(fieldName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(matchingErrors.Any(), $"No validation errors found for field '{fieldName}'");

        if (!string.IsNullOrWhiteSpace(errorFragment))
        {
            Assert.True(
                matchingErrors.Any(e => e.Contains(errorFragment, StringComparison.OrdinalIgnoreCase)),
                $"Field '{fieldName}' does not contain expected fragment '{errorFragment}'");
        }

        Log($"Validation error found: Field={fieldName}, Fragment={errorFragment}");
    }

    #endregion

    #region Specialized Assertions

    public void ShouldBeNotFound<T>(ResponseType<T> result)
    {
        ShouldFail(result, "Resource not found");
        Log("NotFound validated.");
    }

    public void ShouldBeUnauthorized<T>(ResponseType<T> result)
    {
        ShouldFail(result, "Unauthorized access");
        Log("Unauthorized validated.");
    }

    public void ShouldBeValidationError<T>(ResponseType<T> result)
    {
        ShouldFail(result, "Validation failed");

        Assert.NotNull(result.Errors);
        Assert.IsNotEmpty(result.Errors);

        Log("General validation error validated.");
    }

    #endregion

    #region Async Variants

    public async Task ShouldSucceedAsync<T>(
        Task<ResponseType<T>> resultTask,
        string? expectedMessage = null)
    {
        ShouldSucceed(await resultTask, expectedMessage);
    }

    public async Task ShouldFailAsync<T>(
        Task<ResponseType<T>> resultTask,
        string? expectedMessage = null,
        List<string>? expectedErrors = null)
    {
        ShouldFail(await resultTask, expectedMessage, expectedErrors);
    }

    #endregion

    #region Utility

    public T CreateTestEntity<T>(Action<T>? configure = null) where T : new()
    {
        var entity = new T();
        configure?.Invoke(entity);
        return entity;
    }

    #endregion
}
