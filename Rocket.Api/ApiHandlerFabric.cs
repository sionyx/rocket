
namespace Rocket.Api
{
    public class ApiHandlerFabric : IApiHandlerFabric
    {
        public CashinPointsGetter CashinPointsGetter()
        {
            return new CashinPointsGetter(new BaseGetter(ApiConstants.ApiBaseAddress));
        }
    }
}
