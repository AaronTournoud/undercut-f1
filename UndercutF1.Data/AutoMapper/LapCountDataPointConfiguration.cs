using AutoMapper;

namespace UndercutF1.Data.AutoMapper;

public class LapCountDataPointConfiguration : Profile
{
    public LapCountDataPointConfiguration() =>
        CreateMap<LapCountDataPoint, LapCountDataPoint>()
            .ForAllMembers(opts => opts.Condition((_, _, member) => member != null));
}
