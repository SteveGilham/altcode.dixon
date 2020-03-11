using System.Diagnostics.CodeAnalysis;

namespace AltCode.Dixon.TestData
{
  public class Justifications
  {
    /// <summary>
    /// Used in a unit test -- should be covered
    /// Should generate a "No Justification given" FxCop message
    /// </summary>
    /// <returns>A constant string</returns>
    [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
    public string Token()
    {
      return "Canary";
    }

    /// <summary>
    /// Used in a unit test -- should be covered
    /// Should generate a "No Justification given" FxCop message
    /// </summary>
    /// <returns>A constant string</returns>
    [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic",
      Justification = "Shan't")]
    public string AnotherToken()
    {
      return "Canary";
    }

    /// <summary>
    /// Used in a unit test -- should be covered
    /// </summary>
    /// <returns>A constant string</returns>
    [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic",
      Justification = "Because of reasons")]
    public string YetAnotherToken()
    {
      return "Canary";
    }
  }
}