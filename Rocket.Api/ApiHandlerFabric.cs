
namespace Rocket.Api
{
    public class ApiHandlerFabric
    {
        public CashinPointsGetter CashinPointsGetter()
        {
            return new CashinPointsGetter(new BaseGetter(ApiConstants.ApiBaseAddress));
        }
    }
}
