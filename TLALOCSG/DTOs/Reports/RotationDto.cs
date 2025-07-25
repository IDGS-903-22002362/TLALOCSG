namespace TLALOCSG.DTOs.Reports;
public record RotationDto(int MaterialId,
                          string Material,
                          decimal QtyOnHand,
                          decimal AvgDailyOut,
                          decimal DaysOfSupply);