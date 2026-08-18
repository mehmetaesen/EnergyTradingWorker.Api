using EnergyTrading.Application;
using EnergyTrading.Domain;

namespace EnergyTrading.Application.Transparency;

public sealed record RawTransparencyData(DateOnly Date, string Payload);
