namespace TLALOCSG.DTOs.Reports;
public record MarginDto(int ProductId,
                        string Product,
                        decimal StdCost,
                        decimal AvgSalePrice,
                        decimal MarginPercent);