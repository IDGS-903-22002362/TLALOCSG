namespace TLALOCSG.DTOs.Reports;
public record SalesReportLineDto(DateTime Date,
                                 int Orders,
                                 int Units,
                                 decimal Total);