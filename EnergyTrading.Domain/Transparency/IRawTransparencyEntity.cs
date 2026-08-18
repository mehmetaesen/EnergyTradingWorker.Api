using EnergyTrading.Domain;

namespace EnergyTrading.Domain.Transparency;

public interface IRawTransparencyEntity : IPeriodEntity
{
    string Payload { get; set; }
}
