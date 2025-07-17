using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiCash.Application.AutoMapper;

namespace CommonTestUtilities.Mapper
{
    public class MapperBuilder
    {
        public static IMapper Build ()
        {
            var mapper = new MapperConfiguration(config => 
            {
                config.AddProfile(new AutoMapping());
            });

            return mapper.CreateMapper();
        }
    }
}
