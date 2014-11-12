using Rocket.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rocket.Api
{
    public class CashinPointsGetter
    {
        private readonly BaseGetter _baseGetter;
        internal CashinPointsGetter(BaseGetter baseGetter)
        {
            _baseGetter = baseGetter;
        }

        public async Task<IList<CashinPoint>> GetPointsAsync()
        {
            var data = await _baseGetter.GetData<CashinPointsResponse>("cashin.json");
            return (data != null) ? data.Points : null;
        }
    }
}
