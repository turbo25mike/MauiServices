using System.Text.Json.Serialization;

namespace Turbo.Maui.Services.Examples.Shared.Models;

public class ShortUser
{
    public static ShortUser Create(User user) => new() { ID = user.ID, FullName = $"{user.FirstName} {user.LastName}" };

    [JsonPropertyName("id")]
    public string? ID { get; set; }
    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }
}

public partial class User : Model
{
    [JsonPropertyName("acceptedEulaVersion")]
    public int? AcceptedEulaVersion { get; set; }

    [JsonPropertyName("autoMapPinResults")]
    public bool AutoMapPinResults { get; set; } = true;

    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("measurementStandard")]
    public MeasurementStandard MeasurementStandard;

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }
}

public enum MeasurementStandard { Yards, Meters }

public class Model
{
    public Model()
    {
        ID = Guid.NewGuid().ToString();
        CreatedDate = DateTime.UtcNow;
        UpdatedDate = DateTime.UtcNow;
    }

    [JsonPropertyName("id")]
    public string ID { get; set; }

    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }

    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }

    [JsonPropertyName("updatedDate")]
    public DateTime? UpdatedDate { get; set; }

    [JsonPropertyName("removedBy")]
    public string? RemovedBy { get; set; }

    [JsonPropertyName("removedDate")]
    public DateTime? RemovedDate { get; set; }
}