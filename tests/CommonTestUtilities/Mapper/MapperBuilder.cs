using AutoMapper;
using MiCash.Application.AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace CommonTestUtilities.Mapper
{
    public class MapperBuilder
    {
        public static IMapper Build ()
        {
            var mapper = new MapperConfiguration(config => 
            {
                config.AddMaps(typeof(AutoMapping).Assembly);
            }, NullLoggerFactory.Instance);

            return mapper.CreateMapper();
        }
    }
}
